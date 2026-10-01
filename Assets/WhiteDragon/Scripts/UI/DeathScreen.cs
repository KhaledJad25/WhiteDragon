using UnityEngine;

namespace WhiteDragon
{
    /// <summary>"You died" overlay. R starts a new run by reloading the scene.</summary>
    public class DeathScreen : MonoBehaviour
    {
        public PlayerHealth health;
        public Color overlayColor = new Color(0.05f, 0f, 0f, 0.75f);

        GUIStyle titleStyle, textStyle;
        bool restarting;

        void Awake()
        {
            if (health == null) health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (restarting || health == null || !health.State.IsDead) return;
            if (!GameInput.Restart.WasPressedThisFrame()) return;
            restarting = true;
            SceneReloader.ReloadActive();
        }

        void OnGUI()
        {
            if (health == null || !health.State.IsDead) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                titleStyle.normal.textColor = new Color(0.75f, 0.05f, 0.05f);
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
                textStyle.normal.textColor = new Color(0.85f, 0.8f, 0.75f);
            }

            var old = GUI.color;
            GUI.color = overlayColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;

            float cy = Screen.height * 0.5f;
            GUI.Label(new Rect(0f, cy - 120f, Screen.width, 80f), "YOU DIED", titleStyle);
            GUI.Label(new Rect(0f, cy - 20f, Screen.width, 30f), StatsLine(), textStyle);
            GUI.Label(new Rect(0f, cy + 30f, Screen.width, 30f), "Press R to start a new run", textStyle);
        }

        static string StatsLine() => $"Seed {RunSession.Seed}     Rooms cleared {RunSession.RoomsCleared}";
    }
}
