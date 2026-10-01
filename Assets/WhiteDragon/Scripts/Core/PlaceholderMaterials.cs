using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Cached flat-color placeholder materials created in code.</summary>
    public static class PlaceholderMaterials
    {
        static readonly Dictionary<Color, Material> lit = new Dictionary<Color, Material>();
        static Material particle;

        public static Material Lit(Color color)
        {
            if (!lit.TryGetValue(color, out var m))
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Standard")
                             ?? Shader.Find("Sprites/Default");
                m = new Material(shader) { color = color };
                lit[color] = m;
            }
            return m;
        }

        /// <summary>Unlit vertex-colored material for particles.</summary>
        public static Material Particle()
        {
            if (particle == null) particle = new Material(Shader.Find("Sprites/Default"));
            return particle;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            lit.Clear();
            particle = null;
        }
    }
}
