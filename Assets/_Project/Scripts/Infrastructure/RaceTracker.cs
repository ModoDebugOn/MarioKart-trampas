using System.Collections.Generic;
using KartRace.Domain.Race;
using UnityEngine;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// Keeps the race up to date: every frame, after the karts have moved, asks the track how far
    /// along the lap each one is and re-sorts the standings. The bootstrap creates it.
    /// </summary>
    [DefaultExecutionOrder(-10)]   // its LateUpdate before the HUD reads the standings
    public sealed class RaceTracker : MonoBehaviour
    {
        TrackPath track;
        readonly Dictionary<KartBody, Racer> racers = new Dictionary<KartBody, Racer>();

        public Standings Standings { get; private set; }

        public void Begin(TrackPath path, IEnumerable<KartBody> karts)
        {
            track = path;
            float lap = track.Line.Length;
            foreach (KartBody kart in karts)
            {
                var progress = RaceProgress.OnGrid(lap, track.DistanceAlong(kart.transform.position));
                racers[kart] = new Racer(kart.Character, progress);
            }
            Standings = new Standings(racers.Values);
        }

        public Racer RacerOf(KartBody kart) => racers[kart];

        void LateUpdate()
        {
            if (Standings == null) return;
            foreach (var pair in racers)
                pair.Value.Progress.Update(track.DistanceAlong(pair.Key.transform.position));
            Standings.Update();
        }
    }
}
