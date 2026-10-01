using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WhiteDragon
{
    public class CharacterTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        CharacterApplier MakePlayer(CharacterDefinition character)
        {
            var go = new GameObject("TestPlayer");
            cleanup.Add(go);
            var applier = go.AddComponent<CharacterApplier>();
            applier.character = character;
            applier.Apply();
            return applier;
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            cleanup.Add(o);
            return o;
        }

        [Test]
        public void Character_AppliesOverridesHeartsAndItems()
        {
            var item = Make<ItemDefinition>();
            item.id = "test_item";
            item.statModifiers = new[] { new StatModifier(StatType.Damage, ModifierKind.Flat, 1f) };
            var c = Make<CharacterDefinition>();
            c.statOverrides = new[] { new StatOverride(StatType.Damage, 5f), new StatOverride(StatType.MoveSpeed, 7f) };
            c.startingRedContainers = 2;
            c.startingSoulHearts = 3;
            c.startingDarkHearts = 2;
            c.startingItems.Add(item);
            c.startingItems.Add(null);

            var player = MakePlayer(c);
            var stats = player.GetComponent<PlayerStats>().Stats;
            var health = player.GetComponent<PlayerHealth>().State;
            var items = player.GetComponent<PlayerInventory>().Loadout.Items;

            Assert.AreEqual(5f, stats.GetBase(StatType.Damage));
            Assert.AreEqual(6f, stats.Get(StatType.Damage), 1e-4f, "item modifies on top of the override");
            Assert.AreEqual(7f, stats.Get(StatType.MoveSpeed));
            Assert.AreEqual(StatBlock.DefaultBase(StatType.FireRate), stats.Get(StatType.FireRate), "not overridden");
            Assert.AreEqual(2, health.RedContainers);
            Assert.AreEqual(4, health.Red);
            Assert.AreEqual(3, health.Soul);
            Assert.AreEqual(2, health.Dark);
            Assert.AreEqual(1, items.Count, "null slot skipped");
            Assert.AreSame(item, items[0]);
        }

        [Test]
        public void NoCharacter_LeavesDefaultsUnchanged()
        {
            var player = MakePlayer(null);
            AssertDefaults(player);
        }

        [Test]
        public void RockThrowerAsset_ReproducesDefaults()
        {
            var rockThrower = Resources.Load<CharacterDefinition>("Characters/Rock Thrower");
            Assert.IsNotNull(rockThrower, "Data/Resources/Characters/Rock Thrower.asset exists");
            Assert.AreEqual("rock_thrower", rockThrower.id);
            AssertDefaults(MakePlayer(rockThrower));
        }

        static void AssertDefaults(CharacterApplier player)
        {
            var stats = player.GetComponent<PlayerStats>().Stats;
            foreach (StatType s in Enum.GetValues(typeof(StatType)))
                Assert.AreEqual(StatBlock.DefaultBase(s), stats.Get(s), 1e-5f, s.ToString());
            var health = player.GetComponent<PlayerHealth>().State;
            Assert.AreEqual(3, health.RedContainers);
            Assert.AreEqual(6, health.Red);
            Assert.AreEqual(0, health.Soul);
            Assert.AreEqual(0, health.Dark);
            Assert.AreEqual(0, player.GetComponent<PlayerInventory>().Loadout.Items.Count);
        }
    }
}
