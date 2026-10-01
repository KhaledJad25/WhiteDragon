using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class ItemSystemTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            cleanup.Add(o);
            return o;
        }

        ItemDefinition Item(string tag = null, params StatModifier[] mods)
        {
            var i = Make<ItemDefinition>();
            i.tags = tag == null ? new string[0] : new[] { tag };
            i.statModifiers = mods;
            return i;
        }

        SynergyDefinition Synergy(string tag, int required, params StatModifier[] mods)
        {
            var s = Make<SynergyDefinition>();
            s.tag = tag;
            s.requiredCount = required;
            s.statModifiers = mods;
            return s;
        }

        static StatModifier Dmg(float v) => new StatModifier(StatType.Damage, ModifierKind.Flat, v);

        // ---------- Loadout ----------

        [Test]
        public void NewItemAsset_HasSafeDefaults()
        {
            var i = Make<ItemDefinition>();
            Assert.AreEqual(1f, i.weightMultiplier);
            Assert.AreEqual(1f, i.recipeEdits.sizeMultiplier);
            Assert.AreEqual("", i.requiredUnlockId);
        }

        [Test]
        public void SameItemTwice_Stacks()
        {
            var stats = new StatBlock();
            var loadout = new ItemLoadout(stats);
            var flint = Item(null, Dmg(1f));
            loadout.Add(flint);
            loadout.Add(flint);
            Assert.AreEqual(5.5f, stats.Get(StatType.Damage), 1e-4f);
            Assert.AreEqual(2, loadout.Items.Count);
        }

        [Test]
        public void RemoveLast_And_Clear_RemoveModifiersCleanly()
        {
            var stats = new StatBlock();
            var loadout = new ItemLoadout(stats);
            var flint = Item(null, Dmg(1f));
            loadout.Add(flint);
            loadout.Add(flint);
            Assert.IsTrue(loadout.RemoveLast());
            Assert.AreEqual(4.5f, stats.Get(StatType.Damage), 1e-4f);
            loadout.Add(Item(null, new StatModifier(StatType.MoveSpeed, ModifierKind.PercentAdd, 0.5f)));
            loadout.Clear();
            Assert.AreEqual(3.5f, stats.Get(StatType.Damage), 1e-4f);
            Assert.AreEqual(5f, stats.Get(StatType.MoveSpeed), 1e-4f);
            Assert.AreEqual(0, stats.ModifierCount);
            Assert.IsFalse(loadout.RemoveLast());
        }

        [Test]
        public void RecipeEdits_ApplyInPickupOrder()
        {
            var stats = new StatBlock();
            var loadout = new ItemLoadout(stats);
            var fire = Item();
            fire.recipeEdits.overrideDamageType = true;
            fire.recipeEdits.damageType = DamageType.Fire;
            var dark = Item();
            dark.recipeEdits.overrideDamageType = true;
            dark.recipeEdits.damageType = DamageType.Dark;
            dark.recipeEdits.projectileCountAdd = 2;

            loadout.Add(fire);
            loadout.Add(dark);
            var r = ShotRecipeBuilder.Build(stats, loadout);
            Assert.AreEqual(DamageType.Dark, r.DamageType);
            Assert.AreEqual(3, r.Count);
            Assert.GreaterOrEqual(r.SpreadDegrees, 8f);

            loadout.Clear();
            loadout.Add(dark);
            loadout.Add(fire);
            Assert.AreEqual(DamageType.Fire, ShotRecipeBuilder.Build(stats, loadout).DamageType);
        }

        [Test]
        public void RecipeBuild_ClampsAfterItems()
        {
            var stats = new StatBlock();
            var loadout = new ItemLoadout(stats);
            var pouch = Item();
            pouch.recipeEdits.projectileCountAdd = 5;
            pouch.recipeEdits.pierceAdd = 4;
            pouch.recipeEdits.sizeMultiplier = 3f;
            for (int i = 0; i < 4; i++) loadout.Add(pouch);
            var r = ShotRecipeBuilder.Build(stats, loadout);
            Assert.AreEqual(12, r.Count);
            Assert.AreEqual(10, r.Pierce);
            Assert.AreEqual(4f, r.SizeScale);
        }

        // ---------- Synergies ----------

        [Test]
        public void Synergy_ActivatesAtThreshold_OnlyOnce_AndDeactivates()
        {
            var stats = new StatBlock();
            var syn = Synergy("fire", 2, Dmg(10f));
            var loadout = new ItemLoadout(stats, new[] { syn });

            loadout.Add(Item("fire"));
            Assert.AreEqual(0, loadout.ActiveSynergies.Count);
            Assert.AreEqual(3.5f, stats.Get(StatType.Damage), 1e-4f);

            loadout.Add(Item("FIRE"));
            Assert.AreEqual(1, loadout.ActiveSynergies.Count);
            Assert.AreEqual(13.5f, stats.Get(StatType.Damage), 1e-4f);

            loadout.Add(Item("fire"));
            Assert.AreEqual(13.5f, stats.Get(StatType.Damage), 1e-4f, "never double-applied");

            loadout.RemoveLast();
            loadout.RemoveLast();
            Assert.AreEqual(0, loadout.ActiveSynergies.Count);
            Assert.AreEqual(3.5f, stats.Get(StatType.Damage), 1e-4f);
        }

        [Test]
        public void Synergy_RecipeEditsAndEffects_Apply()
        {
            var stats = new StatBlock();
            var effect = Make<RecordingEffect>();
            var syn = Synergy("rock", 1);
            syn.recipeEdits.pierceAdd = 3;
            syn.effects.Add(effect);
            var loadout = new ItemLoadout(stats, new[] { syn });
            loadout.Add(Item("rock"));
            var r = ShotRecipeBuilder.Build(stats, loadout);
            Assert.AreEqual(3, r.Pierce);
            Assert.AreEqual(1, r.GetStacks(effect));
        }

        [Test]
        public void Clear_DeactivatesSynergies()
        {
            var stats = new StatBlock();
            var loadout = new ItemLoadout(stats, new[] { Synergy("x", 1, Dmg(1f)) });
            loadout.Add(Item("x"));
            loadout.Clear();
            Assert.AreEqual(0, loadout.ActiveSynergies.Count);
            Assert.AreEqual(0, stats.ModifierCount);
        }

        // ---------- Effects ----------

        [Test]
        public void SameEffectFromSeveralSources_AppearsOnceWithStacks()
        {
            var stats = new StatBlock();
            var effect = Make<RecordingEffect>();
            var a = Item("t");
            a.effects.Add(effect);
            var syn = Synergy("t", 2);
            syn.effects.Add(effect);
            var loadout = new ItemLoadout(stats, new[] { syn });
            loadout.Add(a);
            loadout.Add(a);

            var r = ShotRecipeBuilder.Build(stats, loadout);
            Assert.AreEqual(1, r.Effects.Count);
            Assert.AreEqual(3, r.Effects[0].Stacks);
            Assert.AreEqual(1, effect.ModifyCalls);
            Assert.AreEqual(3, effect.LastStacks);
        }

        [Test]
        public void ProjectileCallsEffectHooks()
        {
            var effect = Make<RecordingEffect>();
            var recipe = new ShotRecipe { Damage = 2f, Speed = 20f, Range = 50f };
            recipe.AddEffect(effect, 2);
            var target = MakeTarget(new Vector3(0f, 500f, 3f));

            var p = Projectile.Spawn(recipe, new Vector3(0f, 500f, 0f), Vector3.forward, null);
            cleanup.Add(p.gameObject);
            Assert.AreEqual(1, effect.SpawnCalls);

            FlyUntilDone(p);
            Assert.Greater(effect.UpdateCalls, 0);
            Assert.AreEqual(1, effect.HitCalls);
            Assert.AreEqual(2f, target.TotalDamage, 1e-4f);
        }

        [Test]
        public void ItemMadeOnlyFromAssets_AppliesStatusOnHit()
        {
            var burn = Make<StatusEffectDefinition>();
            burn.duration = 3f;
            burn.damagePerSecond = 2f;
            var apply = Make<ApplyStatusEffect>();
            apply.status = burn;
            var item = Item();
            item.effects.Add(apply);

            var stats = new StatBlock();
            var loadout = new ItemLoadout(stats);
            loadout.Add(item);
            var recipe = ShotRecipeBuilder.Build(stats, loadout);

            var target = MakeTarget(new Vector3(0f, 520f, 3f));
            var receiver = target.gameObject.AddComponent<StatusReceiver>();
            var p = Projectile.Spawn(recipe, new Vector3(0f, 520f, 0f), Vector3.forward, null);
            cleanup.Add(p.gameObject);
            FlyUntilDone(p);

            Assert.AreEqual(1, receiver.GetStacks(burn));
        }

        [Test]
        public void SplitFragments_NeverDamageTheEnemyThatSpawnedThem()
        {
            var split = Make<SplitOnHitEffect>();
            split.fragmentsPerStack = 2;
            split.spreadDegrees = 0f;
            var recipe = new ShotRecipe { Damage = 4f, Speed = 20f, Range = 50f };
            recipe.AddEffect(split);
            var target = MakeTarget(new Vector3(0f, 540f, 3f));

            var p = Projectile.Spawn(recipe, new Vector3(0f, 540f, 0f), Vector3.forward, null);
            cleanup.Add(p.gameObject);
            FlyUntilDone(p);

            var fragments = Object.FindObjectsByType<Projectile>();
            foreach (var f in fragments) cleanup.Add(f.gameObject);
            Assert.AreEqual(2, fragments.Length, "fragments were spawned");
            foreach (var f in fragments) FlyUntilDone(f);

            Assert.AreEqual(1, target.Hits, "only the original rock hit");
            Assert.AreEqual(4f, target.TotalDamage, 1e-4f);
        }

        TestDamageable MakeTarget(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = position;
            cleanup.Add(go);
            Physics.SyncTransforms();
            return go.AddComponent<TestDamageable>();
        }

        static void FlyUntilDone(Projectile p)
        {
            for (int i = 0; i < 100 && p != null && !p.IsDespawned; i++)
                p.Tick(0.02f);
        }

        // ---------- Statuses ----------

        StatusEffectDefinition Status(StatusStacking stacking, int maxStacks = 3, float dps = 2f, float duration = 3f, float interval = 0.5f, float speed = 1f)
        {
            var s = Make<StatusEffectDefinition>();
            s.stacking = stacking;
            s.maxStacks = maxStacks;
            s.damagePerSecond = dps;
            s.duration = duration;
            s.tickInterval = interval;
            s.speedMultiplier = speed;
            return s;
        }

        (StatusReceiver, TestDamageable) MakeReceiver()
        {
            var go = new GameObject("StatusTarget");
            cleanup.Add(go);
            var d = go.AddComponent<TestDamageable>();
            return (go.AddComponent<StatusReceiver>(), d);
        }

        [Test]
        public void Status_TicksDamage_ThenExpires()
        {
            var (receiver, target) = MakeReceiver();
            var burn = Status(StatusStacking.RefreshDuration);
            receiver.Apply(burn, 1);
            for (int i = 0; i < 6; i++) receiver.Tick(0.5f);
            Assert.AreEqual(6f, target.TotalDamage, 1e-3f);
            Assert.AreEqual(6, target.Hits);
            Assert.AreEqual(0, receiver.Active.Count, "expired");
            receiver.Tick(1f);
            Assert.AreEqual(6f, target.TotalDamage, 1e-3f);
        }

        [Test]
        public void Status_StackIntensity_CapsAndScalesDamage()
        {
            var (receiver, target) = MakeReceiver();
            var burn = Status(StatusStacking.StackIntensity, maxStacks: 3);
            receiver.Apply(burn, 2);
            receiver.Apply(burn, 2);
            Assert.AreEqual(3, receiver.GetStacks(burn));
            receiver.Tick(0.5f);
            Assert.AreEqual(3f, target.TotalDamage, 1e-3f);
        }

        [Test]
        public void Status_RefreshDuration_And_Ignore()
        {
            var (receiver, _) = MakeReceiver();
            var refresh = Status(StatusStacking.RefreshDuration, maxStacks: 1, dps: 0f, duration: 2f);
            receiver.Apply(refresh, 1);
            receiver.Tick(1.5f);
            receiver.Apply(refresh, 5);
            Assert.AreEqual(1, receiver.GetStacks(refresh));
            receiver.Tick(1.5f);
            Assert.AreEqual(1, receiver.Active.Count, "duration was refreshed");

            var ignore = Status(StatusStacking.Ignore, maxStacks: 5, dps: 0f, duration: 2f);
            receiver.Apply(ignore, 1);
            receiver.Tick(1.5f);
            receiver.Apply(ignore, 3);
            Assert.AreEqual(1, receiver.GetStacks(ignore));
            receiver.Tick(0.6f);
            Assert.AreEqual(0, receiver.GetStacks(ignore), "not refreshed, so expired");
        }

        [Test]
        public void Status_SpeedMultiplier_Combines()
        {
            var (receiver, _) = MakeReceiver();
            Assert.AreEqual(1f, receiver.SpeedMultiplier);
            receiver.Apply(Status(StatusStacking.RefreshDuration, dps: 0f, speed: 0.5f), 1);
            receiver.Apply(Status(StatusStacking.RefreshDuration, dps: 0f, speed: 0.8f), 1);
            Assert.AreEqual(0.4f, receiver.SpeedMultiplier, 1e-4f);
        }
    }
}
