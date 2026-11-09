using KartRace.Application.UseCases;
using KartRace.Domain.Items;
using KartRace.Domain.Race;
using UnityEngine;
using UnityEngine.UI;

namespace KartRace.Presentation
{
    /// <summary>
    /// What the next box would give you, live: one bar per item with its chance at your current
    /// distance to the leader, from the roulette you spin (players' or AI's). Drive away from the
    /// leader and watch the bananas shrink and the bullets grow. Every item keeps its row, even at
    /// 0 %, so the bars move instead of the list reshuffling. It builds its own canvas.
    /// </summary>
    public sealed class ItemOddsChart : MonoBehaviour
    {
        [SerializeField] ItemIcons icons;
        [SerializeField] bool visible = true;
        [Tooltip("Size, compared with a 1080p video.")]
        [SerializeField, Range(0.5f, 2f)] float size = 1f;
        [Tooltip("How fast the bars follow a change of band (per second).")]
        [SerializeField] float barSpeed = 4f;
        [SerializeField] Color barColor = new Color(1f, 0.78f, 0.2f);

        // Top: below the roulette's frame (40 + 170 units from the top) and its gap.
        const float RowHeight = 34f, IconSize = 30f, BarWidth = 150f, Top = 270f;

        // From the defensive items of the leader to the ones that win races from the back.
        static readonly Item[] Order =
        {
            Item.Coin, Item.Banana, Item.TripleBanana, Item.GreenPuck, Item.TripleGreenPuck, Item.Horn,
            Item.RedPuck, Item.TripleRedPuck, Item.Bomb, Item.Ink, Item.Turbo, Item.TripleTurbo,
            Item.BlueBomb, Item.GoldenTurbo, Item.Lightning, Item.Star, Item.Bullet,
        };

        sealed class Row
        {
            public Image Icon, Bar;
            public Text Percent;
            public float Shown;
        }

        readonly Row[] rows = new Row[Order.Length];
        OpenItemBox open;
        Standings standings;
        Racer racer;
        Canvas canvas;
        GameObject canvasObject;
        Camera view;
        Text header;

        public bool Visible { get => visible; set => visible = value; }

        /// <summary>With split screen: only over that player's camera.</summary>
        public void ShowIn(Camera camera)
        {
            view = camera;
            HudCanvas.Fit(canvas, view);
        }

        /// <summary>Whose odds to show (yours), and where the roulette and the distance come from.</summary>
        public void Show(OpenItemBox itemBoxes, Standings race, Racer you)
        {
            open = itemBoxes;
            standings = race;
            racer = you;
        }

        // In Start, not Awake: the race copies this object for player 2 in its Awake, and the copy
        // must not carry a canvas already built.
        void Start()
        {
            canvas = HudCanvas.Create(transform, "Item Odds");
            HudCanvas.Fit(canvas, view);
            canvasObject = canvas.gameObject;
            Transform parent = canvasObject.transform;

            // A dark panel behind it all, so it reads over the grass and the sky.
            Box(parent, "Panel", new Vector2(-25f, -Top + RowHeight + 16f),
                new Vector2(BarWidth + 200f, RowHeight * (Order.Length + 1) + 30f), new Color(0.08f, 0.09f, 0.14f, 0.6f), 1f);
            header = Label(parent, "Header", new Vector2(-40f, -Top + RowHeight + 6f), 26, TextAnchor.MiddleRight, 420f);
            for (int i = 0; i < Order.Length; i++)
            {
                float y = -Top - i * RowHeight;
                var row = new Row();
                // Right to left: percentage, bar (it grows to the right from its left end), icon.
                float barStart = -120f - BarWidth;
                var barSize = new Vector2(BarWidth, RowHeight - 12f);
                Box(parent, $"{Order[i]} Track", new Vector2(barStart, y - 6f), barSize, new Color(0f, 0f, 0f, 0.35f), 0f);
                row.Bar = Box(parent, $"{Order[i]} Bar", new Vector2(barStart, y - 6f), barSize, barColor, 0f);
                row.Percent = Label(parent, $"{Order[i]} %", new Vector2(-40f, y), 22, TextAnchor.MiddleRight, 70f);
                row.Icon = Box(parent, $"{Order[i]} Icon", new Vector2(barStart - 10f, y - 2f), Vector2.one * IconSize, Color.white, 1f);
                row.Icon.preserveAspect = true;
                rows[i] = row;
            }
        }

        void LateUpdate()
        {
            canvasObject.SetActive(visible && open != null);
            if (!canvasObject.activeSelf) return;

            float behind = standings.DistanceToLeader(racer);
            ItemRoulette roulette = open.RouletteOf(racer);
            ItemTable.Band band = roulette.BandFor(behind);
            int index = 0;
            while (roulette.Table.Bands[index] != band) index++;
            header.text = (behind < 0.5f ? "En cabeza" : $"Líder a {behind:0} m") +
                          $" · tramo {index + 1}/{roulette.Table.Bands.Count}";

            for (int i = 0; i < Order.Length; i++)
            {
                Row row = rows[i];
                float chance = band.Chance(Order[i]);
                row.Shown = Mathf.Lerp(row.Shown, chance, 1f - Mathf.Exp(-barSpeed * Time.deltaTime));
                // The longest bar of any band is about 45 %: that fills the track.
                row.Bar.rectTransform.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(row.Shown / 0.45f), RowHeight - 12f) * size;
                row.Percent.text = chance > 0f ? $"{chance * 100f:0} %" : "";
                if (row.Icon.sprite == null && icons != null) row.Icon.sprite = icons.Of(Order[i]);
                row.Icon.color = new Color(1f, 1f, 1f, chance > 0f ? 1f : 0.25f);   // out of reach: faded
            }
        }

        /// <summary>
        /// A plain rectangle anchored to the top right. `side` is the point of it that sits at `at`:
        /// 0 its left edge (it grows to the right), 1 its right edge.
        /// </summary>
        Image Box(Transform parent, string name, Vector2 at, Vector2 dimensions, Color color, float side)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(side, 1f);
            rect.anchoredPosition = at * size;
            rect.sizeDelta = dimensions * size;
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>White bold text with a dark outline, anchored to the top right.</summary>
        Text Label(Transform parent, string name, Vector2 at, int fontSize, TextAnchor alignment, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = at * size;
            rect.sizeDelta = new Vector2(width, RowHeight) * size;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.RoundToInt(fontSize * size);
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            go.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }
}
