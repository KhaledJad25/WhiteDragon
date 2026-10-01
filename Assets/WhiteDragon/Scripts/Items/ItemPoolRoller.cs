using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Picks items from a pool: filter (pool, exclusions, locks), roll a rarity (Luck-adjusted),
    /// then an item within it by weightMultiplier. Deterministic for a given RunRandom. Never throws.
    /// </summary>
    public class ItemPoolRoller
    {
        public const string FallbackPool = "normal";
        public const float MinLuck = -5f, MaxLuck = 10f;
        static readonly float[] BaseRarityWeights = { 60f, 28f, 10f, 2f };

        readonly List<ItemDefinition> items;
        readonly Func<string, bool> isUnlocked;

        public ItemPoolRoller(IEnumerable<ItemDefinition> items, Func<string, bool> isUnlocked = null)
        {
            this.items = (items ?? Enumerable.Empty<ItemDefinition>())
                .Where(i => i != null)
                .OrderBy(i => i.id ?? "", StringComparer.Ordinal)
                .ToList();
            this.isUnlocked = isUnlocked ?? Unlocks.Has;
        }

        /// <summary>weight x max(0.2, 1 + 0.12 x Luck x tier), Luck clamped to [-5, 10], tier Common = 0.</summary>
        public static float RarityWeight(ItemRarity rarity, float luck)
        {
            int tier = (int)rarity;
            float baseWeight = tier < BaseRarityWeights.Length ? BaseRarityWeights[tier] : 1f;
            float l = Mathf.Clamp(luck, MinLuck, MaxLuck);
            return baseWeight * Mathf.Max(0.2f, 1f + 0.12f * l * tier);
        }

        public ItemDefinition Roll(string poolId, float luck, RunRandom rng, ICollection<string> excludedIds = null)
        {
            if (rng == null) return null;
            var pick = RollFrom(poolId, luck, rng, excludedIds);
            if (pick == null && !string.Equals(poolId, FallbackPool, StringComparison.OrdinalIgnoreCase))
                pick = RollFrom(FallbackPool, luck, rng, excludedIds);
            return pick;
        }

        public List<ItemDefinition> RollDistinct(string poolId, int count, float luck, RunRandom rng, IEnumerable<string> excludedIds = null)
        {
            var result = new List<ItemDefinition>();
            var excluded = new HashSet<string>(excludedIds ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++)
            {
                var pick = Roll(poolId, luck, rng, excluded);
                if (pick == null) break;
                result.Add(pick);
                excluded.Add(pick.id ?? "");
            }
            return result;
        }

        ItemDefinition RollFrom(string poolId, float luck, RunRandom rng, ICollection<string> excludedIds)
        {
            var eligible = items.Where(i =>
                    i.InPool(poolId)
                    && i.weightMultiplier > 0f
                    && isUnlocked(i.requiredUnlockId)
                    && !IsExcluded(i, excludedIds))
                .ToList();
            if (eligible.Count == 0) return null;

            var rarities = eligible.Select(i => i.rarity).Distinct().OrderBy(r => (int)r).ToList();
            var rarityWeights = rarities.Select(r => RarityWeight(r, luck)).ToList();
            int rarityIndex = rng.PickWeighted(RandomStream.Items, rarityWeights);
            if (rarityIndex < 0) return null;

            var inRarity = eligible.Where(i => i.rarity == rarities[rarityIndex]).ToList();
            int itemIndex = rng.PickWeighted(RandomStream.Items, inRarity.Select(i => i.weightMultiplier).ToList());
            return itemIndex < 0 ? null : inRarity[itemIndex];
        }

        static bool IsExcluded(ItemDefinition item, ICollection<string> excludedIds)
        {
            if (excludedIds == null || string.IsNullOrEmpty(item.id)) return false;
            if (excludedIds.Contains(item.id)) return true;
            foreach (var id in excludedIds)
                if (string.Equals(id, item.id, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
