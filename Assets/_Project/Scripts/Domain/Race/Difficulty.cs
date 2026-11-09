using KartRace.Domain.Cheats;
using KartRace.Domain.Items;

namespace KartRace.Domain.Race
{
    /// <summary>
    /// A difficulty level. It does not change how the AI works, only its numbers: the rubber band
    /// (how much a rival behind may speed up, how much one ahead holds back, where it wants to be
    /// compared with you) and how good the rivals are: how fast, how tidy their line and how well
    /// they use their items.
    /// The numbers are ours, chosen so the difference shows on screen: Nintendo has never published theirs.
    /// </summary>
    public sealed class Difficulty
    {
        public RubberBand Band { get; }
        /// <summary>
        /// How good the best rival is: 1 is as fast as your kart, 0.92 an 8 % slower. Above 1 a rival
        /// ahead of you can never be caught, band or no band.
        /// </summary>
        public float Skill { get; }
        /// <summary>
        /// Share of the AI route's width the rivals drift over (AiPilot.Wander): a good rival sticks
        /// to the racing line, a clumsy one wanders from edge to edge.
        /// </summary>
        public float Wander { get; }
        /// <summary>When the rivals use their items.</summary>
        public ItemHabits Items { get; }

        public Difficulty(RubberBand band, float skill, float wander = 0.7f, ItemHabits items = null)
        {
            Band = band;
            Skill = skill;
            Wander = wander;
            Items = items ?? new ItemHabits(1.5f, true);
        }

        // If you are faster, the best rival settles at Lead - DeadZone: about 33 m behind you on easy,
        // 17 on normal, and 2 m AHEAD of you on hard: just enough to overtake you. How far it may then
        // get before it waits is each rival's own slack (RaceBootstrap's packAhead).

        /// <summary>
        /// The rivals stay well behind you, barely catch up and hold back a lot. They wander all
        /// over the road and use their items late and at any moment.
        /// </summary>
        public static Difficulty Easy { get; } = new Difficulty(new RubberBand(
            deadZone: 8f, maxBoost: 0.15f, catchUpDistance: 80f, maxHoldBack: 0.35f, holdBackDistance: 30f, lead: -25f),
            skill: 0.92f, wander: 1f, items: new ItemHabits(reaction: 4f, tactics: false));

        /// <summary>They stay a bit behind you, and catch up about as much as they hold back.</summary>
        public static Difficulty Normal { get; } = new Difficulty(new RubberBand(
            deadZone: 5f, maxBoost: 0.3f, catchUpDistance: 50f, maxHoldBack: 0.2f, holdBackDistance: 30f, lead: -12f),
            skill: 0.97f, wander: 0.7f, items: new ItemHabits(reaction: 1.5f, tactics: true));

        /// <summary>
        /// The best rival gets just ahead of you and stays there: it is as fast as you and catches up
        /// hard, so driving alone you cannot pass it for good; it takes something extra (an item, a
        /// better line). The rest catch up hard and wait for you half as much. They keep to the
        /// racing line and use their items at once, at the right moment.
        /// </summary>
        public static Difficulty Hard { get; } = new Difficulty(new RubberBand(
            deadZone: 6f, maxBoost: 0.5f, catchUpDistance: 40f, maxHoldBack: 0.1f, holdBackDistance: 40f, lead: 8f),
            skill: 1f, wander: 0.3f, items: new ItemHabits(reaction: 0.4f, tactics: true));
    }
}
