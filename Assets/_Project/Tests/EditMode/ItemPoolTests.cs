using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ItemPoolTests
{
    static ItemDefinition MakeItem(string id, ItemRarity rarity, ItemPoolType[] pools, float weight = 1f, string unlockId = "")
    {
        ItemDefinition i = ScriptableObject.CreateInstance<ItemDefinition>();
        i.id = id;
        i.displayName = id;
        i.rarity = rarity;
        i.pools = pools;
        i.weightMultiplier = weight;
        i.requiredUnlockId = unlockId;
        return i;
    }

    [Test]
    public void SameSeed_ProducesSameSequence()
    {
        var rng1 = new RunRandom(4242);
        var rng2 = new RunRandom(4242);

        for (int i = 0; i < 50; i++)
        {
            Assert.AreEqual(rng1.Value(RandomStream.Items), rng2.Value(RandomStream.Items), 0.000001f);
            Assert.AreEqual(rng1.Range(RandomStream.Rewards, 1, 100), rng2.Range(RandomStream.Rewards, 1, 100));
        }
    }

    [Test]
    public void StreamIsolation_BurningOneStream_LeavesOthersUntouched()
    {
        var rngA = new RunRandom(999);
        var rngB = new RunRandom(999);

        // Burn 1000 numbers on Items stream in rngA
        for (int i = 0; i < 1000; i++)
        {
            rngA.Value(RandomStream.Items);
        }

        // Rewards and Encounter streams on rngA and rngB must match exactly
        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(rngA.Value(RandomStream.Rewards), rngB.Value(RandomStream.Rewards), 0.000001f);
            Assert.AreEqual(rngA.Range(RandomStream.Encounter, 0, 50), rngB.Range(RandomStream.Encounter, 0, 50));
        }
    }

    [Test]
    public void SameSeed_GivesSameRoll()
    {
        var items = new List<ItemDefinition>
        {
            MakeItem("i1", ItemRarity.Common, new[] { ItemPoolType.Normal }),
            MakeItem("i2", ItemRarity.Uncommon, new[] { ItemPoolType.Normal }),
            MakeItem("i3", ItemRarity.Rare, new[] { ItemPoolType.Normal }),
            MakeItem("i4", ItemRarity.Legendary, new[] { ItemPoolType.Normal })
        };

        var rng1 = new RunRandom(12345);
        var rng2 = new RunRandom(12345);

        for (int i = 0; i < 20; i++)
        {
            var roll1 = ItemPoolRoller.Roll(items, ItemPoolType.Normal, rng1, 0f);
            var roll2 = ItemPoolRoller.Roll(items, ItemPoolType.Normal, rng2, 0f);
            Assert.AreEqual(roll1.id, roll2.id);
        }
    }

    [Test]
    public void DifferentPools_ReturnOnlyItemsFromThatPool()
    {
        var items = new List<ItemDefinition>
        {
            MakeItem("norm_1", ItemRarity.Common, new[] { ItemPoolType.Normal }),
            MakeItem("boss_1", ItemRarity.Rare, new[] { ItemPoolType.Boss }),
            MakeItem("treas_1", ItemRarity.Uncommon, new[] { ItemPoolType.Treasure })
        };

        var rng = new RunRandom(777);
        for (int i = 0; i < 25; i++)
        {
            var roll = ItemPoolRoller.Roll(items, ItemPoolType.Boss, rng, 0f);
            Assert.AreEqual("boss_1", roll.id);
        }
    }

    [Test]
    public void ExcludedItems_AreNeverReturned()
    {
        var items = new List<ItemDefinition>
        {
            MakeItem("common_a", ItemRarity.Common, new[] { ItemPoolType.Normal }),
            MakeItem("common_b", ItemRarity.Common, new[] { ItemPoolType.Normal })
        };

        var exclusions = new HashSet<string> { "common_a" };
        var rng = new RunRandom(555);

        for (int i = 0; i < 30; i++)
        {
            var roll = ItemPoolRoller.Roll(items, ItemPoolType.Normal, rng, 0f, exclusions);
            Assert.AreEqual("common_b", roll.id);
        }
    }

    [Test]
    public void LockedItems_WithFalsePredicate_AreNeverReturned()
    {
        var items = new List<ItemDefinition>
        {
            MakeItem("open", ItemRarity.Common, new[] { ItemPoolType.Normal }),
            MakeItem("locked", ItemRarity.Common, new[] { ItemPoolType.Normal }, 1f, "beat_boss_1")
        };

        var rng = new RunRandom(101);
        Func<string, bool> isUnlocked = id => false;

        for (int i = 0; i < 30; i++)
        {
            var roll = ItemPoolRoller.Roll(items, ItemPoolType.Normal, rng, 0f, null, isUnlocked);
            Assert.AreEqual("open", roll.id);
        }

        // When unlocked, both are eligible
        Func<string, bool> unlockedTrue = id => true;
        bool sawLocked = false;
        for (int i = 0; i < 50; i++)
        {
            var roll = ItemPoolRoller.Roll(items, ItemPoolType.Normal, rng, 0f, null, unlockedTrue);
            if (roll.id == "locked") sawLocked = true;
        }
        Assert.IsTrue(sawLocked);
    }

    [Test]
    public void FallbackToNormal_HappensWhenRequestedPoolEmpty_AndNullWhenNothingEligible()
    {
        var items = new List<ItemDefinition>
        {
            MakeItem("normal_only", ItemRarity.Common, new[] { ItemPoolType.Normal })
        };

        var rng = new RunRandom(888);

        // Requested pool is Boss, but items only has Normal -> fallback to Normal
        var fallbackRoll = ItemPoolRoller.Roll(items, ItemPoolType.Boss, rng, 0f);
        Assert.IsNotNull(fallbackRoll);
        Assert.AreEqual("normal_only", fallbackRoll.id);

        // If Normal is also empty/excluded -> returns null, never throws
        var exclusions = new HashSet<string> { "normal_only" };
        var emptyRoll = ItemPoolRoller.Roll(items, ItemPoolType.Boss, rng, 0f, exclusions);
        Assert.IsNull(emptyRoll);
    }

    [Test]
    public void RollDistinct_NeverReturnsDuplicates()
    {
        var items = new List<ItemDefinition>
        {
            MakeItem("item_1", ItemRarity.Common, new[] { ItemPoolType.Treasure }),
            MakeItem("item_2", ItemRarity.Uncommon, new[] { ItemPoolType.Treasure }),
            MakeItem("item_3", ItemRarity.Rare, new[] { ItemPoolType.Treasure }),
            MakeItem("item_4", ItemRarity.Legendary, new[] { ItemPoolType.Treasure })
        };

        var rng = new RunRandom(333);
        var picked = ItemPoolRoller.RollDistinct(items, ItemPoolType.Treasure, rng, 0f, 3);

        Assert.AreEqual(3, picked.Count);
        var seen = new HashSet<string>();
        foreach (var p in picked)
        {
            Assert.IsTrue(seen.Add(p.id), $"Duplicate item encountered: {p.id}");
        }
    }

    [Test]
    public void Luck10_ProducesStrictlyMoreRareAndLegendary_ThanLuckMinus5()
    {
        var items = new List<ItemDefinition>
        {
            MakeItem("c", ItemRarity.Common, new[] { ItemPoolType.Normal }),
            MakeItem("u", ItemRarity.Uncommon, new[] { ItemPoolType.Normal }),
            MakeItem("r", ItemRarity.Rare, new[] { ItemPoolType.Normal }),
            MakeItem("l", ItemRarity.Legendary, new[] { ItemPoolType.Normal })
        };

        const int iterations = 2000;
        int highTierAtLuckMinus5 = 0;
        int highTierAtLuck10 = 0;

        var rngNeg = new RunRandom(54321);
        for (int i = 0; i < iterations; i++)
        {
            var item = ItemPoolRoller.Roll(items, ItemPoolType.Normal, rngNeg, -5f);
            if (item.rarity == ItemRarity.Rare || item.rarity == ItemRarity.Legendary)
            {
                highTierAtLuckMinus5++;
            }
        }

        var rngPos = new RunRandom(54321);
        for (int i = 0; i < iterations; i++)
        {
            var item = ItemPoolRoller.Roll(items, ItemPoolType.Normal, rngPos, 10f);
            if (item.rarity == ItemRarity.Rare || item.rarity == ItemRarity.Legendary)
            {
                highTierAtLuck10++;
            }
        }

        Assert.Greater(highTierAtLuck10, highTierAtLuckMinus5,
            $"Expected Luck 10 high-tier count ({highTierAtLuck10}) to be strictly greater than Luck -5 ({highTierAtLuckMinus5})");
    }

    [Test]
    public void Luck0_MatchesRoughlyBaseDistribution()
    {
        var items = new List<ItemDefinition>
        {
            MakeItem("c", ItemRarity.Common, new[] { ItemPoolType.Normal }),
            MakeItem("u", ItemRarity.Uncommon, new[] { ItemPoolType.Normal }),
            MakeItem("r", ItemRarity.Rare, new[] { ItemPoolType.Normal }),
            MakeItem("l", ItemRarity.Legendary, new[] { ItemPoolType.Normal })
        };

        // Base weights: Common 60, Uncommon 28, Rare 10, Legendary 2 (total = 100)
        // 60%, 28%, 10%, 2%
        const int iterations = 5000;
        int common = 0, uncommon = 0, rare = 0, leg = 0;

        var rng = new RunRandom(777123);
        for (int i = 0; i < iterations; i++)
        {
            var item = ItemPoolRoller.Roll(items, ItemPoolType.Normal, rng, 0f);
            if (item.rarity == ItemRarity.Common) common++;
            else if (item.rarity == ItemRarity.Uncommon) uncommon++;
            else if (item.rarity == ItemRarity.Rare) rare++;
            else if (item.rarity == ItemRarity.Legendary) leg++;
        }

        float cRatio = (float)common / iterations;
        float uRatio = (float)uncommon / iterations;
        float rRatio = (float)rare / iterations;
        float lRatio = (float)leg / iterations;

        Assert.AreEqual(0.60f, cRatio, 0.05f);
        Assert.AreEqual(0.28f, uRatio, 0.05f);
        Assert.AreEqual(0.10f, rRatio, 0.04f);
        Assert.AreEqual(0.02f, lRatio, 0.02f);
    }
}
