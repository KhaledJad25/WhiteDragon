using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// TextMesh colors are vertex colors, which Unity does not convert in Linear color space, so text would
    /// render paler than authored. Every TextMesh color goes through Set so it shows the authored color.
    /// </summary>
    public static class TextMeshColor
    {
        /// <summary>The authored (sRGB) color as the TextMesh needs it in the active color space. Alpha unchanged.</summary>
        public static Color Display(Color authored) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? authored.linear : authored;

        /// <summary>Sets text to show the authored color.</summary>
        public static void Set(TextMesh text, Color authored) => text.color = Display(authored);
    }
}
