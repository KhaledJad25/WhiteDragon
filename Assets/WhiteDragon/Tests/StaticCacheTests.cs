using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Static caches must never hand out a destroyed object. Objects made during Play mode are destroyed when
    /// it ends, but statics survive (no domain reload on exit), so caches re-validate before returning.
    /// </summary>
    public class StaticCacheTests
    {
        [Test]
        public void LitMaterial_DestroyedInCache_IsRecreated()
        {
            var color = new Color(0.123f, 0.456f, 0.789f);
            var first = PlaceholderMaterials.Lit(color);
            Object.DestroyImmediate(first);
            var again = PlaceholderMaterials.Lit(color);
            Assert.IsTrue(again != null, "a live material comes back");
            TestColors.AssertApprox(color, again.color);
        }

        [Test]
        public void EmissiveMaterial_DestroyedInCache_IsRecreated()
        {
            var color = new Color(0.321f, 0.654f, 0.987f);
            var first = PlaceholderMaterials.Emissive(color, 0.6f);
            Object.DestroyImmediate(first);
            var again = PlaceholderMaterials.Emissive(color, 0.6f);
            Assert.IsTrue(again != null, "a live material comes back");
            TestColors.AssertApprox(color, again.color);
        }
    }
}
