using System.Collections.Generic;
using KartRace.Domain.Driving;
using KartRace.Domain.Race;
using KartRace.Domain.Cheats;

namespace KartRace.Application
{
    /// <summary>
    /// Pulls every rival towards the humans: each frame, the chase rule says how big the rival's
    /// gap is (to which human, or to what spot, depends on the rule), and the rubber band turns it
    /// into the rival's pace. Switched off, everyone races at their normal pace.
    /// Each rival has a rank in the pack (0 the worst … 1 the best) that decides two things:
    /// - its skill: the best has the difficulty's, the worst SkillSpread less;
    /// - its slack (how far it may drop behind or get ahead before the band pulls): the worst may
    ///   drop the furthest behind, the best may get the furthest ahead. So the pack strings out along
    ///   the track instead of everyone stopping at the same point.
    /// </summary>
    public sealed class RubberBanding
    {
        public sealed class Rival
        {
            public Racer Racer { get; }
            public AiPilot Pilot { get; }
            /// <summary>Its place in the list of rivals, which the chase rule may use.</summary>
            public int Index { get; }
            /// <summary>Its place in the pack: 0 the worst … 1 the best.</summary>
            public float Rank { get; internal set; }
            /// <summary>Metres it may drop behind the band's Lead before it speeds up.</summary>
            public float SlackBehind { get; internal set; }
            /// <summary>Metres it may get past the band's Lead before it waits.</summary>
            public float SlackAhead { get; internal set; }
            /// <summary>
            /// The gap its band measured last: metres the spot it chases is ahead of it. That spot
            /// is Racer.Progress.Total + Gap along the track.
            /// </summary>
            public float Gap { get; internal set; }

            public Rival(Racer racer, AiPilot pilot, int index)
            {
                Racer = racer;
                Pilot = pilot;
                Index = index;
            }
        }

        readonly IReadOnlyList<Racer> humans;
        readonly List<Rival> rivals = new List<Rival>();

        /// <summary>Who the rivals chase when there is more than one human; switchable during the race.</summary>
        public IChaseRule Rule { get; set; }
        /// <summary>The band's numbers and how good the rivals are (speed, line, items).</summary>
        public Difficulty Level { get; set; } = Difficulty.Normal;
        public RubberBand Band => Level.Band;
        public bool Enabled { get; set; } = true;
        /// <summary>The best rival has the level's skill; the worst is this much slower: 0.06 = 6 %.</summary>
        public float SkillSpread { get; set; } = 0.06f;
        /// <summary>Metres of track the pack strings out over behind the band's Lead (the worst rival's slack).</summary>
        public float PackLength { get; set; } = 160f;
        /// <summary>Metres past the band's Lead the best rival may get before it waits (the rest, less).</summary>
        public float PackAhead { get; set; } = 30f;

        public IReadOnlyList<Rival> Rivals => rivals;
        public IReadOnlyList<Racer> Humans => humans;

        /// <param name="humans">The racers driven by people (at least one).</param>
        /// <param name="rule">Who the rivals chase among them.</param>
        public RubberBanding(IReadOnlyList<Racer> humans, IChaseRule rule)
        {
            this.humans = humans;
            Rule = rule;
        }

        /// <summary>One human: every rule gives the same, the gap to that racer.</summary>
        public RubberBanding(Racer chased) : this(new[] { chased }, new ChaseLeader()) { }

        public Rival AddRival(Racer racer, AiPilot pilot)
        {
            var rival = new Rival(racer, pilot, rivals.Count);
            rivals.Add(rival);
            // A new rival changes the pack's size: rank them all again.
            for (int i = 0; i < rivals.Count; i++) rivals[i].Rank = Rank(i, rivals.Count);
            return rival;
        }

        public void Update()
        {
            foreach (Rival rival in rivals)
            {
                rival.Pilot.Skill = Level.Skill * (1f - SkillSpread * (1f - rival.Rank));
                rival.Pilot.Wander = Level.Wander;   // how tidy its line is: the level's, the same for all
                rival.SlackBehind = PackLength * (1f - rival.Rank);
                rival.SlackAhead = PackAhead * rival.Rank;
                rival.Gap = Rule.Gap(rival.Racer, rival.Index, humans);
                rival.Pilot.Pace = Enabled ? Band.Pace(rival.Gap, rival.SlackBehind, rival.SlackAhead) : 1f;
            }
        }

        /// <summary>
        /// Spreads n ranks evenly from 0 to 1, mixed so the order they joined (the grid) is not
        /// the order of the pack. 11 shares no factor with 19, so every rank is used once.
        /// </summary>
        static float Rank(int index, int count)
        {
            if (count <= 1) return 0.5f;
            int slot = (index * 11) % count;
            return slot / (count - 1f);
        }
    }
}
