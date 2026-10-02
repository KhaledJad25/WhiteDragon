using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class VisualHookTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            ProjectileVisuals.Current = null;
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Track<T>(T o) where T : Object
        {
            cleanup.Add(o);
            return o;
        }

        /// <summary>A stand-in "prefab": any GameObject works as an Instantiate template.</summary>
        GameObject FakePrefab(string name, bool withCollider = false)
        {
            var go = Track(withCollider ? GameObject.CreatePrimitive(PrimitiveType.Cube) : new GameObject());
            go.name = name;
            go.transform.position = new Vector3(0f, 900f, 0f);
            return go;
        }

        static Color BlockColor(Renderer r)
        {
            var b = new MaterialPropertyBlock();
            r.GetPropertyBlock(b);
            return b.GetColor("_Color");
        }

        // ---------- Tint helper ----------

        [Test]
        public void Tint_LayersAndRestoresOriginal()
        {
            var cube = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            var r = cube.GetComponent<Renderer>();
            var tint = cube.AddComponent<RendererTint>();
            Assert.IsFalse(r.HasPropertyBlock());

            tint.SetBaseColor(Color.red);
            Assert.AreEqual(Color.red, BlockColor(r));

            tint.Flash(Color.white, 0.1f);
            Assert.AreEqual(Color.white, BlockColor(r), "full flash at start");
            tint.Tick(0.05f);
            Assert.AreEqual(Color.Lerp(Color.red, Color.white, 0.5f), BlockColor(r));
            tint.Tick(0.06f);
            Assert.AreEqual(Color.red, BlockColor(r), "flash over, back to base");

            tint.SetOverlay(Color.blue, 0.6f);
            TestColors.AssertApprox(Color.Lerp(Color.red, Color.blue, 0.6f), BlockColor(r));

            tint.ClearAll();
            Assert.IsFalse(r.HasPropertyBlock(), "materials render untouched again");
        }

        [Test]
        public void Tint_WithoutBase_FlashesFromMaterialColorAndRestores()
        {
            var cube = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            var r = cube.GetComponent<Renderer>();
            Color original = r.sharedMaterial.color;
            var tint = cube.AddComponent<RendererTint>();
            tint.Flash(Color.white, 0.2f);
            tint.Tick(0.1f);
            Assert.AreEqual(Color.Lerp(original, Color.white, 0.5f), BlockColor(r));
            tint.Tick(0.2f);
            Assert.IsFalse(r.HasPropertyBlock());
        }

        // ---------- Enemy ----------

        Enemy MakeEnemy(GameObject visualPrefab)
        {
            var def = Track(ScriptableObject.CreateInstance<EnemyDefinition>());
            def.visualPrefab = visualPrefab;
            var go = Track(new GameObject("Enemy"));
            go.transform.position = new Vector3(0f, 900f, 0f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(go.transform, false);
            var e = go.AddComponent<Enemy>();
            e.definition = def;
            e.Initialize();
            return e;
        }

        [Test]
        public void Enemy_EmptyVisualPrefab_KeepsPlaceholder()
        {
            var e = MakeEnemy(null);
            Assert.IsNull(e.Visual);
            Assert.IsTrue(e.GetComponentsInChildren<Renderer>().All(r => r.enabled));
        }

        [Test]
        public void Enemy_VisualPrefab_IsSpawned_AndPlaceholderHidden()
        {
            var model = FakePrefab("GhoulModel", withCollider: false);
            model.AddComponent<MeshRenderer>();
            var e = MakeEnemy(model);
            Assert.IsNotNull(e.Visual);
            Assert.AreEqual("GhoulModel", e.Visual.name);
            Assert.AreEqual(e.transform, e.Visual.transform.parent);
            Assert.AreEqual(Vector3.zero, e.Visual.transform.localPosition, "placed at the enemy's pivot");
            Assert.IsTrue(e.Visual.GetComponent<Renderer>().enabled);
            Assert.IsFalse(e.transform.GetChild(0).GetComponent<Renderer>().enabled, "placeholder capsule hidden");
        }

        // ---------- Item pedestal ----------

        [Test]
        public void Pedestal_WorldPrefab_ReplacesSphere_AndEmptyKeepsSphere()
        {
            var pedestal = Track(new GameObject("Pedestal")).AddComponent<ItemPedestal>();
            var plain = Track(ScriptableObject.CreateInstance<ItemDefinition>());
            var fancy = Track(ScriptableObject.CreateInstance<ItemDefinition>());
            fancy.worldPrefab = FakePrefab("SwordModel");

            pedestal.SetItem(plain);
            Assert.IsNull(pedestal.WorldModel);
            Assert.IsTrue(pedestal.PlaceholderRenderer.gameObject.activeSelf);

            pedestal.SetItem(fancy);
            Assert.IsNotNull(pedestal.WorldModel);
            Assert.AreEqual("SwordModel", pedestal.WorldModel.name);
            Assert.IsFalse(pedestal.PlaceholderRenderer.gameObject.activeSelf);

            pedestal.SetItem(plain);
            Assert.IsNull(pedestal.WorldModel, "old model removed");
            Assert.IsTrue(pedestal.PlaceholderRenderer.gameObject.activeSelf);
        }

        // ---------- Projectile ----------

        [Test]
        public void Projectile_NoVisuals_UsesTintedSphere()
        {
            var recipe = new ShotRecipe { Damage = 1f, Speed = 10f, Range = 10f, DamageType = DamageType.Fire };
            var p = Track(Projectile.Spawn(recipe, new Vector3(0f, 900f, 0f), Vector3.forward, null).gameObject);
            var visual = p.transform.Find("Visual");
            Assert.IsNotNull(visual.GetComponent<MeshFilter>());
            Assert.AreEqual(Projectile.BaseRadius * 2f, visual.localScale.x, 1e-5f);
            var renderer = visual.GetComponent<Renderer>();
            TestColors.AssertApprox(DamageTypeColors.Tint(DamageType.Fire), renderer.sharedMaterial.color, "shared material per damage type");
            Assert.IsFalse(renderer.HasPropertyBlock(), "no property block on rocks, so they batch");
        }

        [Test]
        public void Projectile_MappedPrefab_IsSpawnedPerDamageType_WithoutColliders()
        {
            var visuals = Track(ScriptableObject.CreateInstance<ProjectileVisuals>());
            var fireRock = FakePrefab("FireRock", withCollider: true);
            visuals.entries.Add(new ProjectileVisuals.Entry { damageType = DamageType.Fire, prefab = fireRock });
            ProjectileVisuals.Current = visuals;

            var fire = new ShotRecipe { Damage = 1f, Speed = 10f, Range = 10f, DamageType = DamageType.Fire, SizeScale = 2f };
            var p = Track(Projectile.Spawn(fire, new Vector3(0f, 900f, 0f), Vector3.forward, null).gameObject);
            var visual = p.transform.Find("Visual");
            Assert.IsNotNull(visual.GetComponent<MeshRenderer>(), "the prefab's renderer");
            Assert.IsNull(visual.GetComponent<Collider>(), "colliders stripped");
            Assert.AreEqual(2f, visual.localScale.x, 1e-5f, "scaled by SizeScale");
            Assert.IsNotNull(fireRock.GetComponent<Collider>(), "the prefab itself is untouched");

            var physical = new ShotRecipe { Damage = 1f, Speed = 10f, Range = 10f, DamageType = DamageType.Physical };
            var q = Track(Projectile.Spawn(physical, new Vector3(0f, 900f, 0f), Vector3.forward, null).gameObject);
            Assert.AreEqual(Projectile.BaseRadius * 2f, q.transform.Find("Visual").localScale.x, 1e-5f, "unmapped type falls back to the sphere");
        }

        // ---------- Status VFX ----------

        [Test]
        public void StatusVfx_SpawnedWhileActive_RemovedWhenItEnds()
        {
            var go = Track(new GameObject("Target"));
            var receiver = go.AddComponent<StatusReceiver>();
            var burn = Track(ScriptableObject.CreateInstance<StatusEffectDefinition>());
            burn.duration = 1f;
            burn.vfxPrefab = FakePrefab("Flames");
            var slow = Track(ScriptableObject.CreateInstance<StatusEffectDefinition>());
            slow.duration = 1f;

            receiver.Apply(burn, 1);
            receiver.Apply(burn, 1);
            receiver.Apply(slow, 1);
            Assert.AreEqual(1, go.transform.childCount, "one VFX for burn, none for slow");
            Assert.AreEqual("Flames", go.transform.GetChild(0).name);

            receiver.Tick(1.1f);
            Assert.AreEqual(0, go.transform.childCount, "removed on expiry");

            receiver.Apply(burn, 1);
            receiver.ClearAll();
            Assert.AreEqual(0, go.transform.childCount, "removed on ClearAll");
        }

        // ---------- Hand socket ----------

        [Test]
        public void Thrower_UsesHandSocket_OrFallsBackToOffsets()
        {
            var player = Track(new GameObject("Thrower"));
            var cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetParent(player.transform, false);
            cam.transform.position = new Vector3(1f, 2f, 3f);
            var thrower = player.AddComponent<RockThrower>();
            thrower.aimCamera = cam;

            Vector3 expected = cam.transform.position + cam.transform.right * 0.2f + cam.transform.up * -0.25f + cam.transform.forward * 0.6f;
            Assert.That(Vector3.Distance(expected, thrower.HandPosition()), Is.LessThan(1e-5f));

            var socket = new GameObject("Hand").transform;
            socket.SetParent(player.transform, false);
            socket.position = new Vector3(5f, 6f, 7f);
            thrower.handSocket = socket;
            Assert.AreEqual(new Vector3(5f, 6f, 7f), thrower.HandPosition());
        }
    }
}
