using System;
using System.Collections.Generic;

public class ItemPoolRoller
{
    static readonly float[] BaseRarityWeights = new float[] { 60f, 28f, 10f, 2f };

    public static float GetEffectiveRarityWeight(ItemRarity rarity, float luck)
    {
        int t = (int)rarity;
        float clampedLuck = Math.Max(-5f, Math.Min(10f, luck));
        float factor = Math.Max(0.2f, 1f + 0.12f * clampedLuck * t);
        return BaseRarityWeights[t] * factor;
    }

    public static ItemDefinition Roll(
        IReadOnlyList<ItemDefinition> allItems,
        ItemPoolType pool,
        RunRandom rng,
        float luck,
        ISet<string> excludedIds = null,
        Func<string, bool> unlockPredicate = null)
    {
        if (allItems == null || allItems.Count == 0 || rng == null)
        {
            return null;
        }

        // 1. Try rolling from the requested pool
        ItemDefinition item = RollFromPool(allItems, pool, rng, luck, excludedIds, unlockPredicate);
        if (item != null)
        {
            return item;
        }

        // 2. Fallback to Normal pool if requested pool was not Normal
        if (pool != ItemPoolType.Normal)
        {
            item = RollFromPool(allItems, ItemPoolType.Normal, rng, luck, excludedIds, unlockPredicate);
            if (item != null)
            {
                return item;
            }
        }

        return null;
    }

    public static List<ItemDefinition> RollDistinct(
        IReadOnlyList<ItemDefinition> allItems,
        ItemPoolType pool,
        RunRandom rng,
        float luck,
        int count,
        ISet<string> excludedIds = null,
        Func<string, bool> unlockPredicate = null)
    {
        List<ItemDefinition> results = new List<ItemDefinition>();
        if (count <= 0 || allItems == null || allItems.Count == 0 || rng == null)
        {
            return results;
        }

        HashSet<string> currentExclusions = excludedIds != null
            ? new HashSet<string>(excludedIds)
            : new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            ItemDefinition item = Roll(allItems, pool, rng, luck, currentExclusions, unlockPredicate);
            if (item == null)
            {
                break;
            }
            results.Add(item);
            currentExclusions.Add(item.id);
        }

        return results;
    }

    static ItemDefinition RollFromPool(
        IReadOnlyList<ItemDefinition> allItems,
        ItemPoolType pool,
        RunRandom rng,
        float luck,
        ISet<string> excludedIds,
        Func<string, bool> unlockPredicate)
    {
        // Group eligible items by rarity
        List<ItemDefinition>[] byRarity = new List<ItemDefinition>[4];
        for (int i = 0; i < 4; i++)
        {
            byRarity[i] = new List<ItemDefinition>();
        }

        int totalEligible = 0;
        for (int i = 0; i < allItems.Count; i++)
        {
            ItemDefinition item = allItems[i];
            if (item == null)
            {
                continue;
            }

            if (excludedIds != null && excludedIds.Contains(item.id))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(item.requiredUnlockId))
            {
                if (unlockPredicate == null || !unlockPredicate(item.requiredUnlockId))
                {
                    continue;
                }
            }

            bool inPool = false;
            if (item.pools != null)
            {
                for (int p = 0; p < item.pools.Length; p++)
                {
                    if (item.pools[p] == pool)
                    {
                        inPool = true;
                        break;
                    }
                }
            }

            if (!inPool)
            {
                continue;
            }

            int rIdx = (int)item.rarity;
            if (rIdx >= 0 && rIdx < 4)
            {
                byRarity[rIdx].Add(item);
                totalEligible++;
            }
        }

        if (totalEligible == 0)
        {
            return null;
        }

        // Calculate weights for rarities that have eligible items
        float[] rarityWeights = new float[4];
        for (int i = 0; i < 4; i++)
        {
            if (byRarity[i].Count > 0)
            {
                rarityWeights[i] = GetEffectiveRarityWeight((ItemRarity)i, luck);
            }
            else
            {
                rarityWeights[i] = 0f;
            }
        }

        int chosenRarity = rng.PickWeighted(RandomStream.Items, rarityWeights);
        if (chosenRarity < 0 || chosenRarity >= 4 || byRarity[chosenRarity].Count == 0)
        {
            // Fallback: pick any non-empty rarity
            for (int i = 0; i < 4; i++)
            {
                if (byRarity[i].Count > 0)
                {
                    chosenRarity = i;
                    break;
                }
            }
        }

        List<ItemDefinition> eligibleInRarity = byRarity[chosenRarity];
        return rng.PickWeighted(RandomStream.Items, eligibleInRarity, item => Math.Max(0.001f, item.weightMultiplier));
    }
}
