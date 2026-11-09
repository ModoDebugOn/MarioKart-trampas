using System;
using System.Collections.Generic;
using System.Numerics;

namespace KartRace.Domain.Driving
{
    /// <summary>
    /// Karts bumping into each other, arcade style. Seen from above every kart is a circle; when
    /// two overlap they are pushed apart, and if they were closing in they share the blow: the one
    /// that hits loses speed, the one that is hit gains it. Only the part of the speed along each
    /// kart's own heading stays (a kart does not slide sideways). Every kart weighs the same.
    /// </summary>
    public static class KartCollisions
    {
        /// <summary>
        /// Separates every pair of karts closer than two radii. Bounce: 0 they stick together
        /// (the one behind just matches the speed of the one in front), 1 a perfect rebound.
        /// Returns how many pairs touched.
        /// </summary>
        public static int Resolve(IReadOnlyList<KartMotion> karts, float radius, float bounce = 0.3f)
        {
            int touches = 0;
            float reach = radius * 2f;
            for (int i = 0; i < karts.Count; i++)
            for (int j = i + 1; j < karts.Count; j++)
            {
                KartMotion a = karts[i], b = karts[j];
                Vector2 d = b.Position - a.Position;
                float distance = d.Length();
                if (distance >= reach) continue;
                touches++;
                // Exactly on top of each other: push them apart sideways.
                Vector2 normal = distance > 1e-4f ? d / distance : new Vector2(a.Forward.Y, -a.Forward.X);
                float overlap = reach - distance;

                Vector2 va = a.Forward * a.Speed, vb = b.Forward * b.Speed;
                float closing = Vector2.Dot(vb - va, normal);   // negative: coming together
                if (closing < 0f)
                {
                    float impulse = -(1f + bounce) * closing * 0.5f;   // equal weights: half each
                    va -= normal * impulse;
                    vb += normal * impulse;
                }
                a.Bump(-normal * overlap * 0.5f, Vector2.Dot(va, a.Forward));
                b.Bump(normal * overlap * 0.5f, Vector2.Dot(vb, b.Forward));
            }
            return touches;
        }
    }
}
