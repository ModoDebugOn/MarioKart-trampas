using System;
using System.Collections.Generic;
using System.Numerics;

namespace KartRace.Domain.Track
{
    /// <summary>
    /// The centre line of a closed circuit, seen from above (X, Z). It is the ruler of the race:
    /// how far along the lap each racer is, and so who is first and how far behind the others are.
    /// Both cheats of the video read it: the item roulette (distance to the leader) and the rubber
    /// banding (distance between a rival and you).
    ///
    /// It is a closed polyline: the last point joins the first one. Distance 0 is the first point
    /// (the start line) and distances wrap around every lap.
    /// </summary>
    public sealed class TrackLine
    {
        readonly Vector2[] points;
        readonly float[] startDistance;   // distance along the line at which each segment starts

        public float Length { get; }
        public int Count => points.Length;
        public Vector2 this[int index] => points[index];

        public TrackLine(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 3) throw new ArgumentException("A closed track needs at least 3 points.");
            this.points = new Vector2[points.Count];
            startDistance = new float[points.Count];
            float distance = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                this.points[i] = points[i];
                startDistance[i] = distance;
                distance += Vector2.Distance(points[i], points[(i + 1) % points.Count]);
            }
            Length = distance;
        }

        /// <summary>
        /// A smooth closed line through every control point (Catmull-Rom spline), sampled about
        /// every `spacing` metres. The first control point stays at distance 0.
        /// </summary>
        public static TrackLine Smooth(IReadOnlyList<Vector2> controlPoints, float spacing)
        {
            int n = controlPoints.Count;
            var samples = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = controlPoints[(i - 1 + n) % n], p1 = controlPoints[i];
                Vector2 p2 = controlPoints[(i + 1) % n], p3 = controlPoints[(i + 2) % n];
                int steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(p1, p2) / spacing));
                for (int s = 0; s < steps; s++)
                    samples.Add(CatmullRom(p0, p1, p2, p3, s / (float)steps));
            }
            return new TrackLine(samples);
        }

        /// <summary>Wraps any distance (negative, or more than a lap) into [0, Length).</summary>
        public float Wrap(float distance)
        {
            float d = distance % Length;
            return d < 0f ? d + Length : d;
        }

        /// <summary>The point of the line at that distance from the start.</summary>
        public Vector2 PointAt(float distance)
        {
            int i = SegmentAt(distance, out float t);
            return Vector2.Lerp(points[i], points[(i + 1) % points.Length], t);
        }

        /// <summary>Which way the track goes at that distance (unit vector).</summary>
        public Vector2 DirectionAt(float distance)
        {
            int i = SegmentAt(distance, out _);
            return Vector2.Normalize(points[(i + 1) % points.Length] - points[i]);
        }

        /// <summary>Unit vector to the right of the direction of travel (X right, Z forward).</summary>
        public Vector2 RightAt(float distance)
        {
            Vector2 d = DirectionAt(distance);
            return new Vector2(d.Y, -d.X);
        }

        /// <summary>
        /// How far along the lap a position is: the distance of the closest point of the line.
        /// A racer anywhere on the road (not only on the line) gets the progress of its slice.
        /// </summary>
        public float DistanceAlong(Vector2 position)
        {
            float best = float.MaxValue, result = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length];
                Vector2 ab = b - a;
                float t = Math.Clamp(Vector2.Dot(position - a, ab) / ab.LengthSquared(), 0f, 1f);
                float squared = Vector2.DistanceSquared(position, a + ab * t);
                if (squared < best)
                {
                    best = squared;
                    result = startDistance[i] + ab.Length() * t;
                }
            }
            return result;
        }

        /// <summary>The segment (from point index to index + 1) at that distance, and how far along it (0 … 1).</summary>
        public int SegmentAt(float distance, out float t)
        {
            float d = Wrap(distance);
            int index = Array.BinarySearch(startDistance, d);
            if (index < 0) index = ~index - 1;
            float length = Vector2.Distance(points[index], points[(index + 1) % points.Length]);
            t = length > 0f ? (d - startDistance[index]) / length : 0f;
            return index;
        }

        /// <summary>
        /// Centripetal Catmull-Rom between p1 and p2 (t from 0 to 1). "Centripetal" spaces the knots
        /// by the square root of the distance between points, so it never makes loops or sharp tips
        /// when the control points are unevenly spaced (the plain one does).
        /// </summary>
        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float k0 = 0f;
            float k1 = k0 + Knot(p0, p1);
            float k2 = k1 + Knot(p1, p2);
            float k3 = k2 + Knot(p2, p3);
            float k = k1 + (k2 - k1) * t;

            Vector2 a1 = Mix(p0, p1, k0, k1, k), a2 = Mix(p1, p2, k1, k2, k), a3 = Mix(p2, p3, k2, k3, k);
            Vector2 b1 = Mix(a1, a2, k0, k2, k), b2 = Mix(a2, a3, k1, k3, k);
            return Mix(b1, b2, k1, k2, k);
        }

        static float Knot(Vector2 a, Vector2 b) => MathF.Max(MathF.Sqrt(Vector2.Distance(a, b)), 1e-4f);

        static Vector2 Mix(Vector2 a, Vector2 b, float ka, float kb, float k) =>
            (kb - k) / (kb - ka) * a + (k - ka) / (kb - ka) * b;
    }
}
