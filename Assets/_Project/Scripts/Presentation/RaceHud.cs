using KartRace.Domain.Race;
using UnityEngine;
using UnityEngine.UI;

namespace KartRace.Presentation
{
    /// <summary>
    /// Your race in a corner of the screen: position, lap and how far the leader is (the number
    /// the item roulette will read), or, if you lead, how far the second is. It only reads the standings; it builds its own canvas.
    /// </summary>
    public sealed class RaceHud : MonoBehaviour
    {
        [Tooltip("Size, compared with a 1080p video.")]
        [SerializeField, Range(0.5f, 2f)] float size = 1f;

        Standings standings;
        Racer player;
        Text position, lap, leader, cheats, chase;
        Canvas canvas;
        Camera view;

        public void Show(Standings race, Racer you)
        {
            standings = race;
            player = you;
        }

        /// <summary>With split screen: only over that player's camera.</summary>
        public void ShowIn(Camera camera)
        {
            view = camera;
            HudCanvas.Fit(canvas, view);
        }

        /// <summary>The rubber band line: on or off, and at which difficulty (in its colour).</summary>
        public void ShowRubberBand(bool on, string difficulty, Color color)
        {
            cheats.text = on ? $"Goma: activada · <color=#{ColorUtility.ToHtmlStringRGB(color)}>{difficulty}</color>"
                             : "Goma: desactivada";
        }

        /// <summary>With two players, who the rivals chase ("al primero"…); empty hides the line.</summary>
        public void ShowChase(string rule) => chase.text = string.IsNullOrEmpty(rule) ? "" : $"Persiguen {rule}";

        // In Start, not Awake: the race copies this object for player 2 in its Awake, and the copy
        // must not carry a canvas already built.
        void Start()
        {
            canvas = HudCanvas.Create(transform, "Race HUD");
            Transform parent = canvas.transform;
            position = Label(parent, 0f, 96);
            lap = Label(parent, -110f, 44);
            leader = Label(parent, -165f, 44);
            cheats = Label(parent, -220f, 44);
            chase = Label(parent, -275f, 44);
            HudCanvas.Fit(canvas, view);
        }

        void LateUpdate()
        {
            if (standings == null) return;
            position.text = $"{standings.PositionOf(player)}.º / {standings.Order.Count}";
            lap.text = $"Vuelta {Mathf.Max(1, player.Progress.Lap)}";
            float behind = standings.DistanceToLeader(player);
            // Leading, how far the second is: with the band off it grows and grows.
            leader.text = behind >= 0.5f ? $"Líder a {behind:0} m"
                        : standings.Order.Count > 1 ? $"2.º a {-player.GapTo(standings.Order[1]):0} m"
                        : "Vas en cabeza";
        }

        /// <summary>A line of white bold text with a dark outline, anchored to the top left.</summary>
        Text Label(Transform parent, float y, int fontSize)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -40f + y) * size;
            rect.sizeDelta = new Vector2(600f, fontSize * 1.3f) * size;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.RoundToInt(fontSize * size);
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            go.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            return text;
        }
    }
}
