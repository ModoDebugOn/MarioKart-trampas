using KartRace.Domain.Driving;
using UnityEngine;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// The AI at the wheel of a kart: asks its AiPilot for throttle and steering every frame.
    /// The race puts it on every kart you are not driving.
    /// The circuit sits at the origin, so the route and the kart share the same X, Z.
    /// </summary>
    [RequireComponent(typeof(KartBody))]
    [DefaultExecutionOrder(-10)]   // before the kart moves this frame
    public sealed class AiDriver : MonoBehaviour
    {
        KartBody kart;

        public AiPilot Pilot { get; private set; }

        public void Begin(AiPilot pilot) => Pilot = pilot;

        void Awake() => kart = GetComponent<KartBody>();

        void Update()
        {
            if (Pilot == null) return;
            Pilot.Drive(kart.Motion, Time.deltaTime, out float throttle, out float steer);
            kart.Throttle = throttle;
            kart.Steer = steer;
        }
    }
}
