using System;
using System.Numerics;

namespace KartRace.Domain.Driving
{
    /// <summary>
    /// Moves one kart on flat ground, seen from above (X right, Z forward, like TrackLine).
    /// Whoever drives it (you or the AI) only gives two numbers every frame: throttle and steering.
    /// </summary>
    public sealed class KartMotion
    {
        readonly KartStats stats;

        public Vector2 Position { get; private set; }
        /// <summary>Degrees, clockwise seen from above: 0 faces +Z, 90 faces +X (as Unity's yaw).</summary>
        public float Heading { get; private set; }
        /// <summary>m/s along the heading; negative going backwards.</summary>
        public float Speed { get; private set; }
        /// <summary>
        /// Multiplies the top speed. 1 for you, always; the rubber band raises it for a rival far
        /// behind, so its kart can go faster than yours. That is the cheat.
        /// </summary>
        public float TopSpeedScale { get; set; } = 1f;

        /// <summary>Set by whoever drives it every frame: 1 accelerate … -1 brake.</summary>
        public float Throttle { get; set; }
        /// <summary>Set by whoever drives it every frame: -1 left … 1 right.</summary>
        public float Steer { get; set; }

        public KartMotion(KartStats stats, Vector2 position, float heading)
        {
            this.stats = stats;
            Position = position;
            Heading = heading;
        }

        public Vector2 Forward
        {
            get
            {
                float radians = Heading * MathF.PI / 180f;
                return new Vector2(MathF.Sin(radians), MathF.Cos(radians));
            }
        }

        /// <summary>One step of the simulation with the controls its driver set.</summary>
        public void Step(float deltaTime) => Step(Throttle, Steer, deltaTime);

        /// <summary>
        /// A bump from another kart: moved by `push` and left going at `speed` along its heading.
        /// </summary>
        public void Bump(Vector2 push, float speed)
        {
            Position += push;
            Speed = Math.Clamp(speed, -stats.ReverseSpeed, stats.TopSpeed * MathF.Max(TopSpeedScale, 1f));
        }

        /// <summary>
        /// One step of the simulation.
        /// throttle: 1 accelerate, 0 nothing, -1 brake (and reverse once stopped).
        /// steer: -1 left, 1 right.
        /// </summary>
        public void Step(float throttle, float steer, float deltaTime)
        {
            throttle = Math.Clamp(throttle, -1f, 1f);
            steer = Math.Clamp(steer, -1f, 1f);

            Speed = NextSpeed(throttle, deltaTime);

            // It turns less when slow, and backwards the steering flips, like a real car.
            float grip = Math.Clamp(MathF.Abs(Speed) / stats.FullTurnSpeed, 0f, 1f);
            Heading += steer * stats.TurnRate * grip * MathF.Sign(Speed) * deltaTime;

            Position += Forward * Speed * deltaTime;
        }

        float NextSpeed(float throttle, float deltaTime)
        {
            if (throttle > 0f && Speed >= 0f)
                return MathF.Min(Speed + stats.Acceleration * throttle * deltaTime, stats.TopSpeed * TopSpeedScale);
            if (throttle < 0f && Speed > 0f)
                return MathF.Max(Speed + stats.Braking * throttle * deltaTime, 0f);
            if (throttle < 0f)
                return MathF.Max(Speed + stats.Acceleration * throttle * deltaTime, -stats.ReverseSpeed);
            if (throttle > 0f)   // going backwards and pressing forwards: brake first
                return MathF.Min(Speed + stats.Braking * throttle * deltaTime, 0f);
            // Nothing pressed: rolls to a stop.
            return Speed > 0f
                ? MathF.Max(Speed - stats.Coasting * deltaTime, 0f)
                : MathF.Min(Speed + stats.Coasting * deltaTime, 0f);
        }
    }
}
