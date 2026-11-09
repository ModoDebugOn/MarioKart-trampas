using System.Collections.Generic;
using KartRace.Application;
using KartRace.Domain.Cheats;
using KartRace.Domain.Track;
using UnityEngine;
using UnityEngine.UI;

namespace KartRace.Presentation
{
    /// <summary>
    /// The invisible rubber band, made visible, in layers that switch on and off for the video:
    /// - Bands: a line from every rival to what it chases: a human's kart, or a spot on the track
    ///   when the chase rule chases no racer (the average of two players). Slack, it sags like a real
    ///   rubber band; the more it stretches the straighter and thicker it gets. Grey when slack,
    ///   orange-red when the rival is speeding up, blue when it is holding back.
    /// - Pace: over each nearby rival, how much faster or slower than normal it goes (+24 %).
    /// - Zone: on the road, where the rivals want to be compared with what they chase. A bright strip
    ///   at the band's Lead (± its dead zone), which moves with the difficulty, and a faint one over
    ///   the whole stretch the pack spreads over (each rival's own slack); one pair per spot being
    ///   chased (two when the pack splits between two players). Hidden with the band off.
    /// Drawn with LineRenderers and a canvas, so the camera sees it and the recording kit records it.
    /// It only reads the race; whoever builds it tells it who to draw.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after the camera has moved, so the labels sit on the karts
    public sealed class RubberBandDrawing : MonoBehaviour
    {
        [SerializeField] Material lineMaterial;

        [Header("Layers")]
        [SerializeField] bool showBands = true;
        [SerializeField] bool showPace = true;
        [SerializeField] bool showZone = true;

        [Header("Look")]
        [SerializeField] Color slackColor = new Color(0.85f, 0.85f, 0.85f, 0.7f);
        [SerializeField] Color boostColor = new Color(1f, 0.35f, 0.1f);
        [SerializeField] Color holdBackColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] Color spotColor = new Color(0.3f, 1f, 0.4f, 0.55f);
        [SerializeField] Color packColor = new Color(1f, 1f, 1f, 0.18f);
        [Tooltip("Metres the band hangs down in the middle when completely slack.")]
        [SerializeField, Range(0f, 1.2f)] float sag = 1f;
        [Tooltip("Rivals further than this from the camera (metres) get no label, so the screen does not fill up.")]
        [SerializeField, Range(10f, 300f)] float labelDistance = 80f;
        [Tooltip("Size of the labels, compared with a 1080p video.")]
        [SerializeField, Range(0.5f, 2f)] float labelSize = 1f;

        const int Points = 16;
        const float Height = 1.2f;   // the band goes from seat to seat, not along the ground
        const float ZoneHeight = 0.08f, ZoneStep = 2f;   // just over the road, a point every 2 m

        sealed class Rival
        {
            public Transform Kart;
            public RubberBanding.Rival Band;
            public LineRenderer Line;
            public Text Label;
        }

        /// <summary>The two strips laid around one spot being chased.</summary>
        sealed class Zone
        {
            public LineRenderer Spot, Pack;
        }

        readonly List<Rival> rivals = new List<Rival>();
        readonly List<Zone> zones = new List<Zone>();
        readonly List<float> chasedSpots = new List<float>();
        IReadOnlyList<Transform> humans;
        RubberBanding banding;
        TrackLine track;
        Transform circuit;
        float roadWidth;
        RectTransform labels;
        Canvas labelCanvas;
        Font font;

        public bool ShowBands { get => showBands; set => showBands = value; }
        public bool ShowPace { get => showPace; set => showPace = value; }
        public bool ShowZone { get => showZone; set => showZone = value; }

        /// <summary>
        /// The band that pulls the rivals (its numbers decide the colours and the zone, its rule who
        /// they chase), the karts of its humans in the same order as its list, and the track the
        /// zone is laid on (its line is in the circuit's local X, Z).
        /// </summary>
        public void Begin(RubberBanding rubberBanding, IReadOnlyList<Transform> humanKarts, TrackLine line,
                          Transform lineSpace, float width)
        {
            banding = rubberBanding;
            humans = humanKarts;
            track = line;
            circuit = lineSpace;
            roadWidth = width;
        }

        public void AddRival(Transform kart, RubberBanding.Rival rival)
        {
            var band = Line($"{kart.name} Band");
            band.positionCount = Points;
            band.numCapVertices = 2;
            rivals.Add(new Rival { Kart = kart, Band = rival, Line = band, Label = Label(kart.name) });
        }

        void LateUpdate()
        {
            if (banding == null) return;
            RubberBand band = banding.Band;
            DrawZones(band);
            Camera camera = Camera.main;
            if (labelCanvas != null && camera != null && labelCanvas.worldCamera != camera) HudCanvas.Fit(labelCanvas, camera);
            foreach (Rival rival in rivals)
            {
                float pace = rival.Band.Pilot.Pace;
                float tension = Tension(pace, band);
                Color color = pace > 1f ? Color.Lerp(slackColor, boostColor, tension * 4f)   // colour shows up early
                            : pace < 1f ? Color.Lerp(slackColor, holdBackColor, tension * 4f)
                            : slackColor;
                DrawBand(rival, tension, color);
                DrawLabel(rival, pace, color, camera);
            }
        }

        /// <summary>How far along the race the spot a rival chases is (metres, laps included).</summary>
        static float ChasedSpot(RubberBanding.Rival rival) => rival.Racer.Progress.Total + rival.Gap;

        /// <summary>
        /// Where the band of a rival ends: on the kart of the human it chases, or, if the spot is no
        /// racer's (the average of the humans), on the track at that distance.
        /// </summary>
        Vector3 Chased(RubberBanding.Rival rival)
        {
            float spot = ChasedSpot(rival);
            for (int i = 0; i < humans.Count; i++)
                if (Mathf.Abs(banding.Humans[i].Progress.Total - spot) < 0.5f) return humans[i].position;
            return OnTrack(spot, 0f);
        }

        /// <summary>0 slack … 1 fully stretched (the rival at its maximum boost or hold back).</summary>
        static float Tension(float pace, RubberBand band)
        {
            if (pace > 1f && band.MaxBoost > 0f) return Mathf.Clamp01((pace - 1f) / band.MaxBoost);
            if (pace < 1f && band.MaxHoldBack > 0f) return Mathf.Clamp01((1f - pace) / band.MaxHoldBack);
            return 0f;
        }

        void DrawBand(Rival rival, float tension, Color color)
        {
            LineRenderer line = rival.Line;
            line.enabled = showBands;
            if (!showBands) return;
            Vector3 from = Chased(rival.Band) + Vector3.up * Height;
            Vector3 to = rival.Kart.position + Vector3.up * Height;
            float hang = sag * (1f - tension);
            for (int i = 0; i < Points; i++)
            {
                float t = i / (Points - 1f);
                // A parabola: nothing at the ends, the most in the middle.
                line.SetPosition(i, Vector3.Lerp(from, to, t) + Vector3.down * hang * 4f * t * (1f - t));
            }
            line.widthMultiplier = Mathf.Lerp(0.12f, 0.4f, tension);
            line.startColor = line.endColor = color;
        }

        /// <summary>
        /// For every spot being chased (one per human chased, or the average): two strips on the
        /// road, measured from it along the track. Where the rivals want to be (Lead ± dead zone),
        /// and how far the pack spreads around it (the widest slack behind and ahead).
        /// </summary>
        void DrawZones(RubberBand band)
        {
            chasedSpots.Clear();
            if (showZone && banding.Enabled)
                foreach (Rival rival in rivals)
                {
                    float spot = ChasedSpot(rival.Band);
                    if (!chasedSpots.Exists(other => Mathf.Abs(other - spot) < 1f)) chasedSpots.Add(spot);
                }

            while (zones.Count < chasedSpots.Count)
            {
                var zone = new Zone
                {
                    Pack = Strip("Pack Zone", roadWidth, packColor),
                    Spot = Strip("Spot Zone", roadWidth, spotColor),
                };
                zone.Spot.sortingOrder = 1;   // over the pack strip it sits in
                zones.Add(zone);
            }
            for (int i = 0; i < zones.Count; i++)
            {
                bool used = i < chasedSpots.Count;
                zones[i].Spot.enabled = zones[i].Pack.enabled = used;
                if (!used) continue;
                float from = chasedSpots[i] + band.Lead - band.DeadZone, to = chasedSpots[i] + band.Lead + band.DeadZone;
                LayAlong(zones[i].Spot, from, to);
                LayAlong(zones[i].Pack, from - banding.PackLength, to + banding.PackAhead);
            }
        }

        /// <summary>The point of the road that far along the track (metres, laps included), lateral metres to the right.</summary>
        Vector3 OnTrack(float distance, float lateral)
        {
            System.Numerics.Vector2 p = track.PointAt(distance) + track.RightAt(distance) * lateral;
            return circuit.TransformPoint(new Vector3(p.X, 0f, p.Y));
        }

        /// <summary>Lays a strip on the road from one distance of the track to another.</summary>
        void LayAlong(LineRenderer strip, float from, float to)
        {
            int count = Mathf.Max(2, Mathf.CeilToInt((to - from) / ZoneStep) + 1);
            strip.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                System.Numerics.Vector2 p = track.PointAt(Mathf.Lerp(from, to, i / (count - 1f)));
                strip.SetPosition(i, circuit.TransformPoint(new Vector3(p.X, ZoneHeight, p.Y)));
            }
        }

        /// <summary>A strip as wide as the road, lying flat on it (it faces up, not the camera).</summary>
        LineRenderer Strip(string name, float width, Color color)
        {
            LineRenderer strip = Line(name);
            strip.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            strip.alignment = LineAlignment.TransformZ;
            strip.widthMultiplier = width;
            strip.startColor = strip.endColor = color;
            return strip;
        }

        /// <summary>An unlit line in world space that casts no shadow.</summary>
        LineRenderer Line(string name)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        void DrawLabel(Rival rival, float pace, Color color, Camera camera)
        {
            Vector3 head = rival.Kart.position + Vector3.up * (Height + 1.6f);
            Vector3 screen = camera != null ? camera.WorldToScreenPoint(head) : Vector3.back;
            bool visible = showPace && screen.z > 0f
                        && Vector3.Distance(rival.Kart.position, camera.transform.position) < labelDistance;
            rival.Label.enabled = visible;
            if (!visible) return;

            float percent = (pace - 1f) * 100f;
            rival.Label.text = Mathf.Abs(percent) < 0.5f ? "0 %" : $"{percent:+0;−0} %";
            rival.Label.color = color;
            // The canvas covers only the camera's part of the screen (its half, with split screen).
            rival.Label.rectTransform.anchoredPosition = ((Vector2)screen - camera.pixelRect.min) / labelCanvas.scaleFactor;
        }

        /// <summary>A label on a canvas of its own, over everything (bold, dark outline).</summary>
        Text Label(string name)
        {
            if (labels == null)
            {
                labelCanvas = HudCanvas.Create(transform, "Pace Labels");   // drawn by the camera, in LateUpdate
                labelCanvas.sortingOrder = -1;   // under the HUD when a rival passes behind it
                labels = (RectTransform)labelCanvas.transform;
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            var go = new GameObject($"{name} Pace", typeof(RectTransform), typeof(Text), typeof(Outline));
            var rect = (RectTransform)go.transform;
            rect.SetParent(labels, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;   // positions in screen pixels from the bottom left
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(200f, 50f) * labelSize;

            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = Mathf.RoundToInt(36 * labelSize);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.LowerCenter;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            go.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }
}
