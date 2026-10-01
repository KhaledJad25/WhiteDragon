using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Small center dot with a dark outline, drawn with IMGUI.</summary>
    public class Crosshair : MonoBehaviour
    {
        public float dotSize = 4f;
        public float outline = 1f;
        public Color dotColor = new Color(0.95f, 0.92f, 0.85f);
        public Color outlineColor = new Color(0f, 0f, 0f, 0.85f);

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            float outer = dotSize + outline * 2f;
            var old = GUI.color;
            GUI.color = outlineColor;
            GUI.DrawTexture(new Rect(cx - outer * 0.5f, cy - outer * 0.5f, outer, outer), Texture2D.whiteTexture);
            GUI.color = dotColor;
            GUI.DrawTexture(new Rect(cx - dotSize * 0.5f, cy - dotSize * 0.5f, dotSize, dotSize), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
