using NUnit.Framework;

namespace WhiteDragon
{
    public class ShotRecipeTests
    {
        [Test]
        public void FromStats_CopiesStats()
        {
            var stats = new StatBlock();
            stats.AddModifier(new StatModifier(StatType.Damage, ModifierKind.Flat, 1.5f), this);
            var r = ShotRecipe.FromStats(stats);
            Assert.AreEqual(5f, r.Damage, 1e-4f);
            Assert.AreEqual(18f, r.Speed);
            Assert.AreEqual(20f, r.Range);
            Assert.AreEqual(1f, r.SizeScale);
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(0, r.Pierce);
            Assert.AreEqual(0f, r.SpreadDegrees);
            Assert.AreEqual(DamageType.Physical, r.DamageType);
        }

        [Test]
        public void Clamp_LowerBounds()
        {
            var r = new ShotRecipe { Count = -3, Pierce = -1, SizeScale = 0f, Speed = 0f, Range = 0f, SpreadDegrees = -10f };
            r.Clamp();
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(0, r.Pierce);
            Assert.AreEqual(0.3f, r.SizeScale, 1e-5f);
            Assert.AreEqual(2f, r.Speed);
            Assert.AreEqual(2f, r.Range);
            Assert.AreEqual(0f, r.SpreadDegrees);
        }

        [Test]
        public void Clamp_UpperBounds()
        {
            var r = new ShotRecipe { Count = 99, Pierce = 99, SizeScale = 99f, Speed = 999f, Range = 999f, SpreadDegrees = 999f };
            r.Clamp();
            Assert.AreEqual(12, r.Count);
            Assert.AreEqual(10, r.Pierce);
            Assert.AreEqual(4f, r.SizeScale);
            Assert.AreEqual(80f, r.Speed);
            Assert.AreEqual(100f, r.Range);
            Assert.AreEqual(120f, r.SpreadDegrees);
        }

        [Test]
        public void Clamp_MultiShotGetsMinimumSpread()
        {
            var r = new ShotRecipe { Count = 3, SpreadDegrees = 0f };
            r.Clamp();
            Assert.AreEqual(8f, r.SpreadDegrees);

            var wide = new ShotRecipe { Count = 3, SpreadDegrees = 30f };
            wide.Clamp();
            Assert.AreEqual(30f, wide.SpreadDegrees);

            var single = new ShotRecipe { Count = 1, SpreadDegrees = 0f };
            single.Clamp();
            Assert.AreEqual(0f, single.SpreadDegrees);
        }

        [Test]
        public void FanAngle_IsSymmetric()
        {
            Assert.AreEqual(0f, ShotRecipe.FanAngle(0, 1, 40f));
            Assert.AreEqual(-10f, ShotRecipe.FanAngle(0, 3, 20f));
            Assert.AreEqual(0f, ShotRecipe.FanAngle(1, 3, 20f));
            Assert.AreEqual(10f, ShotRecipe.FanAngle(2, 3, 20f));
        }
    }
}
