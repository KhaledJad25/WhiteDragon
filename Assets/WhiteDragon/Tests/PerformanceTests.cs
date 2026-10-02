using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class PerformanceTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            Projectile.ClearAll();
            Projectile.PoolInEditMode = false;
            DamageNumber.ClearAll();
            DamageNumber.RunInEditMode = false;
            GameFeelSettings.Current = null;
            GameFeel.Quality = EffectsQuality.High;
            GameFeel.ResetSoundTimers();
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Track<T>(T o) where T : Object
        {
            cleanup.Add(o);
            return o;
        }

        // ---------- Frame-rate independence ----------

        static int SimulateThrows(float rate, float seconds, params float[] frameTimes)
        {
            var timer = new FireTimer();
            int throws = 0;
            float now = 0f;
            for (int f = 0; now < seconds; f++)
            {
                throws += timer.Tick(now, true, rate);
                now += frameTimes[f % frameTimes.Length];
            }
            return throws;
        }

        [TestCase(2f)]
        [TestCase(6f)]
        [TestCase(10f)]
        [TestCase(45f)]
        public void FireRate_IsTheSameAt30And60Fps(float rate)
        {
            const float seconds = 10f;
            int at30 = SimulateThrows(rate, seconds, 1f / 30f);
            int at60 = SimulateThrows(rate, seconds, 1f / 60f);
            int jittery = SimulateThrows(rate, seconds, 1f / 25f, 1f / 40f, 1f / 33f);
            Assert.AreEqual(rate * seconds, at30, 1.0, "30 fps");
            Assert.AreEqual(rate * seconds, at60, 1.0, "60 fps");
            Assert.AreEqual(rate * seconds, jittery, 1.0, "uneven frames");
        }

        [Test]
        public void FireTimer_FiresAtOnceAfterAPause_WithoutBankedShots()
        {
            var timer = new FireTimer();
            Assert.AreEqual(1, timer.Tick(0f, true, 2f));
            Assert.AreEqual(0, timer.Tick(5f, false, 2f));
            Assert.AreEqual(1, timer.Tick(10f, true, 2f), "fires immediately, not 20 saved-up shots");
            Assert.AreEqual(0, timer.Tick(10.1f, true, 2f));
        }

        static float FlyUntilDone(float fps)
        {
            var recipe = new ShotRecipe { Damage = 1f, Speed = 18f, Range = 20f };
            var p = Projectile.Spawn(recipe, new Vector3(0f, 700f, 0f), Vector3.right, null);
            for (int i = 0; i < 1000 && !p.IsDespawned; i++) p.Tick(1f / fps);
            Assert.IsTrue(p.IsDespawned);
            return p.Travelled;
        }

        [Test]
        public void RockRange_IsExactAt30And60Fps()
        {
            Assert.AreEqual(20f, FlyUntilDone(30f), 1e-3f, "30 fps");
            Assert.AreEqual(20f, FlyUntilDone(60f), 1e-3f, "60 fps");
        }

        // ---------- Projectile pool ----------

        [Test]
        public void ReusedProjectile_CarriesNoStateFromItsPreviousLife()
        {
            Projectile.PoolInEditMode = true;
            var homing = Track(ScriptableObject.CreateInstance<HomingEffect>());
            var ignored = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            ignored.transform.position = new Vector3(0f, 720f, 3f);
            var target = ignored.AddComponent<TestDamageable>();
            Physics.SyncTransforms();
            var owner = Track(new GameObject("Owner")).transform;

            var first = new ShotRecipe { Damage = 5f, Speed = 30f, Range = 50f, Pierce = 3, SizeScale = 2f, DamageType = DamageType.Fire };
            first.AddEffect(homing, 2);
            var a = Projectile.Spawn(first, new Vector3(0f, 720f, 0f), Vector3.forward, owner);
            a.IgnoreTarget(target);
            a.GravityScale = 0.2f;
            a.lifetime = 1f;
            for (int i = 0; i < 5; i++) a.Tick(0.02f);
            Assert.AreEqual(0, target.Hits, "ignored in the first life");
            a.Despawn();
            Assert.IsFalse(a.gameObject.activeSelf, "pooled, not destroyed");

            var second = new ShotRecipe { Damage = 1f, Speed = 18f, Range = 20f, DamageType = DamageType.Physical };
            var b = Projectile.Spawn(second, new Vector3(0f, 720f, 0f), Vector3.forward, null);
            Assert.AreSame(a, b, "the pooled object was reused");
            Assert.IsFalse(b.IsDespawned);
            Assert.AreSame(second, b.Recipe);
            Assert.IsNull(b.Owner);
            Assert.AreEqual(Vector3.forward * 18f, b.Velocity);
            Assert.AreEqual(1f, b.GravityScale);
            Assert.AreEqual(Projectile.DefaultLifetime, b.lifetime);
            Assert.AreEqual(0f, b.Travelled);
            Assert.AreEqual(0, b.EffectCount, "no effects carried over");
            var visual = b.transform.Find("Visual");
            Assert.AreEqual(Projectile.BaseRadius * 2f, visual.localScale.x, 1e-5f, "size reset");
            TestColors.AssertApprox(DamageTypeColors.Tint(DamageType.Physical), visual.GetComponent<Renderer>().sharedMaterial.color, "damage type color reset");

            for (int i = 0; i < 20 && !b.IsDespawned; i++) b.Tick(0.02f);
            Assert.AreEqual(1, target.Hits, "ignore list was cleared: the new rock hits the old ignored target");
            Assert.AreEqual(1f, target.TotalDamage, 1e-4f);

            // Same effect again: its state object is reused but reset.
            var third = new ShotRecipe { Damage = 1f, Speed = 18f, Range = 20f };
            third.AddEffect(homing);
            var c = Projectile.Spawn(third, new Vector3(0f, 800f, 0f), Vector3.forward, null);
            Assert.AreEqual(1, c.EffectCount);
            Assert.AreEqual(1, c.GetEffect(0).Stacks);
        }

        [Test]
        public void HomingState_IsReusedAndCleared()
        {
            var homing = Track(ScriptableObject.CreateInstance<HomingEffect>());
            var state = homing.CreateState();
            Assert.IsTrue(homing.ResetState(state));
            var instance = new ShotEffectInstance(homing, 1, null);
            var before = instance.State;
            instance.Reset(homing, 3, null);
            Assert.AreSame(before, instance.State, "no new allocation for the same effect");
            Assert.AreEqual(3, instance.Stacks);
            var split = Track(ScriptableObject.CreateInstance<SplitOnHitEffect>());
            instance.Reset(split, 1, null);
            Assert.AreSame(split, instance.Effect);
            Assert.IsNull(instance.State, "a different effect gets its own (here: no) state");
        }

        [Test]
        public void LiveProjectileCap_RecyclesTheOldest()
        {
            var settings = Track(ScriptableObject.CreateInstance<GameFeelSettings>());
            settings.maxProjectiles = 3;
            GameFeelSettings.Current = settings;
            Projectile.ClearAll();
            var recipe = new ShotRecipe { Damage = 1f, Speed = 1f, Range = 50f };
            var rocks = new List<Projectile>();
            int recycledBefore = Projectile.RecycledByCap;
            for (int i = 0; i < 4; i++)
                rocks.Add(Projectile.Spawn(recipe, new Vector3(i, 740f, 0f), Vector3.forward, null));
            Assert.AreEqual(3, Projectile.LiveCount);
            Assert.IsTrue(rocks[0].IsDespawned, "oldest recycled");
            Assert.IsFalse(rocks[3].IsDespawned);
            Assert.AreEqual(recycledBefore + 1, Projectile.RecycledByCap);
        }

        // ---------- Damage numbers ----------

        [Test]
        public void DamageNumbers_MergePerTarget_CapAndReuse()
        {
            DamageNumber.RunInEditMode = true;
            var settings = Track(ScriptableObject.CreateInstance<GameFeelSettings>());
            settings.maxDamageNumbers = 2;
            GameFeelSettings.Current = settings;
            object enemyA = new object(), enemyB = new object();

            var a = DamageNumber.Spawn(Vector3.zero, 3f, Color.white, enemyA);
            var merged = DamageNumber.Spawn(Vector3.zero, 2f, Color.white, enemyA);
            Assert.AreSame(a, merged, "same target within the window adds up");
            Assert.AreEqual(5f, a.Amount);
            Assert.AreEqual(1, DamageNumber.LiveCount);

            var b = DamageNumber.Spawn(Vector3.zero, 1f, Color.white, enemyB);
            Assert.AreNotSame(a, b);
            a.Tick(0.2f);
            var c = DamageNumber.Spawn(Vector3.zero, 7f, Color.white, null);
            Assert.AreEqual(2, DamageNumber.LiveCount, "capped at 2");
            Assert.AreSame(a, c, "the oldest was reused");
            Assert.AreEqual(7f, c.Amount);

            b.Tick(1f);
            Assert.IsFalse(b.IsShowing, "finished numbers go back to the pool");
            var d = DamageNumber.Spawn(Vector3.zero, 1f, Color.white, null);
            Assert.AreSame(b, d, "pooled number reused");
        }

        [Test]
        public void DamageNumbers_ShowAuthoredColorInActiveColorSpace()
        {
            // TextMesh vertex colors are not converted in Linear, so the number must convert them itself.
            DamageNumber.RunInEditMode = true;
            Color fire = DamageTypeColors.Tint(DamageType.Fire);
            Color expected = QualitySettings.activeColorSpace == ColorSpace.Linear ? fire.linear : fire;
            var n = DamageNumber.Spawn(Vector3.zero, 3f, fire);
            var tm = n.GetComponent<TextMesh>();
            const float OneStep = 1f / 255f; // TextMesh stores colors as 8 bits per channel
            Assert.That(tm.color.g, Is.EqualTo(expected.g).Within(OneStep), "on spawn");
            n.Tick(0.1f);
            Assert.That(tm.color.g, Is.EqualTo(expected.g).Within(OneStep), "while fading");
            Assert.That(tm.color.a, Is.LessThan(1f), "alpha still fades");
        }

        // ---------- Hit-stop, sounds, quality ----------

        [Test]
        public void HitStop_CannotChainForever_AndHasACooldown()
        {
            var gate = new HitStopGate { MaxDuration = 0.15f, Cooldown = 0.1f };
            Assert.IsTrue(gate.Request(0f, 0.04f));
            Assert.IsFalse(gate.Update(0.03f));
            Assert.IsTrue(gate.Update(0.04f), "a single hit lasts its own length");

            Assert.IsFalse(gate.Request(0.1f, 0.04f), "cooldown");
            Assert.IsTrue(gate.Request(0.2f, 0.09f));
            for (float t = 0.21f; t < 0.5f; t += 0.01f) gate.Request(t, 0.09f);
            Assert.IsFalse(gate.Update(0.34f));
            Assert.IsTrue(gate.Update(0.36f), "constant hits still end at the max duration (0.2 + 0.15)");
        }

        [Test]
        public void SameSound_PlaysAtMostOncePerCooldown()
        {
            GameFeel.ResetSoundTimers();
            Assert.IsTrue(GameFeel.TryClaimSound(GameSound.Hit, 1f));
            Assert.IsFalse(GameFeel.TryClaimSound(GameSound.Hit, 1.01f));
            Assert.IsTrue(GameFeel.TryClaimSound(GameSound.Kill, 1.01f), "other sounds are separate");
            Assert.IsTrue(GameFeel.TryClaimSound(GameSound.Hit, 1f + GameFeel.DefaultSoundCooldown * 2f), "free again after the cooldown");
        }

        [Test]
        public void Quality_ScalesCaps_DefaultIsHigh()
        {
            Assert.AreEqual(EffectsQuality.High, Track(ScriptableObject.CreateInstance<GameFeelSettings>()).quality);
            GameFeelSettings.Current = Track(ScriptableObject.CreateInstance<GameFeelSettings>());
            GameFeel.Quality = EffectsQuality.High;
            Assert.AreEqual(1000, GameFeel.MaxProjectiles);
            Assert.AreEqual(2000, GameFeel.MaxParticles);
            Assert.AreEqual(60, GameFeel.MaxDamageNumbers);
            GameFeel.Quality = EffectsQuality.Medium;
            Assert.AreEqual(500, GameFeel.MaxProjectiles);
            GameFeel.Quality = EffectsQuality.Low;
            Assert.AreEqual(250, GameFeel.MaxProjectiles);
            Assert.AreEqual(15, GameFeel.MaxDamageNumbers);
        }

        // ---------- Enemies ----------

        [Test]
        public void DormantAndDeadEnemies_RunNoUpdate()
        {
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.maxHealth = 5f;
            var go = Track(new GameObject("Enemy"));
            var e = go.AddComponent<Enemy>();
            e.definition = def;
            e.SetDormant(true);
            Assert.IsFalse(e.enabled, "room not entered: no Update");
            e.SetDormant(false);
            Assert.IsTrue(e.enabled);
            e.Kill();
            Assert.IsFalse(e.enabled, "dead: no Update");
            e.SetDormant(false);
            Assert.IsFalse(e.enabled, "stays off after death");
        }
    }
}
