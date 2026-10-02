using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Color asserts for colors read back from materials and property blocks. In Linear color space Unity
    /// stores them converted and converts them back on read, so they differ by float rounding only.
    /// </summary>
    public static class TestColors
    {
        public const float Tolerance = 0.0001f;

        /// <summary>Asserts each channel (r, g, b, a) matches within Tolerance.</summary>
        public static void AssertApprox(Color expected, Color actual, string message = null)
        {
            string m = (message == null ? "" : message + ": ") + $"expected {expected} but was {actual}";
            Assert.AreEqual(expected.r, actual.r, Tolerance, m + " (r)");
            Assert.AreEqual(expected.g, actual.g, Tolerance, m + " (g)");
            Assert.AreEqual(expected.b, actual.b, Tolerance, m + " (b)");
            Assert.AreEqual(expected.a, actual.a, Tolerance, m + " (a)");
        }
    }
}
