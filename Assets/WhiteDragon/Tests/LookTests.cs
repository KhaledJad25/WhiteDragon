using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class LookTests
    {
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Track<T>(T o) where T : Object
        {
            cleanup.Add(o);
            return o;
        }

        [Test]
        public void ShippedPalette_LoadsWithEnvironmentMaterials()
        {
            var p = ArtPalette.Current;
            Assert.IsNotNull(p, "Data/Resources/ArtPalette.asset");
            Assert.IsNotEmpty(p.environment);
            foreach (var t in p.environment) Assert.IsNotNull(t.material, "every environment entry has a material");
        }

        [Test]
        public void ApplyToMaterials_SetsPaletteTint_LowSmoothness_NoSpecular()
        {
            var p = Track(ScriptableObject.CreateInstance<ArtPalette>());
            var m = Track(new Material(RenderingDefaults.Current.lit));
            p.environmentSmoothness = 0.1f;
            p.environment.Add(new ArtPalette.MaterialTint { material = m, color = PaletteColor.AshGrey, brightness = 0.5f });

            p.ApplyToMaterials();

            var expected = p.ashGrey * 0.5f;
            expected.a = 1f;
            TestColors.AssertApprox(expected, m.color);
            Assert.AreEqual(0.1f, m.GetFloat("_Smoothness"), 1e-5f);
            Assert.IsTrue(m.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"));
        }

        [Test]
        public void LookRig_AppliesFogAmbientAndSun()
        {
            bool fog = RenderSettings.fog;
            var fogMode = RenderSettings.fogMode;
            float density = RenderSettings.fogDensity;
            Color fogColor = RenderSettings.fogColor, ambient = RenderSettings.ambientLight;
            var ambientMode = RenderSettings.ambientMode;
            try
            {
                var p = Track(ScriptableObject.CreateInstance<ArtPalette>());
                p.fogDensity = 0.05f;
                p.sunIntensity = 0.7f;
                var go = Track(new GameObject("LookRigTest"));
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;
                var rig = go.AddComponent<LookRig>();
                rig.palette = p;
                rig.sun = light;

                rig.Apply();

                Assert.IsTrue(RenderSettings.fog);
                Assert.AreEqual(FogMode.ExponentialSquared, RenderSettings.fogMode);
                Assert.AreEqual(0.05f, RenderSettings.fogDensity, 1e-6f);
                TestColors.AssertApprox(p.nearBlack, RenderSettings.fogColor);
                TestColors.AssertApprox(p.ambient, RenderSettings.ambientLight);
                Assert.AreEqual(0.7f, light.intensity, 1e-6f);
                TestColors.AssertApprox(p.sunColor, light.color);
                Assert.AreEqual(LightShadows.Soft, light.shadows);
            }
            finally
            {
                RenderSettings.fog = fog;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogDensity = density;
                RenderSettings.fogColor = fogColor;
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambient;
            }
        }
    }
}
