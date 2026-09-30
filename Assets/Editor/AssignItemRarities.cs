#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AssignItemRarities
{
    struct TargetData
    {
        public ItemRarity rarity;
        public ItemPoolType[] pools;

        public TargetData(ItemRarity rarity, ItemPoolType[] pools)
        {
            this.rarity = rarity;
            this.pools = pools;
        }
    }

    static readonly Dictionary<string, TargetData> TargetMapping = new Dictionary<string, TargetData>
    {
        // Common, Normal
        { "swift_fingers", new TargetData(ItemRarity.Common, new[] { ItemPoolType.Normal }) },
        { "oathstone", new TargetData(ItemRarity.Common, new[] { ItemPoolType.Normal, ItemPoolType.Holy }) },
        { "smoldering_coal", new TargetData(ItemRarity.Common, new[] { ItemPoolType.Normal }) },
        { "iron_knuckle", new TargetData(ItemRarity.Common, new[] { ItemPoolType.Normal }) },
        { "wolfbone_charm", new TargetData(ItemRarity.Common, new[] { ItemPoolType.Normal }) },
        { "grave_dust", new TargetData(ItemRarity.Common, new[] { ItemPoolType.Normal }) },

        // Uncommon, Normal and Treasure
        { "iron_grip", new TargetData(ItemRarity.Uncommon, new[] { ItemPoolType.Normal, ItemPoolType.Treasure }) },
        { "ember_stone", new TargetData(ItemRarity.Uncommon, new[] { ItemPoolType.Normal, ItemPoolType.Treasure }) },
        { "hawks_feather", new TargetData(ItemRarity.Uncommon, new[] { ItemPoolType.Normal, ItemPoolType.Treasure, ItemPoolType.Holy }) },
        { "cinder_ash", new TargetData(ItemRarity.Uncommon, new[] { ItemPoolType.Normal, ItemPoolType.Treasure }) },
        { "black_tallow", new TargetData(ItemRarity.Uncommon, new[] { ItemPoolType.Normal, ItemPoolType.Treasure, ItemPoolType.Dark }) },

        // Rare, Treasure
        { "twin_stones", new TargetData(ItemRarity.Rare, new[] { ItemPoolType.Treasure }) },
        { "piercing_shard", new TargetData(ItemRarity.Rare, new[] { ItemPoolType.Treasure }) },
        { "hawks_eye", new TargetData(ItemRarity.Rare, new[] { ItemPoolType.Treasure, ItemPoolType.Holy }) },

        // Rare, Boss and Treasure
        { "giants_marrow", new TargetData(ItemRarity.Rare, new[] { ItemPoolType.Boss, ItemPoolType.Treasure }) },

        // Legendary, Boss
        { "brand_of_sacrifice", new TargetData(ItemRarity.Legendary, new[] { ItemPoolType.Boss, ItemPoolType.Dark }) },
    };

    [MenuItem("Tools/WhiteDragon/Assign Item Rarities")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Project/Data/Resources/Items" });
        Dictionary<string, ItemDefinition> loadedItems = new Dictionary<string, ItemDefinition>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null && !string.IsNullOrEmpty(item.id))
            {
                loadedItems[item.id] = item;
            }
        }

        int changedCount = 0;
        int checkedCount = 0;

        foreach (var kvp in TargetMapping)
        {
            string id = kvp.Key;
            TargetData target = kvp.Value;

            if (!loadedItems.TryGetValue(id, out ItemDefinition item))
            {
                Debug.LogWarning($"[AssignItemRarities] Warning: Item with id '{id}' not found in Assets/_Project/Data/Resources/Items.");
                continue;
            }

            checkedCount++;

            ItemRarity oldRarity = item.rarity;
            ItemPoolType[] oldPools = item.pools ?? Array.Empty<ItemPoolType>();

            // Distinct and sorted comparison for pools
            var normalizedOldPools = oldPools.Distinct().OrderBy(p => p).ToArray();
            var normalizedNewPools = target.pools.Distinct().OrderBy(p => p).ToArray();

            bool rarityChanged = oldRarity != target.rarity;
            bool poolsChanged = !normalizedOldPools.SequenceEqual(normalizedNewPools);

            string oldPoolsStr = "[" + string.Join(", ", normalizedOldPools) + "]";
            string newPoolsStr = "[" + string.Join(", ", normalizedNewPools) + "]";

            if (rarityChanged || poolsChanged)
            {
                item.rarity = target.rarity;
                item.pools = normalizedNewPools;
                EditorUtility.SetDirty(item);
                changedCount++;

                Debug.Log($"[AssignItemRarities] {id}: rarity {oldRarity} -> {target.rarity}, pools {oldPoolsStr} -> {newPoolsStr}");
            }
            else
            {
                Debug.Log($"[AssignItemRarities] {id}: unchanged (rarity {oldRarity}, pools {oldPoolsStr})");
            }
        }

        if (changedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[AssignItemRarities] Complete. {changedCount} items changed out of {checkedCount} checked.");
    }
}
#endif
