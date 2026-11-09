using KartRace.Domain.Driving;
using UnityEngine;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// A player at the wheel: passes that player's demo input to the kart. The race puts it on the
    /// kart of each player. With the autopilot on, the kart drives itself like a rival (but the
    /// rubber band never touches it), so you can watch the race without driving.
    /// </summary>
    [RequireComponent(typeof(KartBody))]
    [DefaultExecutionOrder(-10)]   // before the kart moves this frame
    public sealed class PlayerDriver : MonoBehaviour
    {
        KartBody kart;

        /// <summary>Which player drives it: 0 the first, 1 the second (whose keys it reads).</summary>
        public int Player { get; set; }
        public AiPilot Autopilot { get; set; }
        public bool UseAutopilot { get; set; }

        void Awake() => kart = GetComponent<KartBody>();

        void Update()
        {
            if (UseAutopilot && Autopilot != null)
            {
                Autopilot.Drive(kart.Motion, Time.deltaTime, out float throttle, out float steer);
                kart.Throttle = throttle;
                kart.Steer = steer;
                return;
            }
            kart.Motion.TopSpeedScale = 1f;   // back to a normal kart if the autopilot was faster
            kart.Throttle = DemoInput.Throttle(Player);
            kart.Steer = DemoInput.Steer(Player);
        }
    }
}
