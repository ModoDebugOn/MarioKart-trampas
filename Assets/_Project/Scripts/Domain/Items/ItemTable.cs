using System;
using System.Collections.Generic;

namespace KartRace.Domain.Items
{
    /// <summary>
    /// The odds of an item box: a list of bands by distance to the leader, each with a weight per
    /// item. The further behind you are, the further down the list, and the better the items.
    /// Distances are in the game's own units; ItemRoulette turns our metres into them.
    /// </summary>
    public sealed class ItemTable
    {
        public sealed class Band
        {
            /// <summary>Distance to the leader (units) up to which this band is used.</summary>
            public float UpTo { get; }
            public IReadOnlyList<(Item Item, int Weight)> Weights { get; }
            public int Total { get; }

            public Band(float upTo, params (Item Item, int Weight)[] weights)
            {
                UpTo = upTo;
                Weights = weights;
                foreach (var (_, weight) in weights) Total += weight;
                if (Total <= 0) throw new ArgumentException("A band needs at least one item.");
            }

            /// <summary>Chance of that item in this band: 0 … 1.</summary>
            public float Chance(Item item)
            {
                foreach (var (candidate, weight) in Weights)
                    if (candidate == item) return (float)weight / Total;
                return 0f;
            }
        }

        public IReadOnlyList<Band> Bands { get; }

        public ItemTable(params Band[] bands)
        {
            if (bands.Length == 0) throw new ArgumentException("A table needs at least one band.");
            Bands = bands;
        }

        /// <summary>The band for that distance to the leader (units); past the last one, the last one.</summary>
        public Band BandAt(float units)
        {
            foreach (Band band in Bands)
                if (units < band.UpTo) return band;
            return Bands[Bands.Count - 1];
        }

        // Mario Kart 8 (Wii U, version 4.1), Grand Prix, as transcribed by the Super Mario Wiki
        // ("Mario Kart 8 item probability distributions"). Weights are percentages times two, as in
        // the game. Mario Kart 8 Deluxe has not been published: this is the closest public table.
        // We have no fire flower, piranha plant, boomerang or crazy eight, so their weight is left
        // out and the rest share it out: a band with 25 of 200 in them gives the others 1/0.875.
        // [VERIFICAR] before recording.

        /// <summary>The table for racers driven by a player.</summary>
        public static ItemTable Mk8Players { get; } = new ItemTable(
            new Band(400, (Item.Banana, 65), (Item.GreenPuck, 50), (Item.RedPuck, 5), (Item.Turbo, 5),
                (Item.Horn, 5), (Item.Coin, 70)),
            new Band(1000, (Item.Banana, 20), (Item.GreenPuck, 25), (Item.RedPuck, 50), (Item.Turbo, 20),
                (Item.Bomb, 10), (Item.Horn, 5), (Item.Coin, 15), (Item.TripleBanana, 15),
                (Item.TripleGreenPuck, 10), (Item.TripleRedPuck, 5)),            // fire 10, piranha 10, boomerang 5
            new Band(2000, (Item.Banana, 10), (Item.GreenPuck, 20), (Item.RedPuck, 30), (Item.Turbo, 25),
                (Item.Bomb, 15), (Item.TripleTurbo, 15), (Item.Horn, 5), (Item.Coin, 5),
                (Item.TripleBanana, 10), (Item.TripleGreenPuck, 10), (Item.TripleRedPuck, 15)),   // fire 10, piranha 15, boomerang 10, crazy eight 5
            new Band(3300, (Item.GreenPuck, 15), (Item.RedPuck, 20), (Item.Turbo, 50), (Item.Bomb, 5),
                (Item.Ink, 5), (Item.TripleTurbo, 60), (Item.TripleGreenPuck, 10),
                (Item.TripleRedPuck, 10)),                                      // fire 5, piranha 5, boomerang 10, crazy eight 5
            new Band(5500, (Item.RedPuck, 10), (Item.Turbo, 30), (Item.Ink, 5), (Item.BlueBomb, 5),
                (Item.TripleTurbo, 85), (Item.Star, 25), (Item.Bullet, 10), (Item.GoldenTurbo, 25)),   // crazy eight 5
            new Band(8000, (Item.Turbo, 10), (Item.BlueBomb, 5), (Item.TripleTurbo, 65), (Item.Star, 40),
                (Item.Bullet, 30), (Item.Lightning, 5), (Item.GoldenTurbo, 40)),  // crazy eight 5
            new Band(13000, (Item.BlueBomb, 5), (Item.TripleTurbo, 35), (Item.Star, 35), (Item.Bullet, 60),
                (Item.Lightning, 10), (Item.GoldenTurbo, 55)),
            new Band(26000, (Item.TripleTurbo, 10), (Item.Star, 30), (Item.Bullet, 85), (Item.Lightning, 15),
                (Item.GoldenTurbo, 60)),
            new Band(float.PositiveInfinity, (Item.TripleTurbo, 30), (Item.Star, 40), (Item.Bullet, 70),
                (Item.GoldenTurbo, 60)));

        /// <summary>
        /// The table for racers driven by the game. Not the same as yours: its bands are shorter, it
        /// keeps getting bananas and pucks even far behind, and gets fewer bullets at the back.
        /// </summary>
        public static ItemTable Mk8Cpu { get; } = new ItemTable(
            new Band(300, (Item.Banana, 50), (Item.GreenPuck, 40), (Item.RedPuck, 15), (Item.Turbo, 10),
                (Item.Horn, 5), (Item.Coin, 70), (Item.TripleBanana, 10)),
            new Band(700, (Item.Banana, 25), (Item.GreenPuck, 30), (Item.RedPuck, 60), (Item.Turbo, 15),
                (Item.Bomb, 5), (Item.Horn, 5), (Item.Coin, 20), (Item.TripleBanana, 10),
                (Item.TripleGreenPuck, 10)),                                    // fire 5, piranha 10, boomerang 5
            new Band(1300, (Item.Banana, 30), (Item.GreenPuck, 35), (Item.RedPuck, 30), (Item.Turbo, 25),
                (Item.Bomb, 10), (Item.TripleTurbo, 10), (Item.Coin, 15), (Item.TripleBanana, 10),
                (Item.TripleGreenPuck, 10)),                                    // fire 10, piranha 5, boomerang 10
            new Band(2600, (Item.Banana, 30), (Item.GreenPuck, 35), (Item.RedPuck, 15), (Item.Turbo, 45),
                (Item.Bomb, 10), (Item.Ink, 5), (Item.TripleTurbo, 25), (Item.Coin, 5),
                (Item.TripleGreenPuck, 15)),                                    // fire 5, piranha 5, boomerang 5
            new Band(4500, (Item.Banana, 30), (Item.GreenPuck, 35), (Item.RedPuck, 5), (Item.Turbo, 50),
                (Item.Ink, 5), (Item.BlueBomb, 3), (Item.TripleTurbo, 47), (Item.Star, 10),
                (Item.GoldenTurbo, 10), (Item.TripleGreenPuck, 5)),
            new Band(7000, (Item.Banana, 15), (Item.GreenPuck, 20), (Item.Turbo, 50), (Item.BlueBomb, 4),
                (Item.TripleTurbo, 58), (Item.Star, 20), (Item.Bullet, 10), (Item.Lightning, 3),
                (Item.GoldenTurbo, 20)),
            new Band(13000, (Item.Banana, 10), (Item.GreenPuck, 10), (Item.Turbo, 30), (Item.TripleTurbo, 57),
                (Item.Star, 30), (Item.Bullet, 30), (Item.Lightning, 3), (Item.GoldenTurbo, 30)),
            new Band(26000, (Item.Banana, 10), (Item.GreenPuck, 10), (Item.Turbo, 10), (Item.TripleTurbo, 42),
                (Item.Star, 30), (Item.Bullet, 55), (Item.Lightning, 3), (Item.GoldenTurbo, 40)),
            new Band(float.PositiveInfinity, (Item.Turbo, 30), (Item.TripleTurbo, 60), (Item.Star, 30),
                (Item.Bullet, 50), (Item.GoldenTurbo, 30)));
    }
}
