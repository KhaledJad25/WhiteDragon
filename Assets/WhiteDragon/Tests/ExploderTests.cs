using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class ExploderTests
    {
        static readonly Vector3 Arena = new Vector3(-6000f, 0f, 6000f);
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        void Floor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cleanup.Add(floor);
            floor.transform.position = Arena + new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(60f, 1f, 60f);
        }

        PlayerHealth Player(Vector3 at, float invincibility)
        {
            var go = new GameObject("ExploderTarget");
            cleanup.Add(go);
            go.transform.position = at;
            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.35f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            var p = go.AddComponent<PlayerHealth>();
            p.State.InvincibilitySeconds = invincibility;
            return p;
        }

        Enemy Exploder(Vector3 at, string key, PlayerHealth target)
        {
            var e = EnemySpawner.Spawn(EnemyCatalog.Find("exploder"), null, at, key);
            cleanup.Add(e.gameObject);
            e.SetTarget(target);
            return e;
        }

        static string StateOf(Enemy e) => e.Brain is StateMachineBrain sm ? sm.CurrentStateName(e.BrainState) : "";

        [Test]
        public void Exploder_Chases_Telegraphs_ThenExplodesForOneHeart_AndDies()
        {
            Floor();
            var player = Player(Arena, 1f);
            var e = Exploder(Arena + new Vector3(0f, 0f, 8f), "boom#0", player);
            Physics.SyncTransforms();
            int red = player.State.Red;
            var seen = new List<string>();
            float telegraphTime = 0f;
            for (int i = 0; i < 600 && !e.IsDead; i++)
            {
                e.Tick(1f / 60f);
                string s = StateOf(e);
                if (s == "Telegraph") telegraphTime += 1f / 60f;
                if (s.Length > 0 && (seen.Count == 0 || seen[seen.Count - 1] != s)) seen.Add(s);
            }
            CollectionAssert.AreEqual(new[] { "Chase", "Telegraph", "Explode" }, seen, "chase, wind up, blow up");
            Assert.AreEqual(0.8f, telegraphTime, 1f / 60f + 1e-3f, "a 0.8 s wind-up first");
            Assert.IsTrue(e.IsDead, "it blows itself up");
            Assert.AreEqual(red - 2, player.State.Red, "one full heart");
        }

        [Test]
        public void TwoBlastsAtOnce_HitOnce_BecauseOfInvincibility()
        {
            Floor();
            var player = Player(Arena, 1f);
            var a = Exploder(Arena + new Vector3(1f, 0f, 1f), "boom#1", player);
            var b = Exploder(Arena + new Vector3(-1f, 0f, 1f), "boom#2", player);
            Physics.SyncTransforms();
            int red = player.State.Red;
            for (int i = 0; i < 120 && (!a.IsDead || !b.IsDead); i++)
            {
                a.Tick(1f / 60f);
                b.Tick(1f / 60f);
            }
            Assert.IsTrue(a.IsDead && b.IsDead);
            Assert.AreEqual(red - 2, player.State.Red, "the second blast lands during the player's invincibility");
        }

        [Test]
        public void Blast_SparesThePlayerOutsideItsRadius_AndNeverHurtsEnemies()
        {
            Floor();
            var player = Player(Arena + new Vector3(0f, 0f, 20f), 0f);
            var e = Exploder(Arena, "boom#3", player);
            var bystander = EnemySpawner.Spawn(EnemyCatalog.Find("ghoul"), null, Arena + new Vector3(1f, 0f, 0f), "boom#4");
            cleanup.Add(bystander.gameObject);
            bystander.SetDormant(true);
            Physics.SyncTransforms();
            int red = player.State.Red;
            float ghoulHealth = bystander.Health;

            var boom = e.Brain as StateMachineBrain;
            var explode = (ExplodeBehavior)boom.states[boom.IndexOf("Explode")].behaviors[0];
            var state = explode.CreateState();
            explode.Enter(e.Context, state);
            explode.Tick(e.Context, state, 1f / 60f);

            Assert.IsTrue(e.IsDead);
            Assert.AreEqual(red, player.State.Red, "20 m away: outside the 3 m blast");
            Assert.AreEqual(ghoulHealth, bystander.Health, "enemy blasts never hurt enemies");
        }

        [Test]
        public void Blast_DoesNotGoThroughWalls()
        {
            Floor();
            var player = Player(Arena + new Vector3(0f, 0f, 2.2f), 0f);
            var e = Exploder(Arena, "boom#5", player);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cleanup.Add(wall);
            wall.transform.position = Arena + new Vector3(0f, 1.5f, 1.1f);
            wall.transform.localScale = new Vector3(6f, 3f, 0.2f);
            Physics.SyncTransforms();
            int red = player.State.Red;

            var boom = e.Brain as StateMachineBrain;
            var explode = (ExplodeBehavior)boom.states[boom.IndexOf("Explode")].behaviors[0];
            var state = explode.CreateState();
            explode.Enter(e.Context, state);
            explode.Tick(e.Context, state, 1f / 60f);

            Assert.IsTrue(e.IsDead);
            Assert.AreEqual(red, player.State.Red, "2.2 m away, inside the radius, but behind a wall");
        }

        [Test]
        public void SelfDestruct_UsesTheNormalDeathPath_AndTheRoomClears()
        {
            Floor();
            var player = Player(Arena + new Vector3(0f, 0f, 1.5f), 1f);
            var roomGo = new GameObject("BoomRoom");
            cleanup.Add(roomGo);
            roomGo.AddComponent<BoxCollider>();
            var room = roomGo.AddComponent<RoomController>();
            room.roomId = "boom_room";
            var e = Exploder(Arena, "unused", player);
            room.enemies.Add(e);
            room.Initialize();
            room.Enter();
            Assert.AreEqual(RoomProgress.Phase.Fighting, room.Phase);

            var burn = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            cleanup.Add(burn);
            burn.duration = 60f;
            e.GetComponent<StatusReceiver>().Apply(burn, 1);
            int died = 0;
            e.Died += _ => died++;
            Physics.SyncTransforms();

            for (int i = 0; i < 300 && !e.IsDead; i++) e.Tick(1f / 60f);
            room.CheckCleared();

            Assert.IsTrue(e.IsDead);
            Assert.AreEqual(1, died, "the same Died event as a kill, once");
            Assert.AreEqual(0, e.GetComponent<StatusReceiver>().Active.Count, "statuses cleared");
            Assert.IsFalse(e.enabled, "no more updates");
            Assert.AreEqual(0, room.AliveCount());
            Assert.AreEqual(RoomProgress.Phase.Cleared, room.Phase, "the room clears");
        }
    }
}
