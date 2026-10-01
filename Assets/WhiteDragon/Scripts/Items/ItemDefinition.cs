using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>One item. Create an asset in Data/Resources/Items and fill in the fields; no code needed.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public string[] tags = new string[0];

        [Header("Effects")]
        public StatModifier[] statModifiers = new StatModifier[0];
        public RecipeEdits recipeEdits = RecipeEdits.Default;
        public List<ShotEffect> effects = new List<ShotEffect>();

        [Header("Pools")]
        public ItemRarity rarity = ItemRarity.Common;
        [Tooltip("Lowercase pool IDs, compared case-insensitively. Type a new one to make a new pool.")]
        public string[] poolIds = { "normal" };
        public float weightMultiplier = 1f;
        [Tooltip("Empty means always available.")]
        public string requiredUnlockId = "";

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
