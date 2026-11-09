namespace KartRace.Domain.Items
{
    /// <summary>
    /// What an item box gives you. It only looks at how far behind the leader you are: not your
    /// position, not who you are. That distance picks a band of the table, and the band's weights
    /// pick the item.
    /// The table measures in the game's units, which nobody has related to metres, and our track is
    /// not theirs. So we scale it to our lap: the last band that ends (26000 units, "very far") is
    /// half a lap. On a 1.5 km lap the bands end at about 12, 29, 58, 95, 160, 230, 375 and 750 m.
    /// That scale is our choice, not Nintendo's.
    /// </summary>
    public sealed class ItemRoulette
    {
        /// <summary>The distance of the table that we make half a lap.</summary>
        public const float FarUnits = 26000f, FarLapShare = 0.5f;

        public ItemTable Table { get; }
        /// <summary>Metres of our track per unit of the table.</summary>
        public float MetresPerUnit { get; }

        public ItemRoulette(ItemTable table, float lapLength)
        {
            Table = table;
            MetresPerUnit = lapLength * FarLapShare / FarUnits;
        }

        /// <summary>The band used by a racer that many metres behind the leader.</summary>
        public ItemTable.Band BandFor(float metresBehind) => Table.BandAt(metresBehind / MetresPerUnit);

        /// <summary>Chance of that item (0 … 1) for a racer that many metres behind the leader.</summary>
        public float Chance(Item item, float metresBehind) => BandFor(metresBehind).Chance(item);

        /// <summary>
        /// Spins: the item for a racer that many metres behind the leader. Roll is a random number
        /// in [0, 1) from outside, so the same roll always gives the same item (tests, recordings).
        /// </summary>
        public Item Pick(float metresBehind, double roll)
        {
            ItemTable.Band band = BandFor(metresBehind);
            double target = roll * band.Total;
            int sum = 0;
            foreach (var (item, weight) in band.Weights)
            {
                sum += weight;
                if (target < sum) return item;
            }
            return band.Weights[band.Weights.Count - 1].Item;   // only if roll was 1 or more
        }
    }
}
