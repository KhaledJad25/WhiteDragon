using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class RunAndPoolTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        ItemDefinition Item(string id, ItemRarity rarity = ItemRarity.Common, string pool = "normal", string unlock = "", float weight = 1f)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.id = id;
            i.rarity = rarity;
            i.poolIds = new[] { pool };
            i.requiredUnlockId = unlock;
            i.weightMultiplier = weight;
            cleanup.Add(i);
            return i;
        }

        // ---------- RunRandom ----------

        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new RunRandom(1234);
            var b = new RunRandom(1234);
            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(a.Value(RandomStream.Items), b.Value(RandomStream.Items));
                Assert.AreEqual(a.Range(RandomStream.Combat, 0, 1000), b.Range(RandomStream.Combat, 0, 1000));
            }
            Assert.AreNotEqual(new RunRandom(1).Value(RandomStream.Items), new RunRandom(2).Value(RandomStream.Items));
        }

        [Test]
        public void Streams_AreIndependent()
        {
            var a = new RunRandom(99);
            var b = new RunRandom(99);
            for (int i = 0; i < 37; i++) b.Value(RandomStream.Combat);
            b.Range(RandomStream.Generation, 0, 10);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(a.Value(RandomStream.Items), b.Value(RandomStream.Items));
        }

        [Test]
        public void Range_Chance_PickWeighted_Shuffle()
        {
            var r = new RunRandom(5);
            for (int i = 0; i < 200; i++)
            {
                int v = r.Range(RandomStream.Misc, 3, 7);
                Assert.That(v, Is.InRange(3, 6));
                float f = r.Range(RandomStream.Misc, -1f, 1f);
                Assert.That(f, Is.GreaterThanOrEqualTo(-1f).And.LessThan(1f));
            }
            Assert.AreEqual(4, r.Range(RandomStream.Misc, 4, 4));
            Assert.IsFalse(r.Chance(RandomStream.Misc, 0f));
            Assert.IsTrue(r.Chance(RandomStream.Misc, 1f));
            Assert.AreEqual(-1, r.PickWeighted(RandomStream.Misc, new[] { 0f, 0f }));
            Assert.AreEqual(-1, r.PickWeighted(RandomStream.Misc, new float[0]));
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(1, r.PickWeighted(RandomStream.Misc, new[] { 0f, 5f, -2f }));

            var x = Enumerable.Range(0, 20).ToList();
            var y = Enumerable.Range(0, 20).ToList();
            new RunRandom(7).Shuffle(RandomStream.Misc, x);
            new RunRandom(7).Shuffle(RandomStream.Misc, y);
            CollectionAssert.AreEqual(x, y);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 20), x);
        }

        // ---------- RunSession ----------

        [Test]
        public void StartRun_Zero_GivesFreshDifferentSeeds()
        {
            RunSession.StartRun(0);
            int first = RunSession.Seed;
            RunSession.StartRun(0);
            int second = RunSession.Seed;
            Assert.AreNotEqual(0, first);
            Assert.AreNotEqual(first, second);
            RunSession.StartRun(777);
            Assert.AreEqual(777, RunSession.Seed);
            Assert.AreEqual(777, RunSession.Rng.Seed);
        }

        // ---------- Pools ----------

        [Test]
        public void PoolFiltering_And_SameSeedSameResults()
        {
            var items = new[] { Item("a"), Item("b"), Item("boss1", pool: "boss"), Item("boss2", pool: "BOSS") };
            var roller = new ItemPoolRoller(items, _ => true);
            for (int i = 0; i < 50; i++)
                StringAssert.StartsWith("boss", roller.Roll("boss", 0f, new RunRandom(i + 1)).id);

            var r1 = new RunRandom(42);
            var r2 = new RunRandom(42);
            for (int i = 0; i < 20; i++)
                Assert.AreSame(roller.Roll("normal", 0f, r1), roller.Roll("normal", 0f, r2));
        }

        [Test]
        public void Exclusions_AreRespected()
        {
            var items = new[] { Item("a"), Item("b"), Item("c") };
            var roller = new ItemPoolRoller(items, _ => true);
            var rng = new RunRandom(3);
            for (int i = 0; i < 30; i++)
                Assert.AreEqual("c", roller.Roll("normal", 0f, rng, new[] { "a", "B" }).id);
        }

        [Test]
        public void LockedItems_OnlyAppearWhenUnlocked()
        {
            var items = new[] { Item("free"), Item("locked", unlock: "secret") };
            var locked = new ItemPoolRoller(items, id => string.IsNullOrEmpty(id));
            var rng = new RunRandom(11);
            for (int i = 0; i < 50; i++)
                Assert.AreEqual("free", locked.Roll("normal", 0f, rng).id);

            var unlocked = new ItemPoolRoller(items, _ => true);
            var seen = new HashSet<string>();
            for (int i = 0; i < 100; i++) seen.Add(unlocked.Roll("normal", 0f, rng).id);
            CollectionAssert.Contains(seen, "locked");
        }

        [Test]
        public void UnlocksClass_EmptyAlwaysUnlocked()
        {
            Unlocks.Clear();
            Assert.IsTrue(Unlocks.Has(""));
            Assert.IsTrue(Unlocks.Has(null));
            Assert.IsFalse(Unlocks.Has("x"));
            Unlocks.Grant("x");
            Assert.IsTrue(Unlocks.Has("X"));
            Unlocks.Clear();
        }

        [Test]
        public void Fallback_ToNormal_ThenNull_NeverThrows()
        {
            var roller = new ItemPoolRoller(new[] { Item("n") }, _ => true);
            Assert.AreEqual("n", roller.Roll("nonexistent", 0f, new RunRandom(1)).id);

            var empty = new ItemPoolRoller(new[] { Item("t", pool: "treasure") }, _ => true);
            Assert.IsNull(empty.Roll("boss", 0f, new RunRandom(1)));
            Assert.IsNull(empty.Roll("normal", 0f, null));
            Assert.IsNull(new ItemPoolRoller(null).Roll(null, 0f, new RunRandom(1)));
        }

        [Test]
        public void RollDistinct_HasNoDuplicates()
        {
            var items = Enumerable.Range(0, 6).Select(i => Item("i" + i)).ToArray();
            var roller = new ItemPoolRoller(items, _ => true);
            var picks = roller.RollDistinct("normal", 4, 0f, new RunRandom(8));
            Assert.AreEqual(4, picks.Count);
            Assert.AreEqual(4, picks.Distinct().Count());
            Assert.AreEqual(6, roller.RollDistinct("normal", 20, 0f, new RunRandom(8)).Count);
        }

        ItemPoolRoller OnePerRarity() => new ItemPoolRoller(new[]
        {
            Item("c", ItemRarity.Common), Item("u", ItemRarity.Uncommon),
            Item("r", ItemRarity.Rare), Item("l", ItemRarity.Legendary),
        }, _ => true);

        Dictionary<ItemRarity, int> Count(ItemPoolRoller roller, float luck, int seed, int rolls = 2000)
        {
            var rng = new RunRandom(seed);
            var counts = new Dictionary<ItemRarity, int>();
            foreach (ItemRarity r in System.Enum.GetValues(typeof(ItemRarity))) counts[r] = 0;
            for (int i = 0; i < rolls; i++) counts[roller.Roll("normal", luck, rng).rarity]++;
            return counts;
        }

        [Test]
        public void HighLuck_GivesMoreRareAndLegendary()
        {
            var roller = OnePerRarity();
            var high = Count(roller, 10f, 1);
            var low = Count(roller, -5f, 1);
            Assert.Greater(high[ItemRarity.Rare] + high[ItemRarity.Legendary], low[ItemRarity.Rare] + low[ItemRarity.Legendary]);
        }

        [Test]
        public void ZeroLuck_RoughlyMatchesBaseWeights()
        {
            var counts = Count(OnePerRarity(), 0f, 2024);
            Assert.AreEqual(0.60, counts[ItemRarity.Common] / 2000.0, 0.04);
            Assert.AreEqual(0.28, counts[ItemRarity.Uncommon] / 2000.0, 0.04);
            Assert.AreEqual(0.10, counts[ItemRarity.Rare] / 2000.0, 0.03);
            Assert.AreEqual(0.02, counts[ItemRarity.Legendary] / 2000.0, 0.015);
        }

        [Test]
        public void WeightMultiplier_ZeroNeverRolls()
        {
            var roller = new ItemPoolRoller(new[] { Item("a"), Item("never", weight: 0f) }, _ => true);
            var rng = new RunRandom(4);
            for (int i = 0; i < 50; i++) Assert.AreEqual("a", roller.Roll("normal", 0f, rng).id);
        }

        // ---------- Pedestals ----------

        [Test]
        public void PedestalAssignment_IsIndependentOfInputOrder()
        {
            var root = new GameObject("PedestalRoot");
            cleanup.Add(root);
            var pedestals = new List<ItemPedestal>();
            foreach (var n in new[] { "A", "B", "C", "D" })
            {
                var go = new GameObject("Pedestal_" + n);
                go.transform.SetParent(root.transform);
                pedestals.Add(go.AddComponent<ItemPedestal>());
            }
            var items = Enumerable.Range(0, 8).Select(i => Item("item" + i, (ItemRarity)(i % 4))).ToArray();
            var roller = new ItemPoolRoller(items, _ => true);

            var forward = ItemPedestal.AssignItems(pedestals, roller, new RunRandom(55), 0f, null)
                .ToDictionary(x => x.pedestal, x => x.item);
            var reversed = Enumerable.Reverse(pedestals).ToList();
            reversed.Insert(1, reversed[3]);
            reversed.RemoveAt(4);
            var shuffled = ItemPedestal.AssignItems(reversed, roller, new RunRandom(55), 0f, null)
                .ToDictionary(x => x.pedestal, x => x.item);

            foreach (var p in pedestals) Assert.AreSame(forward[p], shuffled[p]);
            Assert.AreEqual(4, forward.Values.Distinct().Count(), "no duplicates across pedestals");
        }
    }
}
