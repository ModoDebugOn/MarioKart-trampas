using System;
using System.Collections.Generic;

namespace KartRace.Domain.Race
{
    /// <summary>
    /// The race classification: who is first and how far behind the leader everyone else is.
    /// The item roulette reads it (distance to the leader decides your odds).
    /// </summary>
    public sealed class Standings
    {
        readonly List<Racer> order;

        /// <summary>Racers from first to last, as of the last Update.</summary>
        public IReadOnlyList<Racer> Order => order;
        public Racer Leader => order[0];

        public Standings(IEnumerable<Racer> racers)
        {
            order = new List<Racer>(racers);
            if (order.Count == 0) throw new ArgumentException("A race needs at least one racer.");
            Update();
        }

        /// <summary>
        /// Sorts by total distance, furthest first. Insertion sort on purpose: it is stable (on a tie
        /// nobody swaps places, so positions do not flicker) and almost free, since from one frame to
        /// the next only a couple of racers change places.
        /// </summary>
        public void Update()
        {
            for (int i = 1; i < order.Count; i++)
            {
                Racer racer = order[i];
                int j = i - 1;
                while (j >= 0 && order[j].Progress.Total < racer.Progress.Total)
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = racer;
            }
        }

        /// <summary>Position in the race: 1 for the leader.</summary>
        public int PositionOf(Racer racer)
        {
            int index = order.IndexOf(racer);
            if (index < 0) throw new ArgumentException($"{racer} is not in this race.");
            return index + 1;
        }

        /// <summary>Metres between the racer and the leader (0 for the leader itself).</summary>
        public float DistanceToLeader(Racer racer) => racer.GapTo(Leader);
    }
}
