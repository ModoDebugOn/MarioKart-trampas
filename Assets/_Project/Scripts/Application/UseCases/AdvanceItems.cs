using System.Collections.Generic;
using KartRace.Domain.Items;
using KartRace.Domain.Race;

namespace KartRace.Application.UseCases
{
    /// <summary>
    /// One frame of the items: the boxes that were taken count down to coming back, and the
    /// roulettes that spin count down to handing over their item.
    /// </summary>
    public sealed class AdvanceItems
    {
        readonly Standings standings;
        readonly IReadOnlyList<ItemBox> boxes;

        public AdvanceItems(Standings standings, IReadOnlyList<ItemBox> boxes)
        {
            this.standings = standings;
            this.boxes = boxes;
        }

        public void Execute(float deltaTime)
        {
            foreach (ItemBox box in boxes) box.Tick(deltaTime);
            foreach (Racer racer in standings.Order) racer.Slot.Tick(deltaTime);
        }
    }
}
