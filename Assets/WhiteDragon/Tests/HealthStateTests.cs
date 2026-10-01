using NUnit.Framework;

namespace WhiteDragon
{
    public class HealthStateTests
    {
        [Test]
        public void StartsWithThreeFullContainers()
        {
            var h = new HealthState();
            Assert.AreEqual(3, h.RedContainers);
            Assert.AreEqual(6, h.Red);
            Assert.IsFalse(h.IsDead);
        }

        [Test]
        public void Damage_HitsOverlayFirst_LastAddedFirst_ThenRed()
        {
            var h = new HealthState();
            h.AddSoul(2);
            h.AddDark(2);
            Assert.IsTrue(h.TryDamage(1, 0f));
            Assert.AreEqual(1, h.Dark);
            Assert.AreEqual(2, h.Soul);
            Assert.IsTrue(h.TryDamage(2, 10f));
            Assert.AreEqual(0, h.Dark);
            Assert.AreEqual(1, h.Soul);
            Assert.AreEqual(6, h.Red);
            Assert.IsTrue(h.TryDamage(3, 20f));
            Assert.AreEqual(0, h.Soul);
            Assert.AreEqual(4, h.Red);
        }

        [Test]
        public void HalfOverlayHearts_FillUp()
        {
            var h = new HealthState();
            h.AddSoul(1);
            h.AddSoul(1);
            Assert.AreEqual(1, h.Overlay.Count);
            Assert.AreEqual(2, h.Overlay[0].Halves);
            h.AddSoul(3);
            Assert.AreEqual(3, h.Overlay.Count);
            Assert.AreEqual(5, h.Soul);
        }

        [Test]
        public void DarkHeartBroken_FiresOnlyWhenEmptied()
        {
            var h = new HealthState();
            int broken = 0;
            h.DarkHeartBroken += () => broken++;
            h.AddDark(2);
            h.TryDamage(1, 0f);
            Assert.AreEqual(0, broken);
            h.TryDamage(1, 5f);
            Assert.AreEqual(1, broken);
            h.AddSoul(2);
            h.TryDamage(2, 10f);
            Assert.AreEqual(1, broken);
        }

        [Test]
        public void InvincibilityFrames_LastOneSecond()
        {
            var h = new HealthState();
            Assert.IsTrue(h.TryDamage(1, 0f));
            Assert.IsTrue(h.IsInvincible(0.5f));
            Assert.IsFalse(h.TryDamage(1, 0.5f));
            Assert.AreEqual(5, h.Red);
            Assert.IsTrue(h.TryDamage(1, 1.0f));
            Assert.AreEqual(4, h.Red);
        }

        [Test]
        public void Heal_ClampsToContainers()
        {
            var h = new HealthState();
            h.TryDamage(3, 0f);
            h.Heal(10);
            Assert.AreEqual(6, h.Red);
            h.AddContainers(1);
            Assert.AreEqual(4, h.RedContainers);
            Assert.AreEqual(8, h.Red);
            h.AddContainers(1, fill: false);
            Assert.AreEqual(8, h.Red);
            h.Heal(5);
            Assert.AreEqual(10, h.Red);
        }

        [Test]
        public void Death_FiresOnce_AndIgnoresFurtherDamage()
        {
            var h = new HealthState();
            int deaths = 0;
            h.Died += () => deaths++;
            h.AddSoul(1);
            h.TryDamage(6, 0f);
            Assert.IsFalse(h.IsDead, "soul half absorbed one");
            h.TryDamage(1, 2f);
            Assert.IsTrue(h.IsDead);
            Assert.AreEqual(1, deaths);
            Assert.IsFalse(h.TryDamage(1, 10f));
            h.Kill();
            h.Heal(2);
            Assert.AreEqual(0, h.Red);
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void Kill_ClearsEverything()
        {
            var h = new HealthState();
            h.AddDark(4);
            h.Kill();
            Assert.IsTrue(h.IsDead);
            Assert.AreEqual(0, h.Dark);
            Assert.AreEqual(0, h.Red);
        }

        [Test]
        public void SoulOnly_KeepsYouAlive()
        {
            var h = new HealthState(0);
            Assert.IsFalse(h.IsDead);
            h.AddSoul(2);
            h.TryDamage(1, 0f);
            Assert.IsFalse(h.IsDead);
            h.TryDamage(1, 5f);
            Assert.IsTrue(h.IsDead);
        }
    }
}
