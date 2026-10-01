using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>One item. Create an asset in Data/Resources/Items and fill in the fields; no code needed.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique lowercase id, e.g. \"iron_tooth\". Never change it once the item is in use.")]
        public string id;
        [Tooltip("Name shown to the player on the pedestal.")]
        public string displayName;
        [Tooltip("Short line shown under the name on the pedestal.")]
        [TextArea] public string description;
        [Tooltip("Lowercase words that synergies count, e.g. \"rock\", \"fire\". Several items sharing a tag can trigger a synergy.")]
        public string[] tags = new string[0];

        [Header("Effects")]
        [Tooltip("Stat changes while held. Flat adds, PercentAdd adds percent (0.25 = +25%), Multiply multiplies (1.5 = x1.5).")]
        public StatModifier[] statModifiers = new StatModifier[0];
        [Tooltip("Changes to every throw: extra rocks, spread, pierce, size, damage type.")]
        public RecipeEdits recipeEdits = RecipeEdits.Default;
        [Tooltip("Shot effect assets (homing, burn, split...). Drag them from Data/Resources/Effects. No empty slots.")]
        public List<ShotEffect> effects = new List<ShotEffect>();

        [Header("Pools")]
        [Tooltip("Rarer tiers appear less often; Luck makes them more likely.")]
        public ItemRarity rarity = ItemRarity.Common;
        [Tooltip("Lowercase pool IDs, compared case-insensitively. Type a new one to make a new pool.")]
        public string[] poolIds = { "normal" };
        [Tooltip("Higher = more common within its rarity. 0 = never appears. 1 = normal.")]
        [Min(0f)]
        public float weightMultiplier = 1f;
        [Tooltip("Empty means always available. Otherwise the item can only appear after this hidden unlock.")]
        public string requiredUnlockId = "";

        [Header("Art (optional)")]
        [Tooltip("2D icon for future UI (HUD, item lists). Not shown anywhere yet.")]
        public Sprite icon;
        [Tooltip("Model floating on the pedestal, replacing the rarity-colored sphere. Centered on its pivot, about 0.5 m across. Empty = sphere.")]
        public GameObject worldPrefab;

        /// <summary>One line describing what the item does, for tools and debugging.</summary>
        public string Summary()
        {
            string text = DescribeEffects(statModifiers, recipeEdits, effects);
            if (!string.IsNullOrEmpty(requiredUnlockId))
                text += (text.Length > 0 ? ", " : "") + $"needs unlock '{requiredUnlockId}'";
            return text.Length > 0 ? text : "no effect";
        }

        internal static string DescribeEffects(StatModifier[] mods, RecipeEdits edits, List<ShotEffect> shotEffects)
        {
            var parts = new List<string>();
            if (mods != null)
                foreach (var m in mods)
                    parts.Add(m.kind == ModifierKind.Flat ? $"{Signed(m.value)} {m.stat}"
                        : m.kind == ModifierKind.PercentAdd ? $"{Signed(m.value * 100f)}% {m.stat}"
                        : $"x{Num(m.value)} {m.stat}");
            if (edits.projectileCountAdd != 0) parts.Add($"{Signed(edits.projectileCountAdd)} rocks");
            if (edits.spreadAddDegrees != 0f) parts.Add($"{Signed(edits.spreadAddDegrees)}° spread");
            if (edits.pierceAdd != 0) parts.Add($"{Signed(edits.pierceAdd)} pierce");
            if (!Mathf.Approximately(edits.sizeMultiplier, 1f)) parts.Add($"x{Num(edits.sizeMultiplier)} size");
            if (edits.overrideDamageType) parts.Add($"{edits.damageType} damage");
            if (shotEffects != null)
                foreach (var e in shotEffects) parts.Add(e != null ? e.name : "(missing effect)");
            return string.Join(", ", parts);
        }

        static string Signed(float v) => (v >= 0f ? "+" : "") + Num(v);
        static string Num(float v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        public bool HasTag(string tag) => Contains(tags, tag);
        public bool InPool(string poolId) => Contains(poolIds, poolId);

        static bool Contains(string[] values, string value)
        {
            if (values == null || string.IsNullOrEmpty(value)) return false;
            foreach (var v in values)
                if (string.Equals(v, value, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
