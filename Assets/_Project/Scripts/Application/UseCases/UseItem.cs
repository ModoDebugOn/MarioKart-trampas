using KartRace.Domain.Items;
using KartRace.Domain.Race;

namespace KartRace.Application.UseCases
{
    /// <summary>
    /// A racer uses the item in its hands, which leaves room for the next box. For now that is all
    /// it does: what each item does on the track comes later.
    /// </summary>
    public sealed class UseItem
    {
        /// <summary>The item used, or null if the racer had none (or its roulette still spins).</summary>
        public Item? Execute(Racer racer) => racer.Slot.Use();
    }
}
