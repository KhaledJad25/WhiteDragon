using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    public class EnemyAuthoringTests
    {
        const string TestFamily = "zz_authoring_test";
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
            string folder = EnemyContentCreator.FamilyFolder(TestFamily);
            // Let any background import of the test assets finish before deleting them (avoids an import-worker race).
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            EnemyCatalog.Reload();
        }

        T Track<T>(T o) where T : Object
        {
            cleanup.Add(o);
            return o;
        }

        [Test]
        public void BehaviorScript_NameAndFolder()
        {
            Assert.AreEqual("DiveBombBehavior", EnemyContentCreator.BehaviorClassName("dive bomb"));
            Assert.AreEqual("ScratchBehavior", EnemyContentCreator.BehaviorClassName("ScratchBehavior"));
            Assert.AreEqual("Assets/WhiteDragon/Scripts/Enemies/Behaviors/Common/DiveBombBehavior.cs",
                EnemyContentCreator.BehaviorScriptPath("DiveBombBehavior", ""));
            Assert.AreEqual("Assets/WhiteDragon/Scripts/Enemies/Behaviors/Specific/Exploder/BlastBehavior.cs",
                EnemyContentCreator.BehaviorScriptPath("BlastBehavior", "exploder"));
        }

        [Test]
        public void BehaviorScriptTemplate_HasInfoStateAndHooks()
        {
            string text = EnemyContentCreator.BehaviorScriptText("BlastBehavior", "Exploder");
            StringAssert.Contains("[EnemyBehaviorInfo(\"TODO: one-line description.\", \"Exploder\")]", text);
            StringAssert.Contains("public class BlastBehavior : EnemyBehavior<BlastBehavior.State>", text);
            StringAssert.Contains("public class State", text);
            StringAssert.Contains("protected override void Enter(EnemyContext ctx, State s)", text);
            StringAssert.Contains("protected override bool Tick(EnemyContext ctx, State s, float dt)", text);
            StringAssert.Contains("protected override void Exit(EnemyContext ctx, State s)", text);
            StringAssert.Contains("namespace WhiteDragon", text);
        }

        [Test]
        public void NewEnemy_CreatesDefinitionWithWorkingStarterBrain_InItsFamilyFolder()
        {
            var def = EnemyContentCreator.CreateEnemy("Test Grunt", TestFamily);
            string folder = EnemyContentCreator.FamilyFolder(TestFamily);
            StringAssert.StartsWith(folder + "/", AssetDatabase.GetAssetPath(def));
            Assert.AreEqual("test_grunt", def.id);
            Assert.AreEqual(TestFamily, def.family);
            var brain = def.brain as StateMachineBrain;
            Assert.IsNotNull(brain, "has a starter brain");
            Assert.AreEqual(1, brain.states.Count);
            Assert.IsInstanceOf<MoveTowardBehavior>(brain.states[0].behaviors[0]);
            Assert.IsInstanceOf<MeleeContactBehavior>(brain.states[0].behaviors[1]);
            StringAssert.StartsWith(folder + "/", AssetDatabase.GetAssetPath(brain));

            var variant = EnemyContentCreator.CreateVariant(def, "Big");
            Assert.AreSame(def, variant.baseEnemy);
            StringAssert.StartsWith(folder + "/", AssetDatabase.GetAssetPath(variant));
        }

        [Test]
        public void NewEnemy_GetsItsOwnBehaviorCopies_InItsOwnFolder()
        {
            var a = EnemyContentCreator.CreateEnemy("Alpha", TestFamily);
            var b = EnemyContentCreator.CreateEnemy("Beta", TestFamily);
            string family = EnemyContentCreator.FamilyFolder(TestFamily);
            var brainA = (StateMachineBrain)a.brain;
            var brainB = (StateMachineBrain)b.brain;
            Assert.AreNotSame(brainA, brainB);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreNotSame(brainA.states[0].behaviors[i], brainB.states[0].behaviors[i], "copies, never shared");
                StringAssert.StartsWith(family + "/Alpha/", AssetDatabase.GetAssetPath(brainA.states[0].behaviors[i]));
                StringAssert.StartsWith(family + "/Beta/", AssetDatabase.GetAssetPath(brainB.states[0].behaviors[i]));
            }
            StringAssert.StartsWith(family + "/Alpha/", AssetDatabase.GetAssetPath(a));
            var zombieChase = EnemyCatalog.Find("ghoul").brain;
            Assert.AreNotSame(zombieChase, brainA, "the zombie's shared assets are left alone");

            ((MeleeContactBehavior)brainA.states[0].behaviors[1]).cooldown = 5f;
            Assert.AreEqual(1f, ((MeleeContactBehavior)brainB.states[0].behaviors[1]).cooldown, "tuning one never changes the other");
        }

        [Test]
        public void EnemyInScene_AppendsToTheRoom_NeverShiftsOtherKeys()
        {
            var roomGo = Track(new GameObject("TestRoom"));
            roomGo.AddComponent<BoxCollider>();
            var room = roomGo.AddComponent<RoomController>();
            room.roomId = "test_room";
            var enemiesRoot = new GameObject("Enemies").transform;
            enemiesRoot.SetParent(roomGo.transform);
            var first = Track(new GameObject("A")).AddComponent<CharacterController>().gameObject.AddComponent<Enemy>();
            var second = Track(new GameObject("B")).AddComponent<CharacterController>().gameObject.AddComponent<Enemy>();
            room.enemies.Add(first);
            room.enemies.Add(second);

            var def = EnemyCatalog.Find("skeleton");
            var added = EnemyContentCreator.BuildInScene(def, null, Vector3.zero, room);
            Track(added.gameObject);
            Assert.AreEqual(3, room.enemies.Count);
            Assert.AreSame(first, room.enemies[0], "existing enemies keep their index (and so their random key)");
            Assert.AreSame(second, room.enemies[1]);
            Assert.AreSame(added, room.enemies[2], "the new one goes at the end");
            Assert.AreEqual(enemiesRoot, added.transform.parent);
            Assert.AreSame(def, added.definition);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(added.gameObject), "uses the skeleton's prefab");

            var bat = EnemyCatalog.Find("bat");
            var fast = EnemyCatalog.FindVariant("fast_bat");
            var built = EnemyContentCreator.BuildInScene(bat, fast, Vector3.zero, null);
            Track(built.gameObject);
            Assert.AreSame(fast, built.variant);
            Assert.IsNull(built.transform.parent, "no room selected: left at the scene root");
        }

        [Test]
        public void EnemyInScene_WithoutPrefab_BuildsPlaceholderWithEverythingAnEnemyNeeds()
        {
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.displayName = "Loose";
            def.movement = MovementMode.Flying;
            var e = EnemyContentCreator.BuildInScene(def, null, new Vector3(0f, 900f, 0f), null);
            Track(e.gameObject);
            Assert.IsNotNull(e.GetComponent<CharacterController>());
            Assert.IsNotNull(e.GetComponent<StatusReceiver>());
            Assert.Greater(e.GetComponentsInChildren<Renderer>().Length, 0, "placeholder shapes");
            Assert.AreEqual(0, e.GetComponentsInChildren<Collider>().Count(c => !(c is CharacterController)), "shapes have no colliders");
        }

        [Test]
        public void BehaviorTypes_AreTheGamesOwn_WithDescriptions()
        {
            var types = EnemyContentCreator.BehaviorTypes();
            CollectionAssert.Contains(types, typeof(DashBehavior));
            CollectionAssert.DoesNotContain(types, typeof(CountingBehavior), "test behaviors are not offered");
            foreach (var t in types)
                Assert.IsNotNull(EnemyContentCreator.Info(t), $"{t.Name} has [EnemyBehaviorInfo]");
        }
    }
}
