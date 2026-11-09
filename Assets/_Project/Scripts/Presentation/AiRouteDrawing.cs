using KartRace.Domain.Driving;
using UnityEngine;

namespace KartRace.Presentation
{
    /// <summary>
    /// The AI's route, drawn on the road: the racing line it follows (yellow) and the edges of the
    /// width it may use (white). What the AI "sees" instead of the asphalt. A layer that switches on
    /// and off for the video; off at the start.
    /// </summary>
    public sealed class AiRouteDrawing : MonoBehaviour
    {
        [SerializeField] Material lineMaterial;
        [SerializeField] bool visible;
        [SerializeField] Color lineColor = new Color(1f, 0.85f, 0.1f, 0.9f);
        [SerializeField] Color edgeColor = new Color(1f, 1f, 1f, 0.6f);

        const float Height = 0.1f, Step = 2f;   // just over the road, a point every 2 m

        LineRenderer line, left, right;

        public bool Visible { get => visible; set => visible = value; }

        /// <summary>The route, and the circuit whose local X, Z it is drawn in.</summary>
        public void Show(AiRoute route, Transform circuit)
        {
            int count = Mathf.CeilToInt(route.Line.Length / Step);
            line = Strip("AI Route", 0.4f, lineColor, count);
            left = Strip("AI Route Left Edge", 0.15f, edgeColor, count);
            right = Strip("AI Route Right Edge", 0.15f, edgeColor, count);
            for (int i = 0; i < count; i++)
            {
                float d = i * Step;
                var (roomLeft, roomRight) = route.RoomAt(d);
                line.SetPosition(i, OnRoute(route, circuit, d, 0f));
                left.SetPosition(i, OnRoute(route, circuit, d, -roomLeft));
                right.SetPosition(i, OnRoute(route, circuit, d, roomRight));
            }
        }

        void LateUpdate()
        {
            if (line == null) return;
            line.enabled = left.enabled = right.enabled = visible;
        }

        static Vector3 OnRoute(AiRoute route, Transform circuit, float distance, float lateral)
        {
            System.Numerics.Vector2 p = route.Line.PointAt(distance) + route.Line.RightAt(distance) * lateral;
            return circuit.TransformPoint(new Vector3(p.X, Height, p.Y));
        }

        /// <summary>A closed line lying flat on the road (it faces up, not the camera).</summary>
        LineRenderer Strip(string name, float width, Color color, int points)
        {
            var strip = new GameObject(name).AddComponent<LineRenderer>();
            strip.transform.SetParent(transform, false);
            strip.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            strip.alignment = LineAlignment.TransformZ;
            strip.sharedMaterial = lineMaterial;
            strip.useWorldSpace = true;
            strip.loop = true;
            strip.positionCount = points;
            strip.widthMultiplier = width;
            strip.startColor = strip.endColor = color;
            strip.sortingOrder = 2;   // over the rubber band's zone
            strip.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            strip.receiveShadows = false;
            return strip;
        }
    }
}
