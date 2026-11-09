using UnityEngine;
using UnityEngine.UI;

namespace KartRace.Presentation
{
    /// <summary>
    /// Two players on the same screen, each with their own camera: player 1 on top, player 2 below,
    /// as in Mario Kart's two players. Always one above the other: side by side, each half of the
    /// 16:9 video would be narrow and the HUD would pile up; this way it shrinks but keeps its
    /// layout, and in a 9:16 reel each half is almost square. A thin line separates the halves:
    /// half of it drawn by each camera, along its edge (the recording kit only records what the
    /// cameras draw). With one camera it just fills the screen.
    /// </summary>
    public sealed class SplitScreen : MonoBehaviour
    {
        [SerializeField] Color lineColor = new Color(0.05f, 0.05f, 0.08f);
        [Tooltip("Thickness of the line between the halves, in HUD units (1080 = the short side of the picture).")]
        [SerializeField, Range(0f, 20f)] float lineWidth = 6f;

        Camera first, second;
        RectTransform lowerEdge, upperEdge;   // of player 1's half, and of player 2's

        public void Show(Camera playerOne, Camera playerTwo)
        {
            first = playerOne;
            second = playerTwo;
        }

        void LateUpdate()
        {
            if (first == null) return;
            if (second == null)
            {
                first.rect = new Rect(0f, 0f, 1f, 1f);
                if (lowerEdge != null) lowerEdge.gameObject.SetActive(false);
                if (upperEdge != null) upperEdge.gameObject.SetActive(false);
                return;
            }

            first.rect = new Rect(0f, 0.5f, 1f, 0.5f);    // player 1 on top
            second.rect = new Rect(0f, 0f, 1f, 0.5f);
            if (lowerEdge == null) lowerEdge = Edge(first, 0f);
            if (upperEdge == null) upperEdge = Edge(second, 1f);
            foreach (RectTransform edge in new[] { lowerEdge, upperEdge })
            {
                edge.gameObject.SetActive(lineWidth > 0f);
                edge.sizeDelta = new Vector2(0f, lineWidth * 0.5f);
            }
        }

        /// <summary>A strip across that camera's half, along its bottom (y 0) or its top (y 1).</summary>
        RectTransform Edge(Camera camera, float y)
        {
            Canvas canvas = HudCanvas.Create(transform, "Split Line");
            HudCanvas.Fit(canvas, camera);
            canvas.sortingOrder = 10;
            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            var edge = (RectTransform)go.transform;
            edge.SetParent(canvas.transform, false);
            edge.anchorMin = new Vector2(0f, y);
            edge.anchorMax = new Vector2(1f, y);
            edge.pivot = new Vector2(0.5f, y);
            edge.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = lineColor;
            return edge;
        }
    }
}
