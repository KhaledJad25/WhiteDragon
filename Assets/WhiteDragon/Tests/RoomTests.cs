using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class RoomTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        GameObject Make(string name, PrimitiveType? primitive = null)
        {
            var go = primitive.HasValue ? GameObject.CreatePrimitive(primitive.Value) : new GameObject(name);
            go.name = name;
            go.transform.position = new Vector3(0f, 800f, 0f);
            cleanup.Add(go);
            return go;
        }

        Enemy MakeEnemy(float hp)
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.maxHealth = hp;
            cleanup.Add(def);
            var go = Make("Enemy");
            var e = go.AddComponent<Enemy>();
            e.definition = def;
            return e;
        }

        // ---------- RoomProgress ----------

        [Test]
        public void Progress_EntersOnce_ClearsOnce()
        {
            var p = new RoomProgress();
            Assert.IsFalse(p.Update(0), "never clears before entry");
            Assert.IsTrue(p.Enter());
            Assert.IsFalse(p.Enter());
            Assert.IsFalse(p.Update(2));
            Assert.AreEqual(RoomProgress.Phase.Fighting, p.Current);
            Assert.IsTrue(p.Update(0));
            Assert.IsFalse(p.Update(0));
            Assert.AreEqual(RoomProgress.Phase.Cleared, p.Current);
            Assert.IsFalse(p.Enter(), "cleared rooms never re-seal");
        }

        // ---------- RoomController ----------

        RoomController MakeRoom(out GameObject door, out ItemPedestal reward, params Enemy[] enemies)
        {
            var room = Make("Room").AddComponent<RoomController>();
            door = Make("Door", PrimitiveType.Cube);
            reward = Make("Reward").AddComponent<ItemPedestal>();
            room.doors.Add(door);
            room.rewardPedestal = reward;
            room.enemies.AddRange(enemies);
            room.Initialize();
            return room;
        }

        [Test]
        public void Room_SealsOnEntry_OpensWhenAllEnemiesDead()
        {
            var a = MakeEnemy(5f);
            var b = MakeEnemy(5f);
            var room = MakeRoom(out var door, out var reward, a, b);
            int clearedBefore = RunSession.RoomsCleared;

            Assert.IsFalse(door.GetComponent<Collider>().enabled, "open before entry");
            Assert.IsFalse(reward.gameObject.activeSelf, "reward hidden");
            Assert.IsTrue(a.IsDormant);

            room.Enter();
            Assert.IsTrue(door.GetComponent<Collider>().enabled);
            Assert.IsTrue(door.GetComponent<Renderer>().enabled);
            Assert.IsFalse(a.IsDormant);

            a.TakeDamage(10f, Vector3.zero);
            room.CheckCleared();
            Assert.AreEqual(RoomProgress.Phase.Fighting, room.Phase);
            Assert.IsTrue(door.GetComponent<Collider>().enabled);

            b.Kill();
            room.CheckCleared();
            Assert.AreEqual(RoomProgress.Phase.Cleared, room.Phase);
            Assert.IsFalse(door.GetComponent<Collider>().enabled);
            Assert.IsFalse(door.GetComponent<Renderer>().enabled);
            Assert.IsTrue(reward.gameObject.activeSelf);
            Assert.AreEqual(clearedBefore + 1, RunSession.RoomsCleared);

            room.CheckCleared();
            Assert.AreEqual(clearedBefore + 1, RunSession.RoomsCleared, "cleared only once");
        }

        [Test]
        public void Room_WithNoEnemies_ClearsOnEntry()
        {
            var room = MakeRoom(out var door, out _);
            room.Enter();
            Assert.AreEqual(RoomProgress.Phase.Cleared, room.Phase);
            Assert.IsFalse(door.GetComponent<Collider>().enabled);
        }

        [Test]
        public void Room_DestroyedEnemyCountsAsDead()
        {
            var a = MakeEnemy(5f);
            var room = MakeRoom(out _, out _, a);
            room.Enter();
            Object.DestroyImmediate(a.gameObject);
            room.CheckCleared();
            Assert.AreEqual(RoomProgress.Phase.Cleared, room.Phase);
        }

        [Test]
        public void Enemy_TakesDamage_AndDiesOnce()
        {
            var e = MakeEnemy(10f);
            int deaths = 0;
            e.Died += _ => deaths++;
            e.TakeDamage(4f, Vector3.zero);
            Assert.AreEqual(6f, e.Health, 1e-4f);
            Assert.IsFalse(e.IsDead);
            e.TakeDamage(6f, Vector3.zero);
            e.TakeDamage(6f, Vector3.zero);
            Assert.IsTrue(e.IsDead);
            Assert.AreEqual(1, deaths);
        }
    }
}
