using System;
using System.Collections.Generic;
using KartRace.Domain.Items;
using KartRace.Domain.Race;

namespace KartRace.Application.UseCases
{
    /// <summary>
    /// A racer drives through an item box. If the box is there it breaks; if the racer has nothing,
    /// the roulette looks at how far behind the leader it is and decides the item on the spot, then
    /// spins for SpinTime seconds before handing it over.
    /// Players and the AI get different odds tables. A racer does not know who drives it: the
    /// players are the racers in the list this is given, everyone else is the AI.
    /// </summary>
    public sealed class OpenItemBox
    {
        readonly Standings standings;
        readonly ItemRoulette players, cpu;
        readonly HashSet<Racer> humans;
        readonly Random random;

        /// <summary>Seconds the roulette spins before you get the item (our number, not measured).</summary>
        public float SpinTime { get; set; } = 3f;

        /// <param name="random">Seeded from outside, so a recording gives the same items every take.</param>
        public OpenItemBox(Standings standings, ItemRoulette players, ItemRoulette cpu,
                           IEnumerable<Racer> humans, Random random)
        {
            this.standings = standings;
            this.players = players;
            this.cpu = cpu;
            this.humans = new HashSet<Racer>(humans);
            this.random = random;
        }

        /// <summary>The roulette that racer spins: the players' one or the AI's one.</summary>
        public ItemRoulette RouletteOf(Racer racer) => humans.Contains(racer) ? players : cpu;

        /// <summary>
        /// Returns the item the racer will get, or null if the box was not there or the racer
        /// already had one (the box breaks anyway, as in the game).
        /// </summary>
        public Item? Execute(Racer racer, ItemBox box)
        {
            if (!box.Take() || !racer.Slot.Empty) return null;
            Item item = RouletteOf(racer).Pick(standings.DistanceToLeader(racer), random.NextDouble());
            racer.Slot.Spin(item, SpinTime);
            return item;
        }
    }
}
