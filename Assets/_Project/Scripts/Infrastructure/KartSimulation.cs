using System.Collections.Generic;
using KartRace.Application.UseCases;
using UnityEngine;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// Moves every kart once a frame, after their drivers have set the controls: the use case
    /// steps them all and settles the bumps, then each transform is put where its kart ended up.
    /// The race creates it.
    /// </summary>
    public sealed class KartSimulation : MonoBehaviour
    {
        AdvanceKarts advance;
        IReadOnlyList<KartBody> bodies;

        public void Begin(AdvanceKarts advanceKarts, IReadOnlyList<KartBody> karts)
        {
            advance = advanceKarts;
            bodies = karts;
        }

        // Drivers run at -10, the race tracker reads the transforms in LateUpdate: this sits between.
        void Update()
        {
            if (advance == null) return;
            advance.Execute(Time.deltaTime);
            foreach (KartBody body in bodies) body.Sync();
        }
    }
}
