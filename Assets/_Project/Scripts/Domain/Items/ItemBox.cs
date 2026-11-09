namespace KartRace.Domain.Items
{
    /// <summary>
    /// An item box on the track. Whoever drives through it while it is there takes it; then it is
    /// gone for RespawnTime seconds. What it gives is not up to the box but to the roulette.
    /// </summary>
    public sealed class ItemBox
    {
        float wait;

        /// <summary>Seconds it takes to come back after someone takes it.</summary>
        public float RespawnTime { get; set; }
        public bool Present => wait <= 0f;

        public ItemBox(float respawnTime) => RespawnTime = respawnTime;

        /// <summary>Drives through it: true if it was there (and now it is gone for a while).</summary>
        public bool Take()
        {
            if (!Present) return false;
            wait = RespawnTime;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (wait > 0f) wait -= deltaTime;
        }
    }
}
