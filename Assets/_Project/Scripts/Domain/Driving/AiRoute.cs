using System;
using System.Numerics;
using KartRace.Domain.Track;

namespace KartRace.Domain.Driving
{
    /// <summary>
    /// The route the AI drives along: not the road, but a line drawn for it, with how much room
    /// there is on each side of it at every point, as the CPU routes of the real games (Mario Kart
    /// Wii's have a width per point [VERIFICAR]). The AI never looks at the asphalt: it only knows
    /// this line and its width.
    /// Ours is a racing line made from the centre of the road: on every curve it moves towards the
    /// inside, so the AI cuts the corners instead of driving down the middle.
    /// </summary>
    public sealed class AiRoute
    {
        readonly float[] roomLeft, roomRight;

        /// <summary>The line itself (TrackLine for its geometry; the race's ruler is still the centre line).</summary>
        public TrackLine Line { get; }

        public AiRoute(TrackLine line, float[] roomLeft, float[] roomRight)
        {
            if (roomLeft.Length != line.Count || roomRight.Length != line.Count)
                throw new ArgumentException("One room value per point of the line.");
            Line = line;
            this.roomLeft = roomLeft;
            this.roomRight = roomRight;
        }

        /// <summary>
        /// Metres a kart may drive to the left (negative) and right (positive) of the line there
        /// without leaving the road.
        /// </summary>
        public (float Left, float Right) RoomAt(float distance)
        {
            int i = Line.SegmentAt(distance, out float t);
            int j = (i + 1) % Line.Count;
            return (Lerp(roomLeft[i], roomLeft[j], t), Lerp(roomRight[i], roomRight[j], t));
        }

        /// <summary>The same room everywhere: a plain route (tests, or a road with no racing line).</summary>
        public static AiRoute Plain(TrackLine line, float room)
        {
            var left = new float[line.Count];
            var right = new float[line.Count];
            for (int i = 0; i < line.Count; i++) left[i] = right[i] = room;
            return new AiRoute(line, left, right);
        }

        /// <summary>
        /// A racing line from the centre of the road. At each point it measures how much the road
        /// turns around it (a stretch of `reach` metres each way), moves towards the inside in
        /// proportion, up to `cut` of the room there is, and then smooths the result so it flows
        /// from one side to the other. The room is what is left to each edge, minus `margin`.
        /// </summary>
        public static AiRoute RacingLine(TrackLine centre, float roadWidth, float margin = 1.5f,
                                         float cut = 0.75f, float reach = 40f)
        {
            int n = centre.Count;
            float half = roadWidth * 0.5f - margin;
            float spacing = centre.Length / n;
            int window = Math.Max(1, (int)(reach / spacing));

            // How much the road turns at each point: + to the right, - to the left (radians per metre).
            var bend = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 before = Vector2.Normalize(centre[i] - centre[(i - 1 + n) % n]);
                Vector2 after = Vector2.Normalize(centre[(i + 1) % n] - centre[i]);
                float cross = before.X * after.Y - before.Y * after.X;   // negative turning right
                bend[i] = -MathF.Asin(Math.Clamp(cross, -1f, 1f)) / spacing;
            }
            bend = Smooth(bend, window);

            // A curve of 40 m radius (0.025 rad/m, a tight corner of ours) already uses all of it.
            const float FullBend = 0.025f;
            var offset = new float[n];
            for (int i = 0; i < n; i++) offset[i] = Math.Clamp(bend[i] / FullBend, -1f, 1f) * half * cut;
            offset = Smooth(offset, window / 2);

            var points = new Vector2[n];
            var left = new float[n];
            var right = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 along = Vector2.Normalize(centre[(i + 1) % n] - centre[i]);
                Vector2 rightward = new Vector2(along.Y, -along.X);
                points[i] = centre[i] + rightward * offset[i];
                left[i] = half + offset[i];    // from the line to the left edge
                right[i] = half - offset[i];   // and to the right one
            }
            return new AiRoute(new TrackLine(points), left, right);
        }

        /// <summary>Average over `window` points each way, round the loop.</summary>
        static float[] Smooth(float[] values, int window)
        {
            int n = values.Length;
            var result = new float[n];
            for (int i = 0; i < n; i++)
            {
                float sum = 0f;
                for (int k = -window; k <= window; k++) sum += values[((i + k) % n + n) % n];
                result[i] = sum / (2 * window + 1);
            }
            return result;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
