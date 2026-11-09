namespace KartRace.Domain.Driving
{
    /// <summary>
    /// How a kart drives. Arcade numbers, not a car simulation: the video is about who is ahead and
    /// by how much, so a kart only needs to speed up, slow down and turn in a believable way.
    /// </summary>
    public sealed class KartStats
    {
        /// <summary>Top speed going forwards, m/s.</summary>
        public float TopSpeed { get; }
        /// <summary>m/s gained every second with the throttle down.</summary>
        public float Acceleration { get; }
        /// <summary>m/s lost every second while braking.</summary>
        public float Braking { get; }
        /// <summary>m/s lost every second with nothing pressed.</summary>
        public float Coasting { get; }
        /// <summary>Top speed going backwards, m/s.</summary>
        public float ReverseSpeed { get; }
        /// <summary>Degrees per second the kart turns with the stick fully to one side.</summary>
        public float TurnRate { get; }
        /// <summary>Below this speed (m/s) it turns less: a stopped kart cannot spin on the spot.</summary>
        public float FullTurnSpeed { get; }

        public KartStats(float topSpeed, float acceleration, float braking, float coasting,
                         float reverseSpeed, float turnRate, float fullTurnSpeed)
        {
            TopSpeed = topSpeed;
            Acceleration = acceleration;
            Braking = braking;
            Coasting = coasting;
            ReverseSpeed = reverseSpeed;
            TurnRate = turnRate;
            FullTurnSpeed = fullTurnSpeed;
        }

        /// <summary>
        /// 25 m/s (90 km/h): a 1.5 km lap in about a minute. Reaches it in about two seconds and at
        /// top speed turns in circles of 16 m radius, the width of the road.
        /// </summary>
        public static KartStats Standard { get; } = new KartStats(
            topSpeed: 25f, acceleration: 12f, braking: 30f, coasting: 5f,
            reverseSpeed: 6f, turnRate: 90f, fullTurnSpeed: 8f);
    }
}
