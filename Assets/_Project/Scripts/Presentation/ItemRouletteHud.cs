using KartRace.Domain.Items;
using KartRace.Domain.Race;
using UnityEngine;
using UnityEngine.UI;

namespace KartRace.Presentation
{
    /// <summary>
    /// Your item, in a frame in the top right corner. While the roulette spins the icons fly past;
    /// when it stops, the one you got stays. The item was decided the moment you took the box: the
    /// spin is only the show. It only reads your slot; it builds its own canvas.
    /// </summary>
    public sealed class ItemRouletteHud : MonoBehaviour
    {
        [SerializeField] ItemIcons icons;
        [Tooltip("Size, compared with a 1080p video.")]
        [SerializeField, Range(0.5f, 2f)] float size = 1f;
        [Tooltip("Seconds each icon stays while the roulette spins.")]
        [SerializeField] float flick = 0.07f;

        Racer racer;
        Image icon;
        RectTransform iconRect;
        float nextFlick, pop;
        int flicking;
        Item? shown;
        Canvas canvas;
        Camera view;

        public void Show(Racer you) => racer = you;

        /// <summary>With split screen: only over that player's camera.</summary>
        public void ShowIn(Camera camera)
        {
            view = camera;
            HudCanvas.Fit(canvas, view);
        }

        // In Start, not Awake: the race copies this object for player 2 in its Awake, and the copy
        // must not carry a canvas already built.
        void Start()
        {
            canvas = HudCanvas.Create(transform, "Item Roulette");
            HudCanvas.Fit(canvas, view);

            // Dark frame with a white border, anchored to the top right.
            RectTransform border = Box(canvas.transform, "Frame", new Color(1f, 1f, 1f, 0.9f), 170f);
            border.anchoredPosition = new Vector2(-40f, -40f) * size;
            RectTransform back = Box(border, "Back", new Color(0.08f, 0.09f, 0.14f, 0.75f), 158f);
            back.anchorMin = back.anchorMax = back.pivot = new Vector2(0.5f, 0.5f);
            back.anchoredPosition = Vector2.zero;

            iconRect = Box(back, "Icon", Color.white, 140f);
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            icon = iconRect.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false;
        }

        void LateUpdate()
        {
            if (racer == null || icons == null) return;
            ItemSlot slot = racer.Slot;
            if (slot.Spinning)
            {
                // Any icon but the same twice in a row: it reads as spinning, not as flickering.
                if (Time.time >= nextFlick)
                {
                    flicking = (flicking + 1 + Random.Range(0, icons.Count - 1)) % icons.Count;
                    nextFlick = Time.time + flick;
                    Set(icons.Of((Item)flicking));
                }
                shown = null;
            }
            else if (slot.Held != shown)
            {
                shown = slot.Held;
                Set(shown != null ? icons.Of(shown.Value) : null);
                if (shown != null) pop = 1f;   // it lands with a little bounce
            }
            pop = Mathf.MoveTowards(pop, 0f, Time.deltaTime * 4f);
            iconRect.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(pop * Mathf.PI));
        }

        void Set(Sprite sprite)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        RectTransform Box(Transform parent, string name, Color color, float side)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = Vector2.one * side * size;
            go.GetComponent<Image>().color = color;
            return rect;
        }
    }
}
