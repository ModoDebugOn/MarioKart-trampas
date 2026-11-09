using System.Collections.Generic;
using KartRace.Domain.Items;
using KartRace.Domain.Race;

namespace KartRace.Application.UseCases
{
    /// <summary>
    /// One frame of the rivals' items: each one with an item in its hands uses it when the level's
    /// habits say so (how long it takes to react, and whether it waits for the right moment: someone
    /// right behind for a trap, someone ahead for a puck). The habits only need who is just ahead
    /// and just behind, which the standings give.
    /// </summary>
    public sealed class UseRivalItems
    {
        readonly Standings standings;
        readonly RubberBanding banding;
        readonly UseItem useItem;
        readonly Dictionary<Racer, float> heldFor = new Dictionary<Racer, float>();

        public UseRivalItems(Standings standings, RubberBanding banding, UseItem useItem)
        {
            this.standings = standings;
            this.banding = banding;
            this.useItem = useItem;
        }

        public void Execute(float deltaTime)
        {
            ItemHabits habits = banding.Level.Items;
            foreach (RubberBanding.Rival rival in banding.Rivals)
            {
                Racer racer = rival.Racer;
                Item? item = racer.Slot.Held;
                if (item == null)
                {
                    heldFor.Remove(racer);
                    continue;
                }
                heldFor.TryGetValue(racer, out float held);
                heldFor[racer] = held += deltaTime;
                Gaps(racer, out float behind, out float ahead);
                if (!habits.ShouldUse(item.Value, held, behind, ahead)) continue;
                useItem.Execute(racer);
                heldFor.Remove(racer);
            }
        }

        /// <summary>Metres to the racer just behind and just ahead (float.MaxValue if there is none).</summary>
        void Gaps(Racer racer, out float behind, out float ahead)
        {
            IReadOnlyList<Racer> order = standings.Order;
            int i = standings.PositionOf(racer) - 1;
            ahead = i > 0 ? racer.GapTo(order[i - 1]) : float.MaxValue;
            behind = i < order.Count - 1 ? order[i + 1].GapTo(racer) : float.MaxValue;
        }
    }
}
