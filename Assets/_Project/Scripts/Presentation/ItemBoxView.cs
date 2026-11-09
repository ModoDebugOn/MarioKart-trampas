using KartRace.Domain.Items;
using UnityEngine;

namespace KartRace.Presentation
{
    /// <summary>
    /// An item box on the track: a see-through cube with a "?" that turns, floats and slowly goes
    /// through the colours of the rainbow. When someone takes it, it shrinks away; when it comes
    /// back, it pops up again. It only reads the box; whoever builds it says which one.
    /// </summary>
    public sealed class ItemBoxView : MonoBehaviour
    {
        [SerializeField] Renderer cube;
        [Tooltip("Degrees per second it turns.")]
        [SerializeField] float spin = 70f;
        [Tooltip("Metres it floats up and down.")]
        [SerializeField] float bob = 0.15f;
        [Tooltip("Seconds it takes to shrink away or pop back.")]
        [SerializeField] float popTime = 0.2f;
        [Tooltip("How long a full turn of the rainbow takes, in seconds.")]
        [SerializeField] float rainbowTime = 6f;

        ItemBox box;
        Vector3 size, rest;
        float shown = 1f, phase;
        MaterialPropertyBlock block;

        public void Show(ItemBox itemBox) => box = itemBox;

        void Awake()
        {
            size = cube.transform.localScale;
            rest = cube.transform.localPosition;
            phase = Random.value;   // so the boxes of a row are not all the same colour
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (box == null) return;
            shown = Mathf.MoveTowards(shown, box.Present ? 1f : 0f, Time.deltaTime / popTime);
            cube.enabled = shown > 0f;
            if (!cube.enabled) return;

            float t = Time.time;
            cube.transform.localScale = size * Mathf.SmoothStep(0f, 1f, shown);
            cube.transform.localPosition = rest + Vector3.up * bob * Mathf.Sin(t * 2f + phase * 6f);
            cube.transform.localRotation = Quaternion.Euler(20f, t * spin + phase * 360f, 10f);

            Color tint = Color.HSVToRGB(Mathf.Repeat(t / rainbowTime + phase, 1f), 0.45f, 1f);
            cube.GetPropertyBlock(block);
            block.SetColor("_Color", tint);
            cube.SetPropertyBlock(block);
        }
    }
}
