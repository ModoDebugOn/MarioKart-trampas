using System.Numerics;
using KartRace.Application;
using KartRace.Application.UseCases;
using KartRace.Domain.Driving;
using KartRace.Domain.Items;
using KartRace.Domain.Race;
using KartRace.Domain.Track;
using NUnit.Framework;

namespace KartRace.Tests
{
    public class AiItemTests
    {
        const float Far = float.MaxValue;
        static readonly ItemHabits Clumsy = new ItemHabits(4f, false), Sharp = new ItemHabits(0.5f, true);

        [Test]
        public void NothingUsed_BeforeItReacts()
        {
            Assert.IsFalse(Clumsy.ShouldUse(Item.Turbo, 3.9f, Far, Far));
            Assert.IsTrue(Clumsy.ShouldUse(Item.Turbo, 4f, Far, Far));
        }

        [Test]
        public void WithoutTactics_UsesAnythingAtAnyMoment()
        {
            Assert.IsTrue(Clumsy.ShouldUse(Item.Banana, 4f, Far, Far));   // nobody behind to hit
            Assert.IsTrue(Clumsy.ShouldUse(Item.RedPuck, 4f, Far, Far));  // nobody ahead to hit
            Assert.IsTrue(Clumsy.ShouldUse(Item.Horn, 4f, Far, Far));
        }

        [Test]
        public void WithTactics_TrapForWhoeverIsRightBehind()
        {
            Assert.IsFalse(Sharp.ShouldUse(Item.Banana, 1f, 40f, Far));
            Assert.IsTrue(Sharp.ShouldUse(Item.Banana, 1f, 10f, Far));
        }

        [Test]
        public void WithTactics_PuckForWhoeverIsAhead()
        {
            Assert.IsFalse(Sharp.ShouldUse(Item.RedPuck, 1f, Far, 100f));
            Assert.IsTrue(Sharp.ShouldUse(Item.RedPuck, 1f, Far, 20f));
            Assert.IsTrue(Sharp.ShouldUse(Item.Turbo, 1f, Far, Far));   // a boost: at once
        }

        [Test]
        public void WithTactics_HornWaits_ButNotForever()
        {
            Assert.IsFalse(Sharp.ShouldUse(Item.Horn, 5f, 5f, 5f));
            Assert.IsTrue(Sharp.ShouldUse(Item.Horn, Sharp.Reaction + ItemHabits.Patience, Far, Far));
        }

        [Test]
        public void Difficulty_HarderRivalsKeepTheirLineAndReactSooner()
        {
            Difficulty easy = Difficulty.Easy, normal = Difficulty.Normal, hard = Difficulty.Hard;
            Assert.Greater(easy.Wander, normal.Wander);
            Assert.Greater(normal.Wander, hard.Wander);
            Assert.Greater(easy.Items.Reaction, normal.Items.Reaction);
            Assert.Greater(normal.Items.Reaction, hard.Items.Reaction);
            Assert.IsFalse(easy.Items.Tactics);
            Assert.IsTrue(hard.Items.Tactics);
        }

        [Test]
        public void UseRivalItems_DropsTheTrapWhenSomeoneComesRightBehind()
        {
            var line = new TrackLine(new[] { new Vector2(0f, 0f), new Vector2(0f, 500f), new Vector2(500f, 500f), new Vector2(500f, 0f) });
            Racer you = new Racer(Character.Mario, new RaceProgress(line.Length, 100f));
            Racer rival = new Racer(Character.Luigi, new RaceProgress(line.Length, 300f));
            var standings = new Standings(new[] { rival, you });
            var banding = new RubberBanding(you) { Level = Difficulty.Hard };
            banding.AddRival(rival, new AiPilot(line, KartStats.Standard));
            var use = new UseRivalItems(standings, banding, new UseItem());

            rival.Slot.Spin(Item.Banana, 0f);
            use.Execute(1f);
            Assert.AreEqual(Item.Banana, rival.Slot.Held, "You are 200 m behind: it keeps it.");

            you.Progress.Update(290f);
            standings.Update();
            use.Execute(1f / 60f);
            Assert.IsTrue(rival.Slot.Empty, "You are right behind: it drops it.");
        }
    }
}
