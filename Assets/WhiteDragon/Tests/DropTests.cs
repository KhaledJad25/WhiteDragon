using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    public class DropTests
    {
        static readonly Vector3 Arena = new Vector3(-7000f, 0f, -7000f);
        const string OldAssetFolder = "Assets/WhiteDragon/Data/ZzOldDropFieldsTest";
        readonly List<Object> cleanup = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            PickupManager.ClearAll();
            EnemyDrops.Enable();
            EnemyDrops.DropInEditMode = true;
            EnemyDrops.ContextOverride = new DropContext();
        }

        [TearDown]
        public void TearDown()
        {
            EnemyDrops.DropInEditMode = false;
            EnemyDrops.ContextOverride = null;
            PickupManager.ClearAll();
            RunSession.EndRun();
            Unlocks.Clear();
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
            if (AssetDatabase.IsValidFolder(OldAssetFolder))
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.DeleteAsset(OldAssetFolder);
            }
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            cleanup.Add(o);
            return o;
        }

        PickupDefinition Pickup(string id, params string[] tags)
        {
            var p = Make<PickupDefinition>();
            p.id = id;
            p.tags = tags;
            p.effects = new List<PickupEffect> { Make<HealEffect>() };
            return p;
        }

        DropTableDefinition Table(float nothing, int rolls, params DropEntry[] entries)
        {
            var t = Make<DropTableDefinition>();
            t.id = "test_table";
            t.nothingWeight = nothing;
            t.rolls = rolls;
            t.entries = entries.ToList();
            return t;
        }

        static DropEntry E(PickupDefinition p, float w, int min = 1, int max = 1, DropCondition c = DropCondition.Always) =>
            new DropEntry { pickup = p, weight = w, minCount = min, maxCount = max, condition = c };

        /// <summary>A table with enough variety that two generators almost never agree.</summary>
        DropTableDefinition VariedTable() => Table(1f, 3,
            E(Pickup("a", "coin"), 1f, 1, 3), E(Pickup("b", "heart"), 1f, 1, 3), E(Pickup("c", "key"), 1f, 1, 3), E(Pickup("d", "soul"), 1f, 1, 3));

        EnemyDefinition EnemyDef(DropTableDefinition table, float chance)
        {
            var d = Make<EnemyDefinition>();
            d.id = "drop_dummy";
            d.displayName = "Drop Dummy";
            d.dropTable = table;
            d.dropChance = chance;
            return d;
        }

        Enemy Spawn(EnemyDefinition def, string key, Vector3 offset, EnemyVariant variant = null)
        {
            var e = EnemySpawner.Spawn(def, variant, Arena + offset, key);
            cleanup.Add(e.gameObject);
            return e;
        }

        static string Describe(List<(PickupDefinition pickup, int count)> drops) =>
            string.Join(",", drops.Select(d => d.pickup.id + "x" + d.count));

        /// <summary>Kills the enemy and returns what it dropped (the pickups added to the live list).</summary>
        static string KillAndCollectDrops(Enemy e)
        {
            int before = PickupManager.LiveCount;
            e.Kill();
            var ids = new List<string>();
            for (int i = before; i < PickupManager.LiveCount; i++) ids.Add(PickupManager.Live[i].Definition.id);
            ids.Sort(System.StringComparer.Ordinal);
            return string.Join(",", ids);
        }

        // ---------- Determinism ----------

        [Test]
        public void SameSeed_SameDrops_RegardlessOfKillOrder()
        {
            var def = EnemyDef(VariedTable(), 1f);
            RunSession.StartRun(4242);
            var a = Spawn(def, "room_t#0", Vector3.zero);
            var b = Spawn(def, "room_t#1", Vector3.right * 5f);
            string aFirst = KillAndCollectDrops(a), bSecond = KillAndCollectDrops(b);

            PickupManager.ClearAll();
            RunSession.StartRun(4242);
            var a2 = Spawn(def, "room_t#0", Vector3.zero);
            var b2 = Spawn(def, "room_t#1", Vector3.right * 5f);
            string bFirst = KillAndCollectDrops(b2), aSecond = KillAndCollectDrops(a2);

            Assert.IsNotEmpty(aFirst);
            Assert.AreEqual(aFirst, aSecond, "enemy #0 drops the same, killed first or second");
            Assert.AreEqual(bSecond, bFirst, "enemy #1 drops the same, killed first or second");
        }

        [Test]
        public void DifferentSeeds_GiveDifferentDrops()
        {
            var def = EnemyDef(VariedTable(), 1f);
            var ctx = new DropContext();
            var enemies = Enumerable.Range(0, 8).Select(i => Spawn(def, "room_t#" + i, Vector3.right * i)).ToList();
            string seed1 = string.Join("|", enemies.Select(e => Describe(EnemyDrops.RollFor(e, new RunRandom(1), ctx))));
            string seed2 = string.Join("|", enemies.Select(e => Describe(EnemyDrops.RollFor(e, new RunRandom(2), ctx))));
            string seed1Again = string.Join("|", enemies.Select(e => Describe(EnemyDrops.RollFor(e, new RunRandom(1), ctx))));
            Assert.AreEqual(seed1, seed1Again);
            Assert.AreNotEqual(seed1, seed2);
        }

        [Test]
        public void AppendingAnEnemyToARoom_DoesNotChangeExistingDrops()
        {
            var def = EnemyDef(VariedTable(), 1f);
            var ctx = new DropContext();
            List<string> DropsOf(int count)
            {
                var roomGo = new GameObject("DropRoom");
                cleanup.Add(roomGo);
                roomGo.AddComponent<BoxCollider>();
                var room = roomGo.AddComponent<RoomController>();
                room.roomId = "drop_room";
                for (int i = 0; i < count; i++) room.enemies.Add(Spawn(def, "unused", Vector3.right * i));
                room.Initialize();
                return room.enemies.Select(e => Describe(EnemyDrops.RollFor(e, new RunRandom(777), ctx))).ToList();
            }
            var before = DropsOf(3);
            var after = DropsOf(4);
            CollectionAssert.AreEqual(before, after.Take(3).ToList(), "the first three enemies keep their drops");
            Assert.AreEqual("drop_room#0:drop", DropRoller.EnemyKey("drop_room#0"));
            Assert.AreEqual("drop_room:reward", DropRoller.RoomKey("drop_room"));
        }

        // ---------- Weights and chances ----------

        [Test]
        public void Luck_LowersTheNothingWeight()
        {
            var table = Table(4f, 1, E(Pickup("coin", "coin"), 1f));
            Assert.AreEqual(4f, DropRoller.EffectiveNothingWeight(table, new DropContext { Luck = 0f }), 1e-5f);
            Assert.AreEqual(2f, DropRoller.EffectiveNothingWeight(table, new DropContext { Luck = 10f }), 1e-5f, "5% less per luck");
            Assert.AreEqual(0.8f, DropRoller.EffectiveNothingWeight(table, new DropContext { Luck = 100f }), 1e-5f, "at most 80% less");

            int Nothing(float luck) => DebugPanel.RollDistribution(table, 5, 2000, new DropContext { Luck = luck })
                .Where(r => r.label == "nothing").Select(r => r.count).DefaultIfEmpty(0).First();
            Assert.Less(Nothing(10f), Nothing(0f), "fewer empty rolls with luck");
        }

        [Test]
        public void TagMultiplier_ScalesEntryAndNothingWeights()
        {
            var coin = E(Pickup("coin", "coin"), 3f);
            var soul = E(Pickup("soul_half", "heart", "soul"), 2f);
            var table = Table(4f, 1, coin, soul);
            var ctx = new DropContext
            {
                TagMultipliers = new Dictionary<string, float> { { "coin", 2f }, { "heart", 0.5f }, { "soul", 3f }, { "nothing", 0.25f } },
            };
            Assert.AreEqual(6f, DropRoller.EffectiveWeight(coin, ctx), 1e-5f);
            Assert.AreEqual(3f, DropRoller.EffectiveWeight(soul, ctx), 1e-5f, "every tag on the pickup multiplies: 2 x 0.5 x 3");
            Assert.AreEqual(1f, DropRoller.EffectiveNothingWeight(table, ctx), 1e-5f, "\"nothing\" targets the nothing weight");
        }

        [Test]
        public void DropRateStat_ChangesTheEnemyChance()
        {
            Assert.AreEqual(1f, StatBlock.DefaultBase(StatType.DropRate), "base 1");
            var def = EnemyDef(VariedTable(), 0.2f);
            Assert.AreEqual(0.25f, DropRoller.EnemyDropChance(def, null, 1.25f), 1e-5f);
            Assert.AreEqual(1f, DropRoller.EnemyDropChance(def, null, 100f), "clamped to 1");

            var player = new GameObject("DropRatePlayer");
            cleanup.Add(player);
            player.AddComponent<PlayerStats>();
            var inventory = player.AddComponent<PlayerInventory>();
            var scavenger = Make<ItemDefinition>();
            scavenger.statModifiers = new[] { new StatModifier(StatType.DropRate, ModifierKind.PercentAdd, 0.25f) };
            inventory.Add(scavenger);
            Assert.AreEqual(1.25f, DropContext.FromPlayer(player).DropRate, 1e-5f, "a +25% DropRate item");
        }

        [Test]
        public void VariantMultiplier_ChangesTheChance_AndItsTableOverrides()
        {
            var baseTable = VariedTable();
            var def = EnemyDef(baseTable, 0.2f);
            var variant = Make<EnemyVariant>();
            Assert.AreEqual(1f, variant.dropChanceMultiplier, "defaults to 1");
            variant.dropChanceMultiplier = 2f;
            Assert.AreEqual(0.4f, DropRoller.EnemyDropChance(def, variant, 1f), 1e-5f);
            Assert.AreSame(baseTable, DropRoller.EnemyTable(def, variant), "no override: the base table");
            var own = Table(0f, 1, E(Pickup("key", "key"), 1f));
            variant.dropTable = own;
            Assert.AreSame(own, DropRoller.EnemyTable(def, variant));
        }

        [Test]
        public void ChanceZero_NeverDrops_ChanceOne_AlwaysRolls()
        {
            var table = Table(0f, 1, E(Pickup("coin", "coin"), 1f));
            for (int i = 0; i < 50; i++)
            {
                Assert.IsEmpty(DropRoller.Roll(table, 0f, new RunRandom(i), new DropContext()));
                Assert.AreEqual(1, DropRoller.Roll(table, 1f, new RunRandom(i), new DropContext()).Count);
            }
        }

        [Test]
        public void CharacterModifiers_Apply()
        {
            var character = Make<CharacterDefinition>();
            character.dropModifiers = new[] { new DropModifier("heart", 3f) };
            character.statOverrides = new[] { new StatOverride { stat = StatType.DropRate, value = 2f } };
            var player = new GameObject("CharacterDropPlayer");
            cleanup.Add(player);
            player.SetActive(false);
            player.AddComponent<PlayerStats>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerInventory>();
            var modifiers = player.AddComponent<PlayerDropModifiers>();
            var applier = player.AddComponent<CharacterApplier>();
            applier.character = character;
            applier.Apply();

            Assert.AreEqual(3f, modifiers.Multiplier("heart"), 1e-5f);
            var ctx = DropContext.FromPlayer(player);
            Assert.AreEqual(2f, ctx.DropRate, 1e-5f, "a DropRate override through the existing stat overrides");
            Assert.AreEqual(3f, ctx.Multiplier("HEART"), 1e-5f, "tags ignore case");
        }

        [Test]
        public void ItemAndSynergyModifiers_StackByMultiplying_AndAreClamped()
        {
            var character = Make<CharacterDefinition>();
            character.dropModifiers = new[] { new DropModifier("key", 2f) };
            var a = Make<ItemDefinition>();
            a.dropModifiers = new[] { new DropModifier("key", 3f), new DropModifier("coin", 100f) };
            var b = Make<ItemDefinition>();
            b.dropModifiers = new[] { new DropModifier("heart", 0.001f), new DropModifier("", 5f) };
            var synergy = Make<SynergyDefinition>();
            synergy.dropModifiers = new[] { new DropModifier("coin", 2f) };
            var result = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
            PlayerDropModifiers.Aggregate(character, new[] { a, b }, new[] { synergy }, result);

            Assert.AreEqual(6f, result["key"], 1e-5f, "character x2 then item x3");
            Assert.AreEqual(10f, result["coin"], 1e-5f, "x200 clamped to 10");
            Assert.AreEqual(0.1f, result["heart"], 1e-5f, "x0.001 clamped to 0.1");
            Assert.IsFalse(result.ContainsKey(""), "empty tags are ignored");
        }

        [Test]
        public void PlayerModifiers_FollowTheLoadout()
        {
            var player = new GameObject("LoadoutDropPlayer");
            cleanup.Add(player);
            player.AddComponent<PlayerStats>();
            var inventory = player.AddComponent<PlayerInventory>();
            var modifiers = player.AddComponent<PlayerDropModifiers>();
            var penny = Make<ItemDefinition>();
            penny.dropModifiers = new[] { new DropModifier("coin", 2f) };
            Assert.AreEqual(1f, modifiers.Multiplier("coin"));
            inventory.Add(penny);
            inventory.Add(penny);
            Assert.AreEqual(4f, modifiers.Multiplier("coin"), 1e-5f, "two copies: x2 x2");
            inventory.RemoveLast();
            Assert.AreEqual(2f, modifiers.Multiplier("coin"), 1e-5f, "rebuilt when the loadout changes");
        }

        // ---------- Entry rules ----------

        [Test]
        public void PlayerHurtEntries_AreSkippedAtFullHealth()
        {
            var table = Table(0f, 1, E(Pickup("half_heart", "heart"), 1f, c: DropCondition.PlayerHurt));
            Assert.IsEmpty(DropRoller.Roll(table, 1f, new RunRandom(3), new DropContext { PlayerHurt = false }));
            Assert.AreEqual(1, DropRoller.Roll(table, 1f, new RunRandom(3), new DropContext { PlayerHurt = true }).Count);

            var player = new GameObject("HurtPlayer");
            cleanup.Add(player);
            var health = player.AddComponent<PlayerHealth>();
            Assert.IsFalse(DropContext.FromPlayer(player).PlayerHurt, "full red health");
            health.State.TryDamage(1, 1000f);
            Assert.IsTrue(DropContext.FromPlayer(player).PlayerHurt);
        }

        [Test]
        public void LockedEntries_AreSkipped()
        {
            var locked = Pickup("secret", "coin");
            locked.requiredUnlockId = "zz_test_drop_unlock";
            var table = Table(0f, 1, E(locked, 1f));
            Assert.IsEmpty(DropRoller.Roll(table, 1f, new RunRandom(9), new DropContext()));
            Unlocks.Grant("zz_test_drop_unlock");
            Assert.AreEqual(1, DropRoller.Roll(table, 1f, new RunRandom(9), new DropContext()).Count);
        }

        // ---------- Sources ----------

        [Test]
        public void RoomClear_SpawnsExactlyOneRoll()
        {
            var roomGo = new GameObject("RewardRoom");
            cleanup.Add(roomGo);
            roomGo.transform.position = Arena;
            roomGo.AddComponent<BoxCollider>();
            var room = roomGo.AddComponent<RoomController>();
            room.roomId = "reward_room";
            room.rewardTable = Table(0f, 1, E(Pickup("coin", "coin"), 1f));
            var enemy = Spawn(EnemyDef(null, 0f), "unused", Vector3.zero);
            room.enemies.Add(enemy);
            room.Initialize();
            room.Enter();
            Assert.AreEqual(0, PickupManager.LiveCount, "nothing before the clear");

            enemy.Kill();
            room.CheckCleared();
            Assert.AreEqual(1, PickupManager.LiveCount, "one roll of one coin");
            room.CheckCleared();
            room.CheckCleared();
            Assert.AreEqual(1, PickupManager.LiveCount, "only once");
        }

        [Test]
        public void ExploderSelfDestruct_AlsoDrops()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cleanup.Add(floor);
            floor.transform.position = Arena + new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(40f, 1f, 40f);
            var target = new GameObject("ExploderDropTarget");
            cleanup.Add(target);
            target.transform.position = Arena + new Vector3(0f, 0f, 1.5f);
            var health = target.AddComponent<PlayerHealth>();

            var exploder = Object.Instantiate(EnemyCatalog.Find("exploder"));
            cleanup.Add(exploder);
            exploder.dropTable = Table(0f, 1, E(Pickup("coin", "coin"), 1f));
            exploder.dropChance = 1f;
            var e = Spawn(exploder, "boom_drop#0", Vector3.zero);
            e.SetTarget(health);
            Physics.SyncTransforms();
            for (int i = 0; i < 300 && !e.IsDead; i++) e.Tick(1f / 60f);

            Assert.IsTrue(e.IsDead, "it blew itself up");
            Assert.AreEqual(1, PickupManager.LiveCount, "and dropped like any death");
        }

        [Test]
        public void OldEnemyAndVariantAssets_LoadWithChanceZero_AndMultiplierOne()
        {
            Directory.CreateDirectory(OldAssetFolder);
            string defPath = WriteOldAsset("OldEnemy", typeof(EnemyDefinition), "  id: old_enemy\n  maxHealth: 20\n");
            string variantPath = WriteOldAsset("OldVariant", typeof(EnemyVariant), "  id: old_variant\n  healthMultiplier: 2\n");

            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(defPath);
            var variant = AssetDatabase.LoadAssetAtPath<EnemyVariant>(variantPath);
            Assert.IsNotNull(def);
            Assert.IsNotNull(variant);
            Assert.AreEqual("old_enemy", def.id);
            Assert.AreEqual(0f, def.dropChance);
            Assert.IsNull(def.dropTable);
            Assert.AreEqual(2f, variant.healthMultiplier);
            Assert.AreEqual(1f, variant.dropChanceMultiplier, "written before the field existed");
            Assert.IsNull(variant.dropTable);
            Assert.AreEqual(0f, DropRoller.EnemyDropChance(def, variant, 1f));
        }

        /// <summary>An asset as Unity saved it before the drop fields existed (no dropTable / dropChance lines).</summary>
        static string WriteOldAsset(string name, System.Type type, string fields)
        {
            var temp = ScriptableObject.CreateInstance(type);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(temp)));
            Object.DestroyImmediate(temp);
            string path = $"{OldAssetFolder}/{name}.asset";
            File.WriteAllText(path,
                "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n" +
                "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n" +
                "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n" +
                $"  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}\n" +
                $"  m_Name: {name}\n  m_EditorClassIdentifier: \n" + fields);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return path;
        }

        // ---------- Debug ----------

        [Test]
        public void RollDistribution_CountsEveryRoll_AndRepeatsForASeed()
        {
            var table = Table(1f, 1, E(Pickup("coin", "coin"), 1f), E(Pickup("key", "key"), 1f));
            var first = DebugPanel.RollDistribution(table, 11, 300, new DropContext());
            Assert.AreEqual(300, first.Sum(r => r.count), "one result per roll (count 1 each)");
            CollectionAssert.AreEquivalent(new[] { "coin", "key", "nothing" }, first.Select(r => r.label));
            CollectionAssert.AreEqual(first, DebugPanel.RollDistribution(table, 11, 300, new DropContext()), "same seed, same distribution");
        }
    }
}
