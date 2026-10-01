using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Draws hearts in the top-left with IMGUI: red containers, then soul and dark overlay hearts.</summary>
    public class HeartsHUD : MonoBehaviour
    {
        public PlayerHealth health;
        public Vector2 origin = new Vector2(16f, 16f);
        public float heartSize = 28f;
        public float spacing = 4f;
        public int perRow = 6;

        public Color redColor = new Color(0.8f, 0.05f, 0.08f);
        public Color soulColor = new Color(0.3f, 0.5f, 1f);
        public Color darkColor = new Color(0.3f, 0.08f, 0.4f);
        public Color emptyColor = new Color(0.15f, 0.12f, 0.12f, 0.9f);
        public Color outlineColor = new Color(0f, 0f, 0f, 0.9f);

        /// <summary>Lowest screen y the hearts may reach (useful for laying out other UI).</summary>
        public static float BottomY { get; private set; }
        /// <summary>Rightmost screen x the hearts reach.</summary>
        public static float RightX { get; private set; }

        Texture2D heart;

        void Awake()
        {
            if (health == null) health = GetComponent<PlayerHealth>();
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || health == null) return;
            if (heart == null) heart = MakeHeartTexture(32);
            var s = health.State;
            int slot = 0;

            for (int c = 0; c < s.RedContainers; c++)
            {
                int halves = Mathf.Clamp(s.Red - c * 2, 0, 2);
                DrawHeart(slot++, redColor, halves, true);
            }
            foreach (var h in s.Overlay)
                DrawHeart(slot++, h.Kind == HeartKind.Soul ? soulColor : darkColor, h.Halves, false);

            int rows = Mathf.Max(1, Mathf.CeilToInt(slot / (float)perRow));
            BottomY = origin.y + rows * (heartSize + spacing);
            RightX = origin.x + Mathf.Min(slot, perRow) * (heartSize + spacing);
        }

        void DrawHeart(int slot, Color color, int halves, bool isContainer)
        {
            float x = origin.x + (slot % perRow) * (heartSize + spacing);
            float y = origin.y + (slot / perRow) * (heartSize + spacing);
            var rect = new Rect(x, y, heartSize, heartSize);
            var old = GUI.color;

            GUI.color = outlineColor;
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, heartSize + 4f, heartSize + 4f), heart);
            if (isContainer)
            {
                GUI.color = emptyColor;
                GUI.DrawTexture(rect, heart);
            }
            GUI.color = color;
            if (halves >= 2)
                GUI.DrawTexture(rect, heart);
            else if (halves == 1)
                GUI.DrawTextureWithTexCoords(new Rect(x, y, heartSize * 0.5f, heartSize), heart, new Rect(0f, 0f, 0.5f, 1f));
            GUI.color = old;
        }

        static Texture2D MakeHeartTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * 2.6f - 1.3f;
                float y = (py + 0.5f) / size * 2.6f - 1.15f;
                float a = x * x + y * y - 1f;
                bool inside = a * a * a - x * x * y * y * y <= 0f;
                pixels[py * size + px] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        void OnDestroy()
        {
            if (heart != null) Destroy(heart);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            BottomY = 0f;
            RightX = 0f;
        }
    }
}
