using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhiteDragon
{
    public class EnemyDebugToolsTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            Enemy.FreezeAI = false;
            Projectile.ClearAll();
            RunSession.EndRun();
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        Enemy Track(Enemy e)
        {
            cleanup.Add(e.gameObject);
            return e;
        }

        static float[] Draw(RunRandom r, int n) => Enumerable.Range(0, n).Select(_ => r.Value(RandomStream.Combat)).ToArray();

        [Test]
        public void DebugSpawns_UseTheirOwnKeys_NeverJoinARoom_NeverShiftRoomEnemies()
        {
            RunSession.StartRun(9001);
            var roomGo = new GameObject("KeyRoom");
            cleanup.Add(roomGo);
            roomGo.AddComponent<BoxCollider>();
            var room = roomGo.AddComponent<RoomController>();
            room.roomId = "key_room";
            var member = Track(EnemySpawner.Spawn(EnemyCatalog.Find("ghoul"), null, new Vector3(0f, 3000f, 0f), "unused"));
            room.enemies.Add(member);
            room.Initialize();
            room.Enter();
            var before = Draw(member.Random, 5);

            var debug = Track(EnemySpawner.Spawn(EnemyCatalog.Find("bat"), null, new Vector3(5f, 3000f, 0f), EnemySpawner.NextDebugKey()));
            StringAssert.StartsWith("debug:", debug.SpawnKey);
            Assert.AreEqual(1, room.enemies.Count, "debug spawns never join a room");
            Assert.AreEqual("key_room#0", member.SpawnKey);

            RunSession.StartRun(9001);
            CollectionAssert.AreEqual(before, Draw(member.Random, 5), "the room enemy rolls the same with a debug enemy around");
        }

        [Test]
        public void Spawner_AppliesVariant_AndUsesThePrefab()
        {
            var fast = EnemyCatalog.FindVariant("fast_bat");
            var e = Track(EnemySpawner.Spawn(fast.baseEnemy, fast, new Vector3(0f, 3000f, 10f), EnemySpawner.NextDebugKey()));
            Assert.AreSame(fast, e.variant);
            Assert.AreEqual(4f * 1.6f, e.Stats.MoveSpeed, 1e-4f);
            Assert.AreEqual(0.7f, e.GetComponent<CharacterController>().height, 1e-4f, "the bat prefab's body, not a placeholder");

            var brute = Track(EnemySpawner.Spawn(EnemyCatalog.Find("brute"), null, new Vector3(0f, 3000f, 20f), EnemySpawner.NextDebugKey()));
            Assert.AreEqual(0.75f, brute.GetComponent<CharacterController>().radius, 1e-4f, "the Brute keeps its big body");
        }

        [Test]
        public void FreezeAI_StopsThinkingAndMoving()
        {
            var e = Track(EnemySpawner.Spawn(EnemyCatalog.Find("bat"), null, new Vector3(0f, 3000f, 30f), "freeze#0"));
            var playerGo = new GameObject("FreezePlayer");
            cleanup.Add(playerGo);
            playerGo.transform.position = new Vector3(0f, 3000f, 40f);
            e.SetTarget(playerGo.AddComponent<PlayerHealth>());
            Enemy.FreezeAI = true;
            Vector3 start = e.transform.position;
            for (int i = 0; i < 60; i++) e.Tick(1f / 60f);
            Assert.AreEqual(start, e.transform.position);
            Assert.AreEqual("", e.DebugLabel, "its brain never started");
            Enemy.FreezeAI = false;
            for (int i = 0; i < 60; i++) e.Tick(1f / 60f);
            Assert.AreNotEqual(start, e.transform.position);
        }

        static List<string> Hierarchy(Scene scene) =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Select(t => $"{t.gameObject.scene.name}:{t.name}@{t.position}").ToList();

        [Test]
        public void SmokeTest_AllEnemiesPass_AndTheOpenSceneIsUnchanged()
        {
            // Run straight in the open scene (edit mode cannot add a scene next to the test runner's untitled one):
            // the strictest case, since every object must be removed again by the smoke test itself.
            var scene = SceneManager.GetActiveScene();
            var before = Hierarchy(scene);
            int scenesBefore = SceneManager.sceneCount;

            var report = EnemySmokeTest.RunAll(() => scene, _ => { });
            Debug.Log(report.ToString());

            Assert.IsTrue(report.Passed, report.ToString());
            Assert.AreEqual(EnemyCatalog.All.Count + EnemyCatalog.Variants.Count, report.Runs.Count, "every definition and every variant");
            Assert.AreEqual(scenesBefore, SceneManager.sceneCount);
            Assert.AreEqual(scene, SceneManager.GetActiveScene(), "the active scene was restored");
            CollectionAssert.AreEqual(before, Hierarchy(scene), "the open scene has exactly the same objects in the same places");
        }
    }
}
