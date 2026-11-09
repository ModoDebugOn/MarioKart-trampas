using KartRace.Domain.Cheats;
using KartRace.Domain.Items;
using KartRace.Domain.Race;
using UnityEngine;

namespace KartRace.Infrastructure.Profiles
{
    /// <summary>
    /// A Domain Difficulty as an asset, so its numbers become sliders in the inspector, plus how it
    /// shows on screen. The builder fills it from Difficulty.Easy / Normal / Hard.
    /// Changing the sliders in Play mode changes the race on the spot.
    /// </summary>
    [CreateAssetMenu(menuName = "Kart Race/Difficulty", fileName = "Difficulty")]
    public sealed class DifficultyProfile : ScriptableObject
    {
        [Tooltip("Name shown on screen (in Spanish).")]
        [SerializeField] string displayName = "Normal";
        [Tooltip("Colour of its name on screen.")]
        [SerializeField] Color color = Color.white;

        [Header("Rubber band")]
        [Tooltip("Metres either side of its spot where it is slack.")]
        [SerializeField, Range(0f, 30f)] float deadZone = 5f;
        [Tooltip("Extra speed for a rival far behind: 0.3 = 30 % faster.")]
        [SerializeField, Range(0f, 1f)] float maxBoost = 0.3f;
        [Tooltip("Metres behind (past the dead zone) where the boost is full.")]
        [SerializeField, Range(5f, 200f)] float catchUpDistance = 50f;
        [Tooltip("Speed given up by a rival far ahead: 0.2 = 20 % slower.")]
        [SerializeField, Range(0f, 0.8f)] float maxHoldBack = 0.2f;
        [Tooltip("Metres ahead (past the dead zone) where it holds back the most.")]
        [SerializeField, Range(5f, 200f)] float holdBackDistance = 30f;
        [Tooltip("Metres ahead of you where the rivals want to be (negative: behind you).")]
        [SerializeField, Range(-50f, 50f)] float lead;

        [Header("Rivals")]
        [Tooltip("How good the rivals are on average: 1.04 = 4 % faster than a normal kart.")]
        [SerializeField, Range(0.8f, 1.2f)] float skill = 1f;
        [Tooltip("Share of the AI route's width they drift over: 0 glued to the racing line, 1 edge to edge.")]
        [SerializeField, Range(0f, 1f)] float wander = 0.7f;
        [Tooltip("Seconds a rival holds an item before using it.")]
        [SerializeField, Range(0f, 10f)] float itemReaction = 1.5f;
        [Tooltip("Waits for the right moment: a trap when someone is right behind, a puck when someone is ahead.")]
        [SerializeField] bool itemTactics = true;

        public string DisplayName => displayName;
        public Color Color => color;

        /// <summary>Goes up every time a value changes in the inspector.</summary>
        public int Version { get; private set; }

        public Difficulty CreateDifficulty() => new Difficulty(
            new RubberBand(deadZone, maxBoost, catchUpDistance, maxHoldBack, holdBackDistance, lead), skill,
            wander, new ItemHabits(itemReaction, itemTactics));

        /// <summary>Copies a Domain difficulty into the sliders (used by the editor builder).</summary>
        public void Fill(Difficulty difficulty, string name, Color colour)
        {
            displayName = name;
            color = colour;
            RubberBand band = difficulty.Band;
            deadZone = band.DeadZone;
            maxBoost = band.MaxBoost;
            catchUpDistance = band.CatchUpDistance;
            maxHoldBack = band.MaxHoldBack;
            holdBackDistance = band.HoldBackDistance;
            lead = band.Lead;
            skill = difficulty.Skill;
            wander = difficulty.Wander;
            itemReaction = difficulty.Items.Reaction;
            itemTactics = difficulty.Items.Tactics;
            OnValidate();
        }

        void OnValidate() => Version++;
    }
}
