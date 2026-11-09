using KartRace.Domain.Driving;
using KartRace.Domain.Race;
using UnityEngine;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// The kart in the scene: holds its KartMotion and copies where it is to the transform.
    /// It does not read any input: a driver (you or the AI) fills Throttle and Steer. It does not
    /// move itself either: the KartSimulation moves every kart at once, so they can bump into each other.
    /// The kart builder puts it on every racer prefab.
    /// </summary>
    public sealed class KartBody : MonoBehaviour
    {
        [SerializeField] Character character;
        KartMotion motion;

        public Character Character => character;
        /// <summary>Made on first use, where the kart stands: the race may need it before this Awake runs.</summary>
        public KartMotion Motion => motion ??= new KartMotion(KartStats.Standard,
            new System.Numerics.Vector2(transform.position.x, transform.position.z), transform.eulerAngles.y);

        /// <summary>Set by the driver every frame. 1 accelerate … -1 brake.</summary>
        public float Throttle { get => Motion.Throttle; set => Motion.Throttle = value; }
        /// <summary>Set by the driver every frame. -1 left … 1 right.</summary>
        public float Steer { get => Motion.Steer; set => Motion.Steer = value; }

        /// <summary>Used by the kart builder to say who sits in it.</summary>
        public void Assign(Character who) => character = who;

        /// <summary>Puts the transform where the simulation left the kart.</summary>
        public void Sync()
        {
            var position = Motion.Position;
            transform.SetPositionAndRotation(
                new Vector3(position.X, transform.position.y, position.Y),
                Quaternion.Euler(0f, Motion.Heading, 0f));
        }
    }
}
