using System.Collections.Generic;
using KartRace.Domain.Race;

namespace KartRace.Domain.Cheats
{
    /// <summary>
    /// With more than one human in the race, who does a rival's rubber band pull towards?
    /// The answer is a gap, not a racer: metres the spot it chases is ahead of the rival (positive
    /// when the rival is behind it), the number the band works with. One rule ("the average of the
    /// humans") chases no racer at all, only a spot on the track.
    /// With one human every rule gives the same: the gap to that human.
    /// Nintendo has not said what Mario Kart does: these are the options, to compare them.
    /// </summary>
    public interface IChaseRule
    {
        /// <param name="rival">The rival whose band it is.</param>
        /// <param name="index">The rival's place in the list of rivals (0, 1, 2…): fixed for the whole race.</param>
        /// <param name="humans">The racers driven by people: at least one.</param>
        float Gap(Racer rival, int index, IReadOnlyList<Racer> humans);
    }

    /// <summary>
    /// a) Every rival chases the human in front. The race is the leader's, as with one player; the
    /// human at the back is left alone, with nobody near.
    /// </summary>
    public sealed class ChaseLeader : IChaseRule
    {
        public float Gap(Racer rival, int index, IReadOnlyList<Racer> humans)
        {
            Racer leader = humans[0];
            foreach (Racer human in humans)
                if (human.Progress.Total > leader.Progress.Total) leader = human;
            return rival.GapTo(leader);
        }
    }

    /// <summary>
    /// b) Each rival chases the human nearest to it, ahead or behind. The pack splits in two; the
    /// rivals halfway between switch targets and lurch, and the group with the human at the back
    /// can leave the leader almost alone.
    /// </summary>
    public sealed class ChaseNearest : IChaseRule
    {
        public float Gap(Racer rival, int index, IReadOnlyList<Racer> humans)
        {
            float best = rival.GapTo(humans[0]);
            foreach (Racer human in humans)
            {
                float gap = rival.GapTo(human);
                if (System.Math.Abs(gap) < System.Math.Abs(best)) best = gap;
            }
            return best;
        }
    }

    /// <summary>
    /// c) Every rival chases the average of the humans: a spot halfway between them. With the
    /// humans far apart, the AI races in no man's land, too slow for the one in front and too fast
    /// for the one at the back: nobody gets a race.
    /// </summary>
    public sealed class ChaseAverage : IChaseRule
    {
        public float Gap(Racer rival, int index, IReadOnlyList<Racer> humans)
        {
            float sum = 0f;
            foreach (Racer human in humans) sum += rival.GapTo(human);
            return sum / humans.Count;
        }
    }

    /// <summary>
    /// d) The rivals are shared out: each one chases one human for the whole race, taking turns
    /// down the list (first rival the first human, second the second…). Each player gets part of
    /// the pack, but which part is arbitrary, and nothing says the good rivals are not all on one side.
    /// </summary>
    public sealed class SplitPack : IChaseRule
    {
        public float Gap(Racer rival, int index, IReadOnlyList<Racer> humans) =>
            rival.GapTo(humans[index % humans.Count]);
    }
}
