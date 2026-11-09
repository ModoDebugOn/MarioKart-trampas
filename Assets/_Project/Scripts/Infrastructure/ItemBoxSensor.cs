using System.Collections.Generic;
using KartRace.Application.UseCases;
using KartRace.Domain.Items;
using KartRace.Domain.Race;
using UnityEngine;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// Notices a kart driving through an item box and tells the use case. The karts have no physics
    /// (KartMotion moves them), so instead of triggers it measures: a kart within Radius metres of
    /// a box that is there opens it. 20 karts and a handful of boxes: cheap enough every frame.
    /// </summary>
    public sealed class ItemBoxSensor
    {
        readonly OpenItemBox open;
        readonly IReadOnlyList<(Vector3 At, ItemBox Box)> boxes;
        readonly IReadOnlyList<(Transform Kart, Racer Racer)> karts;
        readonly float radius;

        public ItemBoxSensor(OpenItemBox open, IReadOnlyList<(Vector3 At, ItemBox Box)> boxes,
                             IReadOnlyList<(Transform Kart, Racer Racer)> karts, float radius)
        {
            this.open = open;
            this.boxes = boxes;
            this.karts = karts;
            this.radius = radius;
        }

        public void Detect()
        {
            foreach (var (at, box) in boxes)
            {
                if (!box.Present) continue;
                foreach (var (kart, racer) in karts)
                {
                    Vector3 d = kart.position - at;
                    if (d.x * d.x + d.z * d.z > radius * radius) continue;
                    open.Execute(racer, box);
                    break;   // first one in takes it
                }
            }
        }
    }
}
