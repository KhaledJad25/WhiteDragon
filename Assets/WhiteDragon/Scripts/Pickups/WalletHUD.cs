using System.Text;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>One IMGUI line under the hearts: "Coins: 12   Keys: 0". The text is rebuilt only when the wallet changes.</summary>
    public class WalletHUD : MonoBehaviour
    {
        public PlayerWallet wallet;
        public float gap = 6f;
        public Color textColor = new Color(0.85f, 0.8f, 0.72f);

        string line;
        GUIStyle style;
        PlayerWallet subscribed;

        void Awake()
        {
            if (wallet == null) wallet = GetComponent<PlayerWallet>();
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (wallet == null || subscribed == wallet) return;
            Unsubscribe();
            subscribed = wallet;
            subscribed.Changed += OnChanged;
            line = null;
        }

        void Unsubscribe()
        {
            if (subscribed != null) subscribed.Changed -= OnChanged;
            subscribed = null;
        }

        void OnChanged(string currencyId) => line = null;

        /// <summary>The line shown, e.g. "Coins: 12   Keys: 0" (rebuilt only after a change).</summary>
        public string Line
        {
            get
            {
                Subscribe();
                if (line != null) return line;
                var sb = new StringBuilder();
                if (wallet != null)
                    foreach (var c in wallet.Currencies)
                        sb.Append(sb.Length > 0 ? "   " : "").Append(string.IsNullOrEmpty(c.displayName) ? c.id : c.displayName)
                          .Append(": ").Append(wallet.Get(c.id));
                line = sb.ToString();
                return line;
            }
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || wallet == null) return;
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = textColor } };
            GUI.Label(new Rect(16f, HeartsHUD.BottomY + gap, 400f, 22f), Line, style);
        }
    }
}
