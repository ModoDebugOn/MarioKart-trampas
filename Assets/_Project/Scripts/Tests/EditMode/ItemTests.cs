using KartRace.Application.UseCases;
using KartRace.Domain.Items;
using KartRace.Domain.Race;
using NUnit.Framework;

namespace KartRace.Tests
{
    public class ItemTests
    {
        // A 1.5 km lap, like our circuit: 26000 units = 750 m.
        const float Lap = 1500f;
        static readonly ItemRoulette Players = new ItemRoulette(ItemTable.Mk8Players, Lap);

        [Test]
        public void Scale_TheFarthestBandIsHalfALap()
        {
            Assert.AreEqual(750f, ItemRoulette.FarUnits * Players.MetresPerUnit, 1e-3f);
        }

        [Test]
        public void Leading_GetsBananasAndCoins_NeverAStarOrABullet()
        {
            Assert.Greater(Players.Chance(Item.Banana, 0f), 0.3f);
            Assert.Greater(Players.Chance(Item.Coin, 0f), 0.3f);
            Assert.AreEqual(0f, Players.Chance(Item.Star, 0f));
            Assert.AreEqual(0f, Players.Chance(Item.Bullet, 0f));
        }

        [Test]
        public void FarBehind_GetsStarsAndBullets_NeverABanana()
        {
            Assert.Greater(Players.Chance(Item.Bullet, 800f), 0.3f);
            Assert.Greater(Players.Chance(Item.Star, 800f), 0.15f);
            Assert.AreEqual(0f, Players.Chance(Item.Banana, 800f));
        }

        [Test]
        public void TheBlueBomb_OnlyInTheMiddleOfThePack()
        {
            Assert.AreEqual(0f, Players.Chance(Item.BlueBomb, 0f));
            Assert.Greater(Players.Chance(Item.BlueBomb, 120f), 0f);   // ~4200 units
            Assert.AreEqual(0f, Players.Chance(Item.BlueBomb, 800f));
        }

        [Test]
        public void EveryBand_AddsUpToOne()
        {
            foreach (ItemTable table in new[] { ItemTable.Mk8Players, ItemTable.Mk8Cpu })
                foreach (ItemTable.Band band in table.Bands)
                {
                    float sum = 0f;
                    foreach (var (item, _) in band.Weights) sum += band.Chance(item);
                    Assert.AreEqual(1f, sum, 1e-5f);
                }
        }

        [Test]
        public void Bands_SwitchAtTheirDistance()
        {
            ItemTable table = ItemTable.Mk8Players;
            Assert.AreSame(table.Bands[0], table.BandAt(399f));
            Assert.AreSame(table.Bands[1], table.BandAt(400f));
            Assert.AreSame(table.Bands[table.Bands.Count - 1], table.BandAt(1e9f));
        }

        [Test]
        public void Pick_FollowsTheWeights()
        {
            // First band: banana 65, green 50, red 5, turbo 5, horn 5, coin 70 (of 200).
            Assert.AreEqual(Item.Banana, Players.Pick(0f, 0.0));
            Assert.AreEqual(Item.Banana, Players.Pick(0f, 64.9 / 200));
            Assert.AreEqual(Item.GreenPuck, Players.Pick(0f, 65.1 / 200));
            Assert.AreEqual(Item.Coin, Players.Pick(0f, 0.999));
        }

        [Test]
        public void Pick_ManyRolls_MatchTheChances()
        {
            var random = new System.Random(1);
            int bullets = 0, rolls = 20000;
            for (int i = 0; i < rolls; i++)
                if (Players.Pick(800f, random.NextDouble()) == Item.Bullet) bullets++;
            Assert.AreEqual(Players.Chance(Item.Bullet, 800f), bullets / (float)rolls, 0.02f);
        }

        [Test]
        public void Box_GoneAfterTaking_BackAfterRespawn()
        {
            var box = new ItemBox(2f);
            Assert.IsTrue(box.Take());
            Assert.IsFalse(box.Present);
            Assert.IsFalse(box.Take());
            box.Tick(1.5f);
            Assert.IsFalse(box.Present);
            box.Tick(0.6f);
            Assert.IsTrue(box.Present);
        }

        static Racer At(Character who, float total) => new Racer(who, new RaceProgress(Lap, total));

        /// <summary>A race of the two racers; the humans get the players' table, the rest the AI's.</summary>
        static OpenItemBox Open(Racer leader, Racer last, params Racer[] humans) =>
            new OpenItemBox(new Standings(new[] { leader, last }), Players, new ItemRoulette(ItemTable.Mk8Cpu, Lap),
                            humans, new System.Random(7));

        [Test]
        public void Open_SpinsTheRouletteOfYourDistance()
        {
            Racer leader = At(Character.Mario, 900f), last = At(Character.Luigi, 100f);   // 800 m behind
            OpenItemBox open = Open(leader, last, leader, last);

            Item? first = open.Execute(leader, new ItemBox(2f));
            Item? behind = open.Execute(last, new ItemBox(2f));
            Assert.Greater(Players.Chance(first.Value, 0f), 0f);
            Assert.Greater(Players.Chance(behind.Value, 800f), 0f);
            Assert.IsTrue(last.Slot.Spinning);
            Assert.AreEqual(behind, last.Slot.Rolled);
        }

        [Test]
        public void Open_WithAnItem_BreaksTheBoxAndGivesNothing()
        {
            Racer leader = At(Character.Mario, 900f), last = At(Character.Luigi, 100f);
            OpenItemBox open = Open(leader, last, leader);
            ItemBox one = new ItemBox(2f), two = new ItemBox(2f);

            Assert.IsNotNull(open.Execute(leader, one));
            Assert.IsNull(open.Execute(leader, two));    // still spinning
            Assert.IsFalse(two.Present);                 // the box broke all the same
            Assert.IsNull(open.Execute(last, one));      // gone: nothing for the next one
        }

        [Test]
        public void Advance_StopsTheRouletteAndBringsTheBoxBack()
        {
            Racer leader = At(Character.Mario, 900f), last = At(Character.Luigi, 100f);
            var standings = new Standings(new[] { leader, last });
            var open = new OpenItemBox(standings, Players, Players, new[] { leader }, new System.Random(7)) { SpinTime = 1f };
            var box = new ItemBox(2f);
            var advance = new AdvanceItems(standings, new[] { box });

            Item? item = open.Execute(leader, box);
            advance.Execute(2.1f);
            Assert.AreEqual(item, leader.Slot.Held);
            Assert.IsTrue(box.Present);
        }

        [Test]
        public void PlayersAndAi_SpinDifferentTables()
        {
            Racer you = At(Character.Mario, 900f), rival = At(Character.Luigi, 100f);
            OpenItemBox open = Open(you, rival, you);
            Assert.AreSame(ItemTable.Mk8Players, open.RouletteOf(you).Table);
            Assert.AreSame(ItemTable.Mk8Cpu, open.RouletteOf(rival).Table);
        }

        [Test]
        public void Slot_ItemDecidedAtOnce_InYourHandsWhenTheSpinStops()
        {
            var slot = new ItemSlot();
            Assert.IsTrue(slot.Empty);
            slot.Spin(Item.Star, 3f);
            Assert.AreEqual(Item.Star, slot.Rolled);
            Assert.IsNull(slot.Held);
            Assert.IsNull(slot.Use());   // cannot use it while it spins
            slot.Tick(3.1f);
            Assert.AreEqual(Item.Star, slot.Held);
            Assert.AreEqual(Item.Star, slot.Use());
            Assert.IsTrue(slot.Empty);
        }
    }
}
