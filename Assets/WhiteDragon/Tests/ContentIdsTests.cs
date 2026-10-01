using NUnit.Framework;

namespace WhiteDragon
{
    public class ContentIdsTests
    {
        [TestCase("Iron Tooth", "iron_tooth")]
        [TestCase("IronTooth", "iron_tooth")]
        [TestCase("  Giant's   Gut! ", "giants_gut")]
        [TestCase("Bone-Needle 2", "bone_needle_2")]
        [TestCase("HP2Up", "hp2_up")]
        [TestCase("already_snake", "already_snake")]
        [TestCase("!!!", "")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void ToSnakeCase(string input, string expected)
        {
            Assert.AreEqual(expected, ContentIds.ToSnakeCase(input));
        }

        [Test]
        public void MakeUnique_AddsNumberWhenTaken_IgnoringCase()
        {
            Assert.AreEqual("flint", ContentIds.MakeUnique("flint", new[] { "boulder" }));
            Assert.AreEqual("flint_2", ContentIds.MakeUnique("flint", new[] { "FLINT" }));
            Assert.AreEqual("flint_4", ContentIds.MakeUnique("flint", new[] { "flint", "flint_2", "flint_3" }));
            Assert.AreEqual("flint", ContentIds.MakeUnique("flint", null));
        }

        [Test]
        public void ToFileName_StripsInvalidCharacters()
        {
            Assert.AreEqual("Giant's Gut", ContentIds.ToFileName("  Giant's Gut "));
            Assert.AreEqual("ab", ContentIds.ToFileName("a/b"));
            Assert.AreEqual("New Asset", ContentIds.ToFileName("   "));
            Assert.AreEqual("X", ContentIds.ToFileName("", "X"));
        }
    }
}
