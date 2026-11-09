namespace KartRace.Domain.Track
{
    /// <summary>
    /// Where each racer starts: two columns behind the start line, each place a bit further back
    /// than the one before (staggered, as in the games). Measured along the track line, so it works
    /// on any circuit whose start is on a straight.
    /// </summary>
    public static class StartingGrid
    {
        /// <summary>Metres between the start line and the first place.</summary>
        public const float FirstSlot = 3f;
        /// <summary>Metres between one place and the next (the next column, a bit further back).</summary>
        public const float Spacing = 3.2f;
        /// <summary>Metres from the centre line to each column.</summary>
        public const float Lane = 3f;

        /// <summary>
        /// Place 0 is the pole (left column). Distance is along the track line (negative: behind the
        /// start); lateral is to the right of the direction of travel.
        /// </summary>
        public static void Slot(int place, out float distance, out float lateral)
        {
            distance = -(FirstSlot + place * Spacing);
            lateral = place % 2 == 0 ? -Lane : Lane;
        }
    }
}
