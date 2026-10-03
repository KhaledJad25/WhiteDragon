using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class EnemyBrainTests
    {
        readonly List<Object> cleanup = new List<Object>();
        static float nextX;

        [SetUp]
        public void SetUp() => CountingBehavior.Log.Clear();

        [TearDown]
        public void TearDown()
        {
            Projectile.ClearAll();
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Track<T>(T o) where T : Object
        {
            cleanup.Add(o);
            return o;
        }

        T Behavior<T>(string name = null) where T : EnemyBehavior
        {
            var b = Track(ScriptableObject.CreateInstance<T>());
            b.name = name ?? typeof(T).Name;
            return b;
        }

        static BrainState State(string name, params EnemyBehavior[] behaviors) =>
            new BrainState { name = name, behaviors = new List<EnemyBehavior>(behaviors) };

        static BrainState Go(BrainState s, TransitionCondition c, float value, string target)
        {
            s.transitions.Add(new BrainTransition { condition = c, value = value, target = target });
            return s;
        }

        StateMachineBrain Brain(params BrainState[] states)
        {
            var b = Track(ScriptableObject.CreateInstance<StateMachineBrain>());
            b.states = new List<BrainState>(states);
            return b;
        }

        /// <summary>An enemy far from the scene, with a player at the given offset; each test uses its own spot.</summary>
        (Enemy enemy, PlayerHealth player) Make(EnemyBrainDefinition brain, Vector3 playerOffset, float speed = 2.5f,
            int contact = 1, MovementMode movement = MovementMode.Ground, EnemyVariant variant = null)
        {
            nextX += 50f;
            var origin = new Vector3(nextX, 2000f, 0f);
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.maxHealth = 20f;
            def.moveSpeed = speed;
            def.contactDamage = contact;
            def.brain = brain;
            def.movement = movement;
            var go = Track(new GameObject("Enemy"));
            go.transform.position = origin;
            go.AddComponent<CharacterController>();
            var e = go.AddComponent<Enemy>();
            e.definition = def;
            e.variant = variant;
            e.Initialize();

            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = origin + playerOffset;
            playerGo.AddComponent<CharacterController>();
            var player = playerGo.AddComponent<PlayerHealth>();
            player.State.InvincibilitySeconds = 0f;
            e.SetTarget(player);
            Physics.SyncTransforms();
            return (e, player);
        }

        static string StateName(Enemy e) => ((StateMachineBrain)e.Brain).CurrentStateName(e.BrainState);

        static void Run(Enemy e, int frames, float dt)
        {
            for (int i = 0; i < frames; i++) e.Tick(dt);
        }

        // ---------- Transitions ----------

        [Test]
        public void Transitions_Fire_OnFinishedTimeDistanceAndAlways()
        {
            var wait = Behavior<WaitBehavior>();
            wait.duration = 0.5f;
            var brain = Brain(
                Go(State("A", wait), TransitionCondition.BehaviorFinished, 0f, "B"),
                Go(State("B"), TransitionCondition.TimeInState, 0.3f, "C"),
                Go(State("C"), TransitionCondition.DistanceToPlayerAbove, 100f, "Never"),
                State("D"));
            Go(brain.states[2], TransitionCondition.DistanceToPlayerBelow, 100f, "D");
            Go(brain.states[3], TransitionCondition.Always, 0f, "A");
            var (e, _) = Make(brain, new Vector3(0f, 0f, 30f));

            Run(e, 4, 0.1f);
            Assert.AreEqual("A", StateName(e), "0.4 s: still waiting");
            Run(e, 1, 0.1f);
            Assert.AreEqual("B", StateName(e), "0.5 s: Wait finished");
            Run(e, 2, 0.1f);
            Assert.AreEqual("B", StateName(e));
            Run(e, 1, 0.1f);
            Assert.AreEqual("C", StateName(e), "0.3 s in B");
            Run(e, 1, 0.1f);
            Assert.AreEqual("D", StateName(e), "player within 100 m");
            Run(e, 1, 0.1f);
            Assert.AreEqual("A", StateName(e), "Always");
        }

        [Test]
        public void Transitions_HealthTookDamageAndUnknownTarget()
        {
            var brain = Brain(
                Go(Go(State("Calm"), TransitionCondition.Always, 0f, "Missing"), TransitionCondition.TookDamage, 0f, "Hurt"),
                Go(State("Hurt"), TransitionCondition.HealthBelowPercent, 50f, "Low"),
                State("Low"));
            var (e, _) = Make(brain, new Vector3(0f, 0f, 30f));

            Run(e, 3, 0.1f);
            Assert.AreEqual("Calm", StateName(e), "a transition to a missing state is skipped");
            e.TakeDamage(1f, Vector3.zero);
            Run(e, 1, 0.1f);
            Assert.AreEqual("Hurt", StateName(e));
            Run(e, 1, 0.1f);
            Assert.AreEqual("Hurt", StateName(e), "19/20 health");
            e.TakeDamage(10f, Vector3.zero);
            Run(e, 1, 0.1f);
            Assert.AreEqual("Low", StateName(e), "9/20 health");
        }

        [Test]
        public void State_RunsAllItsBehaviorsTogether_InOrder()
        {
            var a = Behavior<CountingBehavior>("a");
            var b = Behavior<CountingBehavior>("b");
            var (e, _) = Make(Brain(State("Both", a, b)), new Vector3(0f, 0f, 30f));
            Run(e, 3, 0.1f);
            Assert.AreEqual(3, a.Last.Ticks);
            Assert.AreEqual(3, b.Last.Ticks);
            CollectionAssert.AreEqual(new[] { "enter:a", "enter:b" }, CountingBehavior.Log);
        }

        [Test]
        public void TwoEnemies_SharingOneBrainAsset_DoNotShareState()
        {
            var wait = Behavior<WaitBehavior>();
            wait.duration = 0.5f;
            var brain = Brain(Go(State("Wait", wait), TransitionCondition.BehaviorFinished, 0f, "Done"), State("Done"));
            var (first, _) = Make(brain, new Vector3(0f, 0f, 30f));
            var (second, _) = Make(brain, new Vector3(0f, 0f, 30f));

            Run(first, 6, 0.1f);
            Run(second, 2, 0.1f);
            Assert.AreEqual("Done", StateName(first));
            Assert.AreEqual("Wait", StateName(second), "the other enemy's timer is its own");
            Assert.AreNotSame(first.BrainState, second.BrainState);
            Run(second, 3, 0.1f);
            Assert.AreEqual("Done", StateName(second));
        }

        // ---------- Frame-rate independence ----------

        static float TelegraphSeconds(EnemyBrainTests t, float fps)
        {
            var tele = t.Behavior<TelegraphBehavior>();
            tele.duration = 0.5f;
            var brain = t.Brain(Go(t.State2("Wind", tele), TransitionCondition.BehaviorFinished, 0f, "Go"), t.State2("Go"));
            var (e, _) = t.Make(brain, new Vector3(0f, 0f, 30f));
            float dt = 1f / fps, time = 0f;
            for (int i = 0; i < 1000; i++)
            {
                e.Tick(dt);
                time += dt;
                if (StateName(e) != "Wind") break;
            }
            return time;
        }

        BrainState State2(string name, params EnemyBehavior[] behaviors) => State(name, behaviors);

        [Test]
        public void Telegraph_FinishesAtTheSameTime_At30And60Fps()
        {
            float at30 = TelegraphSeconds(this, 30f);
            float at60 = TelegraphSeconds(this, 60f);
            Assert.AreEqual(0.5f, at30, 1e-4f);
            Assert.AreEqual(0.5f, at60, 1e-4f);
        }

        float DashDistance(float fps)
        {
            var dash = Behavior<DashBehavior>();
            dash.speed = 14f;
            dash.duration = 0.4f;
            var (e, _) = Make(Brain(State("Dash", dash)), new Vector3(0f, 0f, 30f), movement: MovementMode.Flying);
            Vector3 start = e.transform.position;
            float dt = 1f / fps;
            for (int i = 0; i < Mathf.CeilToInt(fps); i++) e.Tick(dt);
            return Vector3.Distance(start, e.transform.position);
        }

        [Test]
        public void Dash_CoversTheSameDistance_At30And60Fps()
        {
            float at30 = DashDistance(30f);
            float at60 = DashDistance(60f);
            Assert.AreEqual(14f * 0.4f, at60, 0.01f, "speed x duration");
            Assert.AreEqual(at60, at30, 0.01f);
        }

        [Test]
        public void Dash_StopsAtAnObstacle()
        {
            var dash = Behavior<DashBehavior>();
            dash.speed = 14f;
            dash.duration = 0.4f;
            var (e, _) = Make(Brain(Go(State("Dash", dash), TransitionCondition.BehaviorFinished, 0f, "Stop"), State("Stop")),
                new Vector3(0f, 0f, 30f), movement: MovementMode.Flying);
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = e.transform.position + new Vector3(0f, 1f, 2f);
            wall.transform.localScale = new Vector3(6f, 6f, 0.5f);
            Physics.SyncTransforms();
            float startZ = e.transform.position.z;
            for (int i = 0; i < 10; i++)
            {
                e.Tick(1f / 60f);
                if (StateName(e) != "Dash") break;
            }
            Assert.AreEqual("Stop", StateName(e), "finished early, blocked");
            Assert.Less(e.transform.position.z - startZ, 2f, "did not pass the wall");
        }

        // ---------- Variants ----------

        [Test]
        public void Variant_MultipliesStats_AndOverridesTintAndBrain()
        {
            var brainA = Brain(State("A"));
            var brainB = Brain(State("B"));
            var variant = Track(ScriptableObject.CreateInstance<EnemyVariant>());
            variant.healthMultiplier = 2f;
            variant.speedMultiplier = 0.5f;
            variant.damageMultiplier = 1.5f;
            variant.scaleMultiplier = 1.25f;
            variant.overrideTint = true;
            variant.tint = Color.blue;
            variant.brain = brainB;
            var (e, _) = Make(brainA, new Vector3(0f, 0f, 30f), speed: 4f, contact: 2, variant: variant);

            Assert.AreEqual(40f, e.Stats.MaxHealth, 1e-4f);
            Assert.AreEqual(40f, e.Health, 1e-4f);
            Assert.AreEqual(2f, e.Stats.MoveSpeed, 1e-4f);
            Assert.AreEqual(3, e.Stats.ContactDamage, "2 x 1.5 half hearts");
            Assert.AreEqual(1.25f, e.transform.localScale.x, 1e-4f);
            Assert.AreEqual(Color.blue, e.Stats.Tint);
            Assert.AreSame(brainB, e.Brain, "brain override");
        }

        // ---------- Boss-ready hooks ----------

        [Test]
        public void Brain_CanBeSwappedAtRuntime()
        {
            var a = Behavior<CountingBehavior>("a");
            var b = Behavior<CountingBehavior>("b");
            var brainA = Brain(State("A", a));
            var brainB = Brain(State("B", b));
            var (e, _) = Make(brainA, new Vector3(0f, 0f, 30f));
            Run(e, 2, 0.1f);
            Assert.AreEqual("A", e.DebugLabel);

            e.SetBrain(brainB);
            Run(e, 1, 0.1f);
            Assert.AreSame(brainB, e.Brain);
            Assert.AreEqual("B", e.DebugLabel);
            CollectionAssert.AreEqual(new[] { "enter:a", "exit:a", "enter:b" }, CountingBehavior.Log, "old brain ended, new one began");
        }

        [Test]
        public void Invulnerable_TakesNoDamage_AndHealthThresholdFiresOnce()
        {
            var (e, _) = Make(Brain(State("Idle")), new Vector3(0f, 0f, 30f));
            var crossed = new List<float>();
            e.HealthThresholdCrossed += (_, t) => crossed.Add(t);
            e.AddHealthThreshold(0.5f);

            e.Invulnerable = true;
            e.TakeDamage(15f, Vector3.zero);
            Assert.AreEqual(20f, e.Health);
            e.Invulnerable = false;
            e.TakeDamage(9f, Vector3.zero);
            CollectionAssert.IsEmpty(crossed, "11/20 is above half");
            e.TakeDamage(2f, Vector3.zero);
            e.TakeDamage(2f, Vector3.zero);
            CollectionAssert.AreEqual(new[] { 0.5f }, crossed);
        }

        [Test]
        public void ExampleCodeBrain_RunsWithContextHelpers()
        {
            var brain = Track(ScriptableObject.CreateInstance<ExampleCodeBrain>());
            brain.chaseSeconds = 0.3f;
            brain.restSeconds = 0.2f;
            var (e, _) = Make(brain, new Vector3(0f, 0f, 10f), movement: MovementMode.Flying);
            float z = e.transform.position.z;
            Run(e, 2, 0.1f);
            Assert.AreEqual("Chase", e.DebugLabel);
            Assert.Greater(e.transform.position.z, z, "walks at the player");
            Run(e, 1, 0.1f);
            Assert.AreEqual("Rest", e.DebugLabel);
        }

        [Test]
        public void AnimatorDriver_WindupWithoutMapping_PlaysAttack()
        {
            var attackOnly = new List<AnimatorStateDriver.Mapping> { new AnimatorStateDriver.Mapping { state = ActorState.Attack, parameter = "Attack" } };
            Assert.AreEqual(ActorState.Attack, AnimatorStateDriver.Resolve(ActorState.Windup, attackOnly));
            attackOnly.Add(new AnimatorStateDriver.Mapping { state = ActorState.Windup, parameter = "Windup" });
            Assert.AreEqual(ActorState.Windup, AnimatorStateDriver.Resolve(ActorState.Windup, attackOnly));
            Assert.AreEqual(ActorState.Hit, AnimatorStateDriver.Resolve(ActorState.Hit, attackOnly));
        }

        // ---------- Zombie parity ----------

        StateMachineBrain ZombieBrain()
        {
            var move = Behavior<MoveTowardBehavior>();
            move.avoidObstacles = false;
            var contact = Behavior<MeleeContactBehavior>();
            contact.cooldown = 1f;
            contact.extraReach = 0.25f;
            return Brain(State("Chase", move, contact));
        }

        [Test]
        public void ZombieBrain_MovesExactlyLikeTheBuiltInChase()
        {
            var (zombie, zp) = Make(ZombieBrain(), new Vector3(3f, 0f, 9f));
            var (builtIn, bp) = Make(null, new Vector3(3f, 0f, 9f));
            Vector3 zStart = zombie.transform.position, bStart = builtIn.transform.position;
            for (int i = 0; i < 90; i++)
            {
                zombie.Tick(1f / 60f);
                builtIn.Tick(1f / 60f);
                Vector3 moved = zombie.transform.position - zStart, expected = builtIn.transform.position - bStart;
                Assert.Less(Vector3.Distance(expected, moved), 1e-4f, $"frame {i}: {moved} vs {expected}");
                Assert.AreEqual(builtIn.transform.rotation.eulerAngles.y, zombie.transform.rotation.eulerAngles.y, 1e-3f);
            }
            Vector3 flat = zombie.transform.position - zStart;
            flat.y = 0f;
            Assert.AreEqual(2.5f * 1.5f, flat.magnitude, 1e-3f, "2.5 m/s for 1.5 s");
        }

        [Test]
        public void ZombieBrain_ContactDamage_AndOneSecondCooldown()
        {
            var (e, player) = Make(ZombieBrain(), new Vector3(0f, 0f, 0.8f), contact: 1);
            var seen = new List<ActorState>();
            e.Events.StateRaised += seen.Add;
            int start = player.State.Red;
            e.Tick(1f / 60f);
            CollectionAssert.AreEqual(new[] { ActorState.Move, ActorState.Attack }, seen, "same order as before");
            Assert.AreEqual(start - 1, player.State.Red, "contact damage in half hearts");

            int hits = 1;
            int last = player.State.Red;
            float time = 1f / 60f;
            var hitTimes = new List<float> { 0f };
            for (int i = 0; i < 150; i++)
            {
                e.transform.position = player.transform.position - new Vector3(0f, 0f, 0.8f);
                Physics.SyncTransforms();
                e.Tick(1f / 60f);
                if (player.State.Red != last)
                {
                    hits++;
                    hitTimes.Add(time);
                    last = player.State.Red;
                }
                time += 1f / 60f;
            }
            Assert.AreEqual(3, hits, "hits at 0, 1 and 2 seconds in 2.5 s");
            Assert.AreEqual(1f, hitTimes[1] - hitTimes[0], 1f / 60f + 1e-3f);
            Assert.AreEqual(1f, hitTimes[2] - hitTimes[1], 1f / 60f + 1e-3f);
        }

        [Test]
        public void ShippedGhoulAndBrute_UseTheZombieBrain_WithTodaysNumbers()
        {
            var ghoul = EnemyCatalog.Find("ghoul");
            var brute = EnemyCatalog.Find("brute");
            Assert.IsNotNull(ghoul);
            Assert.IsNotNull(brute);
            Assert.AreSame(ghoul.brain, brute.brain, "one shared zombie brain");
            var brain = (StateMachineBrain)ghoul.brain;
            Assert.AreEqual(1, brain.states.Count);
            var move = (MoveTowardBehavior)brain.states[0].behaviors[0];
            var contact = (MeleeContactBehavior)brain.states[0].behaviors[1];
            Assert.AreEqual(1f, move.speedScale);
            Assert.AreEqual(0f, move.stopDistance);
            Assert.IsFalse(move.avoidObstacles, "straight line, like before");
            Assert.AreEqual(1f, contact.cooldown, "Enemy.contactCooldown default");
            Assert.AreEqual(0.25f, contact.extraReach, "Enemy.contactReach default");
            Assert.AreEqual(1f, contact.damageScale);
            Assert.AreEqual((2.5f, 1, 20f), (ghoul.moveSpeed, ghoul.contactDamage, ghoul.maxHealth));
            Assert.AreEqual((1.6f, 2, 45f), (brute.moveSpeed, brute.contactDamage, brute.maxHealth));
            Assert.AreEqual("undead", ghoul.family);
            Assert.AreEqual(MovementMode.Ground, ghoul.movement);
        }
    }
}
