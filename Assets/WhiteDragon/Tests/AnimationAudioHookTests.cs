using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class AnimationAudioHookTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            GameFeelSettings.Current = null;
            foreach (var p in Object.FindObjectsByType<Projectile>()) cleanup.Add(p.gameObject);
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Track<T>(T o) where T : Object
        {
            cleanup.Add(o);
            return o;
        }

        static List<ActorState> Record(ActorStateEvents events)
        {
            var seen = new List<ActorState>();
            events.StateRaised += seen.Add;
            return seen;
        }

        // ---------- ActorStateEvents ----------

        [Test]
        public void Events_LocomotionOnlyOnChange_OneShotsEveryTime_DieSticks()
        {
            var events = Track(new GameObject("Actor")).AddComponent<ActorStateEvents>();
            var seen = Record(events);
            events.Raise(ActorState.Idle);
            events.Raise(ActorState.Move);
            events.Raise(ActorState.Move);
            events.Raise(ActorState.Attack);
            events.Raise(ActorState.Attack);
            events.Raise(ActorState.Move);
            events.Raise(ActorState.Hit);
            events.Raise(ActorState.Die);
            events.Raise(ActorState.Idle);
            events.Raise(ActorState.Hit);
            CollectionAssert.AreEqual(new[]
            {
                ActorState.Move, ActorState.Attack, ActorState.Attack, ActorState.Move, ActorState.Hit, ActorState.Die,
            }, seen);
            Assert.AreEqual(ActorState.Die, events.Current);
        }

        [Test]
        public void Locomotion_UsesThreshold()
        {
            Assert.AreEqual(ActorState.Idle, ActorStateEvents.Locomotion(0f));
            Assert.AreEqual(ActorState.Idle, ActorStateEvents.Locomotion(ActorStateEvents.MoveThreshold));
            Assert.AreEqual(ActorState.Move, ActorStateEvents.Locomotion(2.5f));
        }

        // ---------- Enemy ----------

        (Enemy enemy, PlayerHealth player) MakeEnemyNearPlayer(float hp = 10f)
        {
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.maxHealth = hp;
            def.moveSpeed = 2f;
            var enemyGo = Track(new GameObject("Enemy"));
            enemyGo.transform.position = new Vector3(0f, 900f, 0f);
            var enemy = enemyGo.AddComponent<Enemy>();
            enemy.definition = def;
            enemy.Initialize();

            var playerGo = Track(new GameObject("Player"));
            playerGo.transform.position = new Vector3(0f, 900f, 0.8f);
            playerGo.AddComponent<CharacterController>();
            var player = playerGo.AddComponent<PlayerHealth>();
            enemy.SetTarget(player);
            return (enemy, player);
        }

        [Test]
        public void Enemy_RaisesIdleMoveAttackHitDie_AtTheRightMoments()
        {
            var (enemy, player) = MakeEnemyNearPlayer();
            var seen = Record(enemy.Events);

            enemy.SetDormant(true);
            enemy.Tick(0.02f);
            Assert.AreEqual(ActorState.Idle, enemy.Events.Current, "dormant = idle");

            enemy.SetDormant(false);
            enemy.Tick(0.02f);
            CollectionAssert.AreEqual(new[] { ActorState.Move, ActorState.Attack }, seen, "walks toward the player, touches it");
            Assert.AreEqual(5, player.State.Red, "the Attack moment is the contact damage");

            seen.Clear();
            enemy.TakeDamage(3f, Vector3.zero);
            CollectionAssert.AreEqual(new[] { ActorState.Hit }, seen);

            seen.Clear();
            enemy.TakeDamage(50f, Vector3.zero);
            CollectionAssert.AreEqual(new[] { ActorState.Die }, seen, "a lethal hit raises Die, not Hit");
            enemy.Tick(0.02f);
            enemy.TakeDamage(1f, Vector3.zero);
            CollectionAssert.AreEqual(new[] { ActorState.Die }, seen, "nothing after death");
        }

        [Test]
        public void DeathDelay_DefaultsToZero()
        {
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            Assert.AreEqual(0f, def.deathDelay);
        }

        // ---------- Player ----------

        [Test]
        public void Player_DamageRaisesHit_LethalDamageRaisesDie()
        {
            var go = Track(new GameObject("Player"));
            var health = go.AddComponent<PlayerHealth>();
            health.State.InvincibilitySeconds = 0f;
            var seen = Record(ActorStateEvents.For(go));

            health.Damage(1);
            CollectionAssert.AreEqual(new[] { ActorState.Hit }, seen);
            health.Damage(10);
            CollectionAssert.AreEqual(new[] { ActorState.Hit, ActorState.Die }, seen);
        }

        [Test]
        public void Player_ThrowRaisesAttack()
        {
            var go = Track(new GameObject("Thrower"));
            go.transform.position = new Vector3(0f, 900f, 0f);
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetParent(go.transform, false);
            var thrower = go.AddComponent<RockThrower>();
            thrower.aimCamera = cam;
            var seen = Record(ActorStateEvents.For(go));

            thrower.Throw();
            thrower.Throw();
            CollectionAssert.AreEqual(new[] { ActorState.Attack, ActorState.Attack }, seen);
        }

        // ---------- Animator driver ----------

        [Test]
        public void AnimatorDriver_WithoutAnimator_DoesNothing()
        {
            var go = Track(new GameObject("Model"));
            var driver = go.AddComponent<AnimatorStateDriver>();
            driver.mappings.Add(new AnimatorStateDriver.Mapping { state = ActorState.Hit, kind = AnimatorStateDriver.ParameterKind.Trigger, parameter = "Hit" });
            Assert.DoesNotThrow(() => driver.Apply(ActorState.Hit));
        }

        // ---------- GameFeel settings ----------

        [Test]
        public void EmptySettings_KeepGeneratedSounds_AndDefaultStrengths()
        {
            GameFeelSettings.Current = Track(ScriptableObject.CreateInstance<GameFeelSettings>());
            foreach (GameSound s in System.Enum.GetValues(typeof(GameSound)))
            {
                var clip = GameFeel.ClipFor(s);
                Assert.IsNotNull(clip, s.ToString());
                Assert.AreSame(GameFeel.Generated(s), clip, s.ToString());
            }
            Assert.AreEqual("Wall", GameFeel.ClipFor(GameSound.Impact).name, "same generated clip as before");

            float shake = GameFeel.ShakeScale, stop = GameFeel.HitStopScale, volume = GameFeel.SoundVolume;
            GameFeel.ApplySettings(GameFeelSettings.Current);
            Assert.AreEqual(1f, GameFeel.ShakeScale);
            Assert.AreEqual(1f, GameFeel.HitStopScale);
            Assert.AreEqual(0.6f, GameFeel.SoundVolume, 1e-6f);
            GameFeel.ShakeScale = shake;
            GameFeel.HitStopScale = stop;
            GameFeel.SoundVolume = volume;
        }

        [Test]
        public void Settings_AssignedClipReplacesOnlyThatSound()
        {
            var settings = Track(ScriptableObject.CreateInstance<GameFeelSettings>());
            var custom = Track(AudioClip.Create("CustomHit", 100, 1, 44100, false));
            settings.hitSound = custom;
            GameFeelSettings.Current = settings;
            Assert.AreSame(custom, GameFeel.ClipFor(GameSound.Hit));
            Assert.AreSame(GameFeel.Generated(GameSound.Throw), GameFeel.ClipFor(GameSound.Throw));
        }
    }
}
