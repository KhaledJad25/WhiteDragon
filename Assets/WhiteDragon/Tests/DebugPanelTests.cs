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
    }
}
