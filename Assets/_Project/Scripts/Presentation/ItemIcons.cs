using KartRace.Domain.Items;
using UnityEngine;

namespace KartRace.Presentation
{
    /// <summary>
    /// The icon of every item (Textures/Items), in the order of the Item enum. One asset that the
    /// roulette and the odds chart share; the item box builder fills it.
    /// </summary>
    public sealed class ItemIcons : ScriptableObject
    {
        [SerializeField] Sprite[] sprites = new Sprite[0];

        public Sprite Of(Item item) => (int)item < sprites.Length ? sprites[(int)item] : null;

        public int Count => sprites.Length;

        /// <summary>Used by the builder: one sprite per item, in enum order.</summary>
        public void Fill(Sprite[] icons) => sprites = icons;
    }
}
