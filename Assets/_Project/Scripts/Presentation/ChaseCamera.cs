using UnityEngine;

namespace KartRace.Presentation
{
    /// <summary>
    /// The usual racing-game camera: behind and above the kart, catching up with it smoothly so
    /// turns are felt instead of the whole world snapping round.
    /// Looking back (key V), it rises high in front of the kart and looks down at the road behind
    /// it: your kart at the bottom of the picture and the pack chasing you up the rest, with their
    /// bands. The rubber band happens behind you, where the usual camera never looks; in a 9:16 reel
    /// the tall picture fits about a hundred metres of pack.
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float distance = 9f;
        [SerializeField] float height = 4f;
        [SerializeField] float lookHeight = 1.2f;
        [Tooltip("How quickly it catches up (1/s): higher is stiffer.")]
        [SerializeField] float follow = 6f;

        [Header("Looking back (key V)")]
        [SerializeField] bool lookBack;
        [Tooltip("Metres above the kart.")]
        [SerializeField] float backHeight = 30f;
        [Tooltip("Metres in front of the kart.")]
        [SerializeField] float backAhead = 20f;
        [Tooltip("Metres behind the kart of the point it looks at: more, and the kart sits lower in the picture " +
                 "(in a reel, keep it out of the bottom fifth: Instagram's caption covers it).")]
        [SerializeField] float backLookAt = 4f;

        public Transform Target
        {
            get => target;
            set { target = value; Snap(); }
        }

        public bool LookBack
        {
            get => lookBack;
            set { lookBack = value; Snap(); }
        }

        void LateUpdate()
        {
            if (target == null) return;
            float t = 1f - Mathf.Exp(-follow * Time.deltaTime);   // same smoothing at any frame rate
            transform.position = Vector3.Lerp(transform.position, Desired(), t);
            transform.LookAt(LookAt());
        }

        void Snap()
        {
            if (target == null) return;
            transform.position = Desired();
            transform.LookAt(LookAt());
        }

        Vector3 Desired() => lookBack
            ? target.position + target.forward * backAhead + Vector3.up * backHeight
            : target.position - target.forward * distance + Vector3.up * height;

        Vector3 LookAt() => lookBack
            ? target.position - target.forward * backLookAt
            : target.position + Vector3.up * lookHeight;
    }
}
