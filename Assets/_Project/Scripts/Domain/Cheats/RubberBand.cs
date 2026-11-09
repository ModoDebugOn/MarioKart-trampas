using System;

namespace KartRace.Domain.Cheats
{
    /// <summary>
    /// The invisible rubber band between a rival and the racer it chases. The rival wants to sit
    /// Lead metres ahead of you (negative: behind you); the further it is from that spot, the harder
    /// the band pulls:
    /// - Rival behind its spot: it speeds up, up to MaxBoost once it is CatchUpDistance off.
    /// - Rival ahead of its spot: it holds back, down to MaxHoldBack once it is HoldBackDistance off.
    /// - Close (within DeadZone): it races normally, so it does not feel glued to you.
    /// Difficulty does not change the band, only its numbers.
    /// Nintendo has never published how Mario Kart does it: this is how it is usually done.
    /// </summary>
    public sealed class RubberBand
    {
        /// <summary>Metres either side of its spot where the band is slack.</summary>
        public float DeadZone { get; }
        /// <summary>Extra speed at full stretch behind: 0.3 is 30 % faster.</summary>
        public float MaxBoost { get; }
        /// <summary>Metres behind (beyond the dead zone) at which the boost is full.</summary>
        public float CatchUpDistance { get; }
        /// <summary>Speed given up at full stretch ahead: 0.2 is 20 % slower.</summary>
        public float MaxHoldBack { get; }
        /// <summary>Metres ahead (beyond the dead zone) at which it holds back the most.</summary>
        public float HoldBackDistance { get; }
        /// <summary>Metres ahead of you where the rival wants to be: 20 sits in front, -15 behind.</summary>
        public float Lead { get; }

        public RubberBand(float deadZone, float maxBoost, float catchUpDistance, float maxHoldBack,
                          float holdBackDistance, float lead = 0f)
        {
            DeadZone = deadZone;
            MaxBoost = maxBoost;
            CatchUpDistance = catchUpDistance;
            MaxHoldBack = maxHoldBack;
            HoldBackDistance = holdBackDistance;
            Lead = lead;
        }

        public static RubberBand Normal { get; } = new RubberBand(
            deadZone: 5f, maxBoost: 0.3f, catchUpDistance: 50f, maxHoldBack: 0.2f, holdBackDistance: 30f);

        /// <summary>
        /// Pace for a rival (1 normal, more is faster) from the gap: metres the chased racer is
        /// ahead of it (Racer.GapTo), so positive when the rival is behind.
        /// Each rival can have its own extra slack: slackBehind metres it may drop behind Lead before
        /// it speeds up, slackAhead metres it may get past Lead before it waits. Different slack for
        /// each rival is what spreads the pack: a pack slower than you strings out behind you, each
        /// rival at its own edge, instead of all of them stopping at the same point.
        /// </summary>
        public float Pace(float gap, float slackBehind = 0f, float slackAhead = 0f)
        {
            float ahead = -gap;                               // metres the rival is ahead of you
            float near = Lead - slackBehind - DeadZone;
            float far = Lead + slackAhead + DeadZone;
            if (ahead < near)
                return 1f + MaxBoost * Stretch(near - ahead, CatchUpDistance);
            if (ahead > far)
                return 1f - MaxHoldBack * Stretch(ahead - far, HoldBackDistance);
            return 1f;
        }

        /// <summary>0 slack … 1 fully stretched, growing in a straight line.</summary>
        static float Stretch(float metres, float full) => Math.Clamp(metres / full, 0f, 1f);
    }
}
