using UnityEngine;
using UnityEngine.UI;

namespace KartRace.Presentation
{
    /// <summary>
    /// The canvas every HUD builds for itself, drawn by a player's camera (the recording kit only
    /// records what the cameras draw: an overlay canvas would not show in the video). With split
    /// screen it only covers that player's half.
    /// Sized against the short side of the picture: 1080 pixels are 1080 units, in a 16:9 video and
    /// in a 9:16 reel alike. So the HUD is just as big in both, and the reel's narrow 1080 is still
    /// wide enough for the left corner and the right one (scaling by height, it would be 1.8 times
    /// bigger there and the two corners would run into each other).
    /// </summary>
    static class HudCanvas
    {
        public static Canvas Create(Transform parent, string name)
        {
            var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;   // the smaller of width/1080 and height/1080
            return canvas;
        }

        /// <summary>Over that camera's part of the screen, or over all of it if there is none.</summary>
        public static void Fit(Canvas canvas, Camera camera)
        {
            if (canvas == null) return;
            if (camera == null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                return;
            }
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.05f;   // in front of everything
        }
    }
}
