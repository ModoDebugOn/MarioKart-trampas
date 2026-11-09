using System.Collections.Generic;
using KartRace.Domain.Driving;

namespace KartRace.Application.UseCases
{
    /// <summary>
    /// One frame of every kart, all at once: each one moves with the controls its driver set, then
    /// the ones that ended up touching bump each other, and the traffic (where everyone is on the
    /// AI's route) is measured again for the drivers of the next frame.
    /// </summary>
    public sealed class AdvanceKarts
    {
        readonly IReadOnlyList<KartMotion> karts;
        readonly Traffic traffic;

        /// <summary>Radius of the circle a kart takes up, seen from above (metres).</summary>
        public float Radius { get; set; } = 1.1f;
        /// <summary>0 karts stick together when they touch, 1 they rebound fully.</summary>
        public float Bounce { get; set; } = 0.3f;
        public bool Collisions { get; set; } = true;

        public AdvanceKarts(IReadOnlyList<KartMotion> karts, Traffic traffic)
        {
            this.karts = karts;
            this.traffic = traffic;
        }

        public void Execute(float deltaTime)
        {
            foreach (KartMotion kart in karts) kart.Step(deltaTime);
            if (Collisions) KartCollisions.Resolve(karts, Radius, Bounce);
            traffic.Update();
        }
    }
}
