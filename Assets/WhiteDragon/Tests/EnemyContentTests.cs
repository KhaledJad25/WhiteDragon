using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>The shipped skeleton, bat and variants, run against a scripted player in a test arena.</summary>
    public class EnemyContentTests
    {
        const string Undead = "Assets/WhiteDragon/Data/Resources/Enemies/Undead/";
        const string Beast = "Assets/WhiteDragon/Data/Resources/Enemies/Beast/";
        static readonly Vector3 Arena = new Vector3(5000f, 0f, 5000f);

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

        void Floor()
        {
            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.name = "TestFloor";
            floor.transform.position = Arena + new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(80f, 1f, 80f);
        }

        PlayerHealth Player(Vector3 position, bool withBody = true)
        {
            var go = Track(new GameObject("ScriptedPlayer"));
            go.transform.position = position;
            if (withBody)
            {
                var cc = go.AddComponent<CharacterController>();
                cc.radius = 0.35f;
                cc.height = 1.8f;
                cc.center = new Vector3(0f, 0.9f, 0f);
            }
            var p = go.AddComponent<PlayerHealth>();
            p.startingContainers = 100;
            p.State.InvincibilitySeconds = 0f;
            return p;
        }

        Enemy Spawn(string prefabPath, Vector3 position, string key, PlayerHealth target, EnemyVariant variant = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var go = Track(Object.Instantiate(prefab, position, Quaternion.identity));
            var e = go.GetComponent<Enemy>();
            e.variant = variant;
            e.SetSpawnKey(key);
            e.Initialize();
            e.SetTarget(target);
            return e;
        }

        static string StateOf(Enemy e) => e.Brain is StateMachineBrain sm ? sm.CurrentStateName(e.BrainState) : "";

        // ---------- Contact and line of sight ----------

        [Test]
        public void Contact_FlyingEnemyAtHeadHeight_Touches()
        {
            Floor();
            var player = Player(Arena);
            var bat = Spawn(Beast + "Enemy_Bat.prefab", Arena + new Vector3(0f, 1.5f, 0.6f), "test#0", player);
            Physics.SyncTransforms();
            Assert.IsTrue(bat.Context.InContactRange(0.3f), "a bat at the player's head touches it (its feet are 1.5 m up, more than its height)");
            bat.transform.position = Arena + new Vector3(0f, 3f, 0.6f);
            Physics.SyncTransforms();
            Assert.IsFalse(bat.Context.InContactRange(0.3f), "well above the head does not");
        }

        [Test]
        public void LineOfSight_OtherEnemiesDoNotBlock_WallsDo()
        {
            Floor();
            var player = Player(Arena);
            var skeleton = Spawn(Undead + "Enemy_Skeleton.prefab", Arena + new Vector3(0f, 0f, 10f), "test#0", player);
            var blocker = Spawn(Undead + "Enemy_Skeleton.prefab", Arena + new Vector3(0f, 0f, 5f), "test#1", player);
            Physics.SyncTransforms();
            Assert.IsTrue(skeleton.Context.HasLineOfSight(), "shots fly through enemies, so they do not block sight");

            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = Arena + new Vector3(0f, 1.5f, 3f);
            wall.transform.localScale = new Vector3(6f, 3f, 0.4f);
            Physics.SyncTransforms();
            Assert.IsFalse(skeleton.Context.HasLineOfSight());
            Assert.IsNotNull(blocker);
        }

        // ---------- Skeleton ----------

        [Test]
        public void Skeleton_FiresOnlyWithLineOfSight_OtherwiseRepositions()
        {
            Floor();
            var player = Player(Arena);
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = Arena + new Vector3(0f, 1.5f, 5f);
            wall.transform.localScale = new Vector3(12f, 3f, 0.4f);
            var skeleton = Spawn(Undead + "Enemy_Skeleton.prefab", Arena + new Vector3(0f, 0f, 10f), "test#0", player);
            Physics.SyncTransforms();

            var states = new HashSet<string>();
            for (int i = 0; i < 120; i++)
            {
                skeleton.Tick(1f / 60f);
                states.Add(StateOf(skeleton));
            }
            Assert.AreEqual(0, Projectile.LiveCount, "no shot without line of sight");
            Assert.IsTrue(states.Contains("Reposition"), "it moves to find a line of sight");
            CollectionAssert.DoesNotContain(states, "Telegraph");

            Object.DestroyImmediate(wall);
            Physics.SyncTransforms();
            for (int i = 0; i < 300 && Projectile.LiveCount == 0; i++) skeleton.Tick(1f / 60f);
            Assert.AreEqual(1, Projectile.LiveCount, "with sight it telegraphs, then fires one shot");
        }

        // ---------- Determinism ----------

        List<string> RunSkeletonAndBat(int seed)
        {
            RunSession.StartRun(seed);
            Floor();
            var player = Player(Arena);
            var skeleton = Spawn(Undead + "Enemy_Skeleton.prefab", Arena + new Vector3(0f, 0f, 10f), "arena#0", player);
            var bat = Spawn(Beast + "Enemy_Bat.prefab", Arena + new Vector3(4f, 1.5f, 4f), "arena#1", player);
            Physics.SyncTransforms();

            var log = new List<string>();
            string sState = "", bState = "";
            const float dt = 1f / 60f;
            for (int i = 0; i < 600; i++)
            {
                float t = i * dt;
                player.transform.position = Arena + new Vector3(Mathf.Cos(t * 0.5f) * 3f, 0f, Mathf.Sin(t * 0.5f) * 3f);
                Physics.SyncTransforms();
                skeleton.Tick(dt);
                bat.Tick(dt);
                if (StateOf(skeleton) != sState) log.Add($"{i} skeleton {sState = StateOf(skeleton)}");
                if (StateOf(bat) != bState) log.Add($"{i} bat {bState = StateOf(bat)}");
                log.Add($"{i} {skeleton.transform.position.x:R},{skeleton.transform.position.z:R} {bat.transform.position.x:R},{bat.transform.position.y:R},{bat.transform.position.z:R}");
            }
            foreach (var o in cleanup) if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
            Projectile.ClearAll();
            return log;
        }

        [Test]
        public void SkeletonAndBat_SameSeed_SameRun_NewSeed_Differs()
        {
            var first = RunSkeletonAndBat(4242);
            var again = RunSkeletonAndBat(4242);
            CollectionAssert.AreEqual(first, again, "10 s at a fixed step: positions and transitions identical");
            Assert.IsTrue(first.Any(l => l.Contains("bat Dash")) && first.Any(l => l.Contains("skeleton Shoot")), "both ran their attacks");

            var other = RunSkeletonAndBat(777);
            CollectionAssert.AreNotEqual(first, other, "another seed: different hover drift and strafe");
        }

        float BatCycleSeconds(float fps)
        {
            Floor();
            // No body: the dash is never cut short by bumping into the player, so the cycle is the full rhythm.
            var player = Player(Arena, withBody: false);
            var bat = Spawn(Beast + "Enemy_Bat.prefab", Arena + new Vector3(4f, 1.5f, 4f), "arena#1", player);
            Physics.SyncTransforms();
            float dt = 1f / fps, time = 0f, start = -1f;
            string last = "";
            for (int i = 0; i < 20 * fps; i++)
            {
                bat.Tick(dt);
                time += dt;
                string now = StateOf(bat);
                if (now != last && now == "Hover")
                {
                    if (start >= 0f) return time - start;
                    start = time;
                }
                last = now;
            }
            return -1f;
        }

        [Test]
        public void BatCycle_SameDuration_At30And60Fps()
        {
            float at60 = BatCycleSeconds(60f);
            foreach (var o in cleanup) if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
            float at30 = BatCycleSeconds(30f);
            Assert.Greater(at60, 0f);
            Assert.AreEqual(at60, at30, 1f / 30f + 1e-4f, "within one frame");
            Assert.AreEqual(1.5f + 0.5f + 0.4f + 0.8f, at60, 1f / 30f + 1e-3f, "hover + telegraph + dash + recover");
        }

        [Test]
        public void BatDash_StopsAtAWall()
        {
            Floor();
            var player = Player(Arena + new Vector3(0f, 0f, 8f));
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = Arena + new Vector3(0f, 1.5f, 3f);
            wall.transform.localScale = new Vector3(30f, 6f, 0.4f);
            var bat = Spawn(Beast + "Enemy_Bat.prefab", Arena + new Vector3(0f, 1.5f, 0f), "arena#1", player);
            Physics.SyncTransforms();
            float dashTime = 0f;
            for (int i = 0; i < 600; i++)
            {
                bat.Tick(1f / 60f);
                if (StateOf(bat) == "Dash") dashTime += 1f / 60f;
                if (StateOf(bat) == "Recover") break;
            }
            Assert.Less(dashTime, 0.39f, "the dash ended early at the wall");
            Assert.Less(bat.transform.position.z - Arena.z, 3f, "it did not pass the wall");
        }

        // ---------- Variants ----------

        Enemy Bare(EnemyDefinition def, EnemyVariant variant)
        {
            var go = Track(new GameObject(def.id));
            go.transform.position = Arena + new Vector3(0f, 0f, 20f);
            go.AddComponent<CharacterController>();
            var e = go.AddComponent<Enemy>();
            e.definition = def;
            e.variant = variant;
            e.Initialize();
            return e;
        }

        [Test]
        public void Variant_ArmoredZombie_MoreHealth_Slower()
        {
            var v = EnemyCatalog.FindVariant("armored_zombie");
            var e = Bare(v.baseEnemy, v);
            Assert.AreEqual("ghoul", v.baseEnemy.id);
            Assert.AreEqual(20f * 2.5f, e.Stats.MaxHealth, 1e-4f);
            Assert.AreEqual(2.5f * 0.7f, e.Stats.MoveSpeed, 1e-4f);
            Assert.AreEqual(1, e.Stats.ContactDamage);
            Assert.AreSame(v.baseEnemy.brain, e.Brain, "still the zombie brain");
        }

        [Test]
        public void Variant_FastBat_Faster()
        {
            var v = EnemyCatalog.FindVariant("fast_bat");
            var e = Bare(v.baseEnemy, v);
            Assert.AreEqual("bat", v.baseEnemy.id);
            Assert.AreEqual(4f * 1.6f, e.Stats.MoveSpeed, 1e-4f);
            Assert.AreEqual(8f * 0.75f, e.Stats.MaxHealth, 1e-4f);
            Assert.AreEqual(0.85f, e.transform.localScale.x, 1e-4f);
        }

        [Test]
        public void Variant_EliteSkeleton_FiresThreeWithItsOwnBrain()
        {
            var v = EnemyCatalog.FindVariant("elite_skeleton");
            Floor();
            var player = Player(Arena);
            var e = Spawn(Undead + "Enemy_Skeleton.prefab", Arena + new Vector3(0f, 0f, 10f), "test#0", player, v);
            Physics.SyncTransforms();
            Assert.AreSame(v.brain, e.Brain, "brain override");
            Assert.AreNotSame(v.baseEnemy.brain, e.Brain);
            for (int i = 0; i < 300 && Projectile.LiveCount == 0; i++) e.Tick(1f / 60f);
            Assert.AreEqual(3, Projectile.LiveCount, "three shots at once");
            Assert.AreEqual(15f * 1.5f, e.Stats.MaxHealth, 1e-4f);
        }
    }
}
