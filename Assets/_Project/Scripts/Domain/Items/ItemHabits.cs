namespace KartRace.Domain.Items
{
    /// <summary>
    /// How a rival plays the item in its hands, which is part of how good it is:
    /// - Reaction: seconds before it uses anything (a clumsy rival sits on its items).
    /// - Tactics: it waits for the right moment. It drops a trap when someone is right behind it
    ///   (they drive into it), throws a puck or a bomb when there is someone ahead to hit, and
    ///   keeps the horn for the blue bomb. After Patience seconds it uses it anyway.
    /// The rest (boosts, star, bullet…) it uses as soon as it reacts. How Mario Kart's CPU decides
    /// is not documented: this is how it is usually done.
    /// </summary>
    public sealed class ItemHabits
    {
        /// <summary>Metres behind it that count as "right behind" (a trap would hit them).</summary>
        public const float Behind = 15f;
        /// <summary>Metres ahead it can reach with a puck or a bomb.</summary>
        public const float Reach = 30f;
        /// <summary>Seconds it keeps an item waiting for its moment; then it uses it anyway.</summary>
        public const float Patience = 10f;

        /// <summary>Seconds it holds any item before using it.</summary>
        public float Reaction { get; }
        /// <summary>Waits for the right moment (true) or uses everything as it comes (false).</summary>
        public bool Tactics { get; }

        public ItemHabits(float reaction, bool tactics)
        {
            Reaction = reaction;
            Tactics = tactics;
        }

        /// <param name="heldFor">Seconds it has had the item in its hands.</param>
        /// <param name="behind">Metres to the nearest racer behind it (float.MaxValue if it is last).</param>
        /// <param name="ahead">Metres to the nearest racer ahead of it (float.MaxValue if it leads).</param>
        public bool ShouldUse(Item item, float heldFor, float behind, float ahead)
        {
            if (heldFor < Reaction) return false;
            if (!Tactics || heldFor >= Reaction + Patience) return true;
            switch (item)
            {
                case Item.Banana:
                case Item.TripleBanana:
                    return behind <= Behind;
                case Item.GreenPuck:
                case Item.TripleGreenPuck:
                case Item.RedPuck:
                case Item.TripleRedPuck:
                case Item.Bomb:
                    return ahead <= Reach;
                case Item.Horn:
                    return false;   // the only thing that stops the blue bomb: it waits
                default:
                    return true;
            }
        }
    }
}
