using KartRace.Domain.Items;

namespace KartRace.Domain.Race
{
    /// <summary>
    /// One of the racers. It does not know who drives it (you or the AI): for the race, the item
    /// roulette and the rubber band every racer is the same, a character, how far it has gone and
    /// the item it carries. Who the rivals chase, and which odds table each racer gets, is decided
    /// outside, by handing the rubber band and the item boxes a list of racers.
    /// </summary>
    public sealed class Racer
    {
        public Character Character { get; }
        public RaceProgress Progress { get; }
        public ItemSlot Slot { get; } = new ItemSlot();

        public Racer(Character character, RaceProgress progress)
        {
            Character = character;
            Progress = progress;
        }

        /// <summary>Metres the other racer is ahead of this one (negative if it is behind).</summary>
        public float GapTo(Racer other) => other.Progress.Total - Progress.Total;

        public override string ToString() => Character.ToString();
    }
}
