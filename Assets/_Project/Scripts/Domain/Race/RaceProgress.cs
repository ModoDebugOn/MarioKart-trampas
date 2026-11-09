using System;

namespace KartRace.Domain.Race
{
    /// <summary>
    /// How far a racer has gone in the whole race, not only in this lap. The track line gives the
    /// distance inside the lap, which jumps back to 0 at the finish line every lap; this class adds
    /// up the steps between frames, so the total keeps growing and anyone a lap down is a lap behind.
    ///
    /// Total 0 is the start line on lap 1. The grid is behind it, so racers start with a negative total.
    /// </summary>
    public sealed class RaceProgress
    {
        readonly float lapLength;
        float lastAlong;   // distance inside the lap on the last update

        /// <summary>Metres since the start line, counting every lap. Negative on the grid.</summary>
        public float Total { get; private set; }

        /// <summary>Lap being driven: 1 from the first time the start line is crossed, 0 on the grid.</summary>
        public int Lap => (int)MathF.Floor(Total / lapLength) + 1;

        /// <summary>Metres inside the current lap, from 0 to the lap length.</summary>
        public float DistanceInLap => Total - (Lap - 1) * lapLength;

        public RaceProgress(float lapLength, float startTotal)
        {
            if (lapLength <= 0f) throw new ArgumentException("The lap must have some length.");
            this.lapLength = lapLength;
            Total = startTotal;
            lastAlong = Wrap(startTotal);
        }

        /// <summary>
        /// Progress of a racer waiting on the grid, from where the track line says it is. The grid is
        /// just behind the start line, so anything in the second half of the lap is behind it
        /// (37 m on a 40 m lap is -3 m), and anything in the first half is already past it.
        /// </summary>
        public static RaceProgress OnGrid(float lapLength, float along) =>
            new RaceProgress(lapLength, along > lapLength / 2f ? along - lapLength : along);

        /// <summary>
        /// New distance inside the lap (what TrackLine.DistanceAlong gives). If it jumped more than
        /// half a lap, the racer crossed the finish line: forwards when it fell (39 m to 1 m is +2 m),
        /// backwards when it rose (1 m to 39 m is -2 m). Nobody drives half a lap in one frame.
        /// </summary>
        public void Update(float along)
        {
            float step = along - lastAlong;
            if (step < -lapLength / 2f) step += lapLength;
            else if (step > lapLength / 2f) step -= lapLength;
            Total += step;
            lastAlong = along;
        }

        float Wrap(float distance)
        {
            float d = distance % lapLength;
            return d < 0f ? d + lapLength : d;
        }
    }
}
