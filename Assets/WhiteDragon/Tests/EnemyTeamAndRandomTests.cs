using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class EnemyTeamAndRandomTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            Projectile.ClearAll();
            RunSession.EndRun();
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Track<T>(T o) where T : Object
        {
            cleanup.Add(o);
            return o;
        }

        // ---------- Teams ----------

        [Test]
        public void TeamRules()
        {
            Assert.IsTrue(Teams.CanDamage(Team.Player, Team.Enemy));
            Assert.IsTrue(Teams.CanDamage(Team.Player, Team.Neutral));
            Assert.IsFalse(Teams.CanDamage(Team.Player, Team.Player), "player rocks never hurt the player");
            Assert.IsTrue(Teams.CanDamage(Team.Enemy, Team.Player));
            Assert.IsFalse(Teams.CanDamage(Team.Enemy, Team.Enemy), "enemies never hurt enemies");
            Assert.IsFalse(Teams.CanDamage(Team.Enemy, Team.Neutral), "enemy attacks hit only the player");
            Assert.IsFalse(Teams.CanDamage(Team.Neutral, Team.Player), "neutral never attacks");
            Assert.IsFalse(Teams.CanDamage(Team.Neutral, Team.Enemy));
        }

        [Test]
        public void EnemyDamage_IsHalfHearts_AtLeastOne()
        {
            Assert.AreEqual(0, Teams.ToHalfHearts(0f));
            Assert.AreEqual(1, Teams.ToHalfHearts(0.2f), "any hit is at least half a heart");
            Assert.AreEqual(1, Teams.ToHalfHearts(1f));
            Assert.AreEqual(2, Teams.ToHalfHearts(2f));
            Assert.AreEqual(3, Teams.ToHalfHearts(2.6f));

            var go = Track(new GameObject("Player"));
            var player = go.AddComponent<PlayerHealth>();
            IDamageable asDamageable = player;
            Assert.AreEqual(Team.Player, asDamageable.Team);
            int red = player.State.Red;
            asDamageable.TakeDamage(2f, Vector3.zero);
            Assert.AreEqual(red - 2, player.State.Red, "2 = one full heart");
            asDamageable.TakeDamage(2f, Vector3.zero);
            Assert.AreEqual(red - 2, player.State.Red, "the usual invincibility still applies");
        }

        [Test]
        public void DefaultTeam_IsNeutral_EnemyIsEnemy()
        {
            IDamageable dummy = Track(new GameObject("Dummy")).AddComponent<TargetDummy>();
            Assert.AreEqual(Team.Neutral, dummy.Team);
            var enemyGo = Track(new GameObject("Enemy"));
            enemyGo.AddComponent<CharacterController>();
            IDamageable enemy = enemyGo.AddComponent<Enemy>();
            Assert.AreEqual(Team.Enemy, enemy.Team);
        }

        GameObject Body(string name, Vector3 position)
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = name;
            go.transform.position = position;
            return go;
        }

        [Test]
        public void EnemyShot_PassesEnemiesAndNeutral_HitsOnlyThePlayer_WallsBlock()
        {
            var origin = new Vector3(0f, 3000f, 0f);
            var enemyGo = Body("Enemy", origin + new Vector3(0f, 0f, 2f));
            enemyGo.AddComponent<CharacterController>();
            var enemy = enemyGo.AddComponent<Enemy>();
            enemy.Initialize();
            var neutral = Body("Neutral", origin + new Vector3(0f, 0f, 4f)).AddComponent<TestDamageable>();
            var playerGo = Body("Player", origin + new Vector3(0f, 0f, 6f));
            var player = playerGo.AddComponent<PlayerHealth>();
            player.State.InvincibilitySeconds = 0f;
            Physics.SyncTransforms();
            int red = player.State.Red;
            float enemyHealth = enemy.Health;

            var recipe = new ShotRecipe { Damage = 1f, Speed = 20f, Range = 30f };
            var shot = Projectile.Spawn(recipe, origin, Vector3.forward, null, Team.Enemy);
            shot.GravityScale = 0f;
            for (int i = 0; i < 60 && !shot.IsDespawned; i++) shot.Tick(0.02f);

            Assert.AreEqual(enemyHealth, enemy.Health, "flew through the other enemy");
            Assert.AreEqual(0, neutral.Hits, "flew through the neutral target");
            Assert.AreEqual(red - 1, player.State.Red, "hit the player for one half heart");

            var wallShot = Projectile.Spawn(recipe, origin + new Vector3(0f, 0f, -6f), Vector3.forward, null, Team.Enemy);
            wallShot.GravityScale = 0f;
            Body("Wall", origin + new Vector3(0f, 0f, -3f));
            Physics.SyncTransforms();
            for (int i = 0; i < 60 && !wallShot.IsDespawned; i++) wallShot.Tick(0.02f);
            Assert.IsTrue(wallShot.IsDespawned);
            Assert.AreEqual(red - 1, player.State.Red, "the wall stopped it");
        }

        [Test]
        public void PlayerShot_PassesThePlayer_HitsEnemyAndNeutral()
        {
            var origin = new Vector3(40f, 3000f, 0f);
            var player = Body("Player", origin + new Vector3(0f, 0f, 2f)).AddComponent<PlayerHealth>();
            var neutral = Body("Neutral", origin + new Vector3(0f, 0f, 4f)).AddComponent<TestDamageable>();
            var enemyGo = Body("Enemy", origin + new Vector3(0f, 0f, 6f));
            enemyGo.AddComponent<CharacterController>();
            var enemy = enemyGo.AddComponent<Enemy>();
            enemy.Initialize();
            Physics.SyncTransforms();
            int red = player.State.Red;
            float enemyHealth = enemy.Health;

            var recipe = new ShotRecipe { Damage = 1f, Speed = 20f, Range = 30f, Pierce = 5 };
            var shot = Projectile.Spawn(recipe, origin, Vector3.forward, null);
            Assert.AreEqual(Team.Player, shot.Team, "rocks default to the player's team");
            shot.GravityScale = 0f;
            for (int i = 0; i < 60 && !shot.IsDespawned; i++) shot.Tick(0.02f);

            Assert.AreEqual(red, player.State.Red, "never hurts the player");
            Assert.AreEqual(1, neutral.Hits);
            Assert.AreEqual(enemyHealth - 1f, enemy.Health, 1e-4f);
        }

        // ---------- Per-enemy randomness ----------

        static float[] Draw(RunRandom r, int n)
        {
            var values = new float[n];
            for (int i = 0; i < n; i++) values[i] = r.Value(RandomStream.Combat);
            return values;
        }

        [Test]
        public void Derive_IsDeterministic_AndIndependent()
        {
            var a = new RunRandom(1234).Derive("room_01#0");
            var b = new RunRandom(1234).Derive("room_01#0");
            CollectionAssert.AreEqual(Draw(a, 8), Draw(b, 8), "same seed and key = same sequence");
            CollectionAssert.AreNotEqual(Draw(new RunRandom(1234).Derive("room_01#1"), 8), Draw(new RunRandom(1234).Derive("room_01#0"), 8), "other key");
            CollectionAssert.AreNotEqual(Draw(new RunRandom(99).Derive("room_01#0"), 8), Draw(new RunRandom(1234).Derive("room_01#0"), 8), "other seed");

            var run = new RunRandom(1234);
            var before = Draw(new RunRandom(1234), 4);
            var child = run.Derive("x");
            Draw(child, 50);
            CollectionAssert.AreEqual(before, Draw(run, 4), "drawing from an enemy's generator never moves the run's");
        }

        Enemy RoomEnemy(string roomKey)
        {
            var go = Track(new GameObject("Enemy"));
            go.AddComponent<CharacterController>();
            var e = go.AddComponent<Enemy>();
            e.SetSpawnKey(roomKey);
            e.SetDormant(true);
            return e;
        }

        [Test]
        public void EnemyGenerator_SameSeedSameRolls_NewSeedDifferent_RunStartRederives()
        {
            var e = RoomEnemy("room_01#0");
            Assert.IsNull(e.Random, "nothing is derived before it wakes");

            RunSession.StartRun(555);
            e.SetDormant(false);
            var first = Draw(e.Random, 6);
            var other = RoomEnemy("room_01#0");
            other.SetDormant(false);
            CollectionAssert.AreEqual(first, Draw(other.Random, 6), "same seed + key = same behavior");

            RunSession.StartRun(556);
            CollectionAssert.AreNotEqual(first, Draw(e.Random, 6), "a new run re-derived it from the new seed");

            RunSession.StartRun(555);
            CollectionAssert.AreEqual(first, Draw(e.Random, 6), "back to the first seed: same rolls again");
        }
    }
}
