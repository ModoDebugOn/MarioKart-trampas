using KartRace.Domain.Track;
using UnityEngine;

namespace KartRace.Infrastructure
{
    /// <summary>
    /// Lives on the circuit prefab and keeps its centre line (in the circuit's local X, Z), the same
    /// one the road mesh was built from. The race asks it for the Domain's TrackLine.
    /// The circuit builder fills it; nobody edits it by hand.
    /// </summary>
    public sealed class TrackPath : MonoBehaviour
    {
        [SerializeField] Vector2[] centreLine = new Vector2[0];
        [SerializeField] float roadWidth = 16f;

        TrackLine line;

        public float RoadWidth => roadWidth;

        public TrackLine Line
        {
            get
            {
                if (line != null) return line;
                var points = new System.Numerics.Vector2[centreLine.Length];
                for (int i = 0; i < points.Length; i++) points[i] = new System.Numerics.Vector2(centreLine[i].x, centreLine[i].y);
                return line = new TrackLine(points);
            }
        }

        /// <summary>Point of the line in world space (on the ground).</summary>
        public Vector3 WorldPoint(System.Numerics.Vector2 p) => transform.TransformPoint(new Vector3(p.X, 0f, p.Y));

        /// <summary>The point of the road that far along the lap, lateral metres to the right of the line.</summary>
        public Vector3 WorldAt(float distance, float lateral) =>
            WorldPoint(Line.PointAt(distance) + Line.RightAt(distance) * lateral);

        /// <summary>How far along the lap a point of the world is (TrackLine.DistanceAlong, in metres).</summary>
        public float DistanceAlong(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return Line.DistanceAlong(new System.Numerics.Vector2(local.x, local.z));
        }

        /// <summary>Used by the circuit builder to save the line it built the road from.</summary>
        public void Store(TrackLine track, float width)
        {
            centreLine = new Vector2[track.Count];
            for (int i = 0; i < track.Count; i++) centreLine[i] = new Vector2(track[i].X, track[i].Y);
            roadWidth = width;
            line = null;
        }
    }
}
