using NUnit.Framework;

namespace WhiteDragon
{
    public class DebugPanelTests
    {
        [TestCase("12345", true, 12345)]
        [TestCase("  42 ", true, 42)]
        [TestCase("-7", true, -7)]
        [TestCase("0", false, 0)]
        [TestCase("", false, 0)]
        [TestCase(null, false, 0)]
        [TestCase("abc", false, 0)]
        [TestCase("99999999999", false, 0)]
        [TestCase("1.5", false, 0)]
        public void TryParseSeed_RejectsInvalidInput(string text, bool ok, int expected)
        {
            Assert.AreEqual(ok, DebugPanel.TryParseSeed(text, out int seed));
            if (ok) Assert.AreEqual(expected, seed);
        }

        [Test]
        public void FilterPickups_MatchesIdNameOrTag()
        {
            var coin = UnityEngine.ScriptableObject.CreateInstance<PickupDefinition>();
            coin.id = "big_coin";
            coin.displayName = "Big Coin";
            coin.tags = new[] { "coin" };
            var heart = UnityEngine.ScriptableObject.CreateInstance<PickupDefinition>();
            heart.id = "soul_half";
            heart.displayName = "Half Soul Heart";
            heart.tags = new[] { "heart", "soul" };
            try
            {
                var all = new[] { coin, null, heart };
                CollectionAssert.AreEqual(new[] { coin, heart }, DebugPanel.FilterPickups(all, "  "), "empty query: all (missing skipped)");
                CollectionAssert.AreEqual(new[] { coin }, DebugPanel.FilterPickups(all, "BIG"), "by id or name, any case");
                CollectionAssert.AreEqual(new[] { heart }, DebugPanel.FilterPickups(all, "soul"), "by tag");
                CollectionAssert.IsEmpty(DebugPanel.FilterPickups(all, "key"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(coin);
                UnityEngine.Object.DestroyImmediate(heart);
            }
        }

        [Test]
        public void SetRedToHalf_KeepsSoulAndDarkHearts()
        {
            var state = new HealthState(3);
            state.AddSoul(3);
            state.AddDark(2);
            Assert.IsTrue(DebugPanel.SetRedToHalf(state, 100f));
            Assert.AreEqual(3, state.Red, "half of 6");
            Assert.AreEqual(3, state.Soul, "soul hearts kept");
            Assert.AreEqual(2, state.Dark, "dark hearts kept");

            var justHit = new HealthState(3);
            justHit.TryDamage(1, 100f);
            Assert.IsFalse(DebugPanel.SetRedToHalf(justHit, 100.5f), "still invincible from the last hit");
            Assert.AreEqual(5, justHit.Red, "unchanged");

            var low = new HealthState(4);
            low.TryDamage(7, 0f);
            Assert.IsTrue(DebugPanel.SetRedToHalf(low, 50f));
            Assert.AreEqual(4, low.Red, "healed up to half of 8");
        }
    }
}
