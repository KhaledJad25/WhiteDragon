using UnityEngine;

namespace WhiteDragon
{
    /// <summary>A currency as data (coins, keys, ...). Create an asset in Data/Resources/Currencies; no code needed.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Currency", fileName = "Currency")]
    public class CurrencyDefinition : ScriptableObject
    {
        [Tooltip("Unique lowercase id, e.g. \"coins\". Pickups and items refer to the currency by this id.")]
        public string id;
        [Tooltip("Name shown on the HUD, e.g. \"Coins\".")]
        public string displayName;
        [Tooltip("Amount at the start of every run.")]
        [Min(0)]
        public int startAmount;
        [Tooltip("Most the player can hold. 0 = unlimited.")]
        [Min(0)]
        public int maxAmount;
        [Tooltip("Optional icon (stored only; the HUD shows text for now).")]
        public Sprite icon;

        /// <summary>value clamped to 0..maxAmount (no upper limit when maxAmount is 0).</summary>
        public int Clamp(int value) => Mathf.Max(0, maxAmount > 0 ? Mathf.Min(value, maxAmount) : value);
    }
}
