namespace KartRace.Domain.Items
{
    /// <summary>
    /// The item a racer carries. One at a time: while the roulette spins or you hold an item, the
    /// boxes you drive through give you nothing. The item is decided the moment you take the box;
    /// the spin is only the show before you get it.
    /// </summary>
    public sealed class ItemSlot
    {
        float spinLeft;

        /// <summary>What the roulette gave, already decided while it still spins (null if none).</summary>
        public Item? Rolled { get; private set; }
        /// <summary>The item in your hands, once the roulette has stopped (null while it spins).</summary>
        public Item? Held => spinLeft > 0f ? null : Rolled;
        public bool Spinning => spinLeft > 0f;
        public bool Empty => Rolled == null;

        /// <summary>The roulette starts on that item and stops after that many seconds.</summary>
        public void Spin(Item item, float seconds)
        {
            Rolled = item;
            spinLeft = seconds;
        }

        public void Tick(float deltaTime)
        {
            if (spinLeft > 0f) spinLeft -= deltaTime;
        }

        /// <summary>Uses the item in your hands: returns it and the slot is empty (null if you had none).</summary>
        public Item? Use()
        {
            Item? item = Held;
            if (item != null) Rolled = null;
            return item;
        }
    }
}
