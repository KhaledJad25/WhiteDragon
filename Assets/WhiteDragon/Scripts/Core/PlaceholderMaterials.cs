using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Cached flat-color placeholder materials created in code (URP). Shaders come from the
    /// RenderingDefaults asset in Resources, so player builds include them.
    /// </summary>
    public static class PlaceholderMaterials
    {
        static readonly Dictionary<Color, Material> lit = new Dictionary<Color, Material>();
        static readonly Dictionary<(Color, float), Material> emissive = new Dictionary<(Color, float), Material>();
        static Material particle;

        /// <summary>URP Lit material of this color. One shared material per color (SRP Batcher friendly).</summary>
        public static Material Lit(Color color)
        {
            // m == null also catches a destroyed material (made in Play mode, which destroys it on exit).
            if (!lit.TryGetValue(color, out var m) || m == null)
            {
                // Instancing stays on as before; with no property block the SRP Batcher handles these.
                m = new Material(Shaders().lit) { color = color, enableInstancing = true };
                lit[color] = m;
            }
            return m;
        }

        /// <summary>URP Lit material of this color that also glows in it (emission = color * glow). Shared per color and glow.</summary>
        public static Material Emissive(Color color, float glow)
        {
            var key = (color, glow);
            if (!emissive.TryGetValue(key, out var m) || m == null)
            {
                var template = RenderingDefaults.Current != null ? RenderingDefaults.Current.emissive : null;
                if (template == null)
                {
                    Debug.LogError("[Rendering] RenderingDefaults.emissive is not set; using a plain lit material.");
                    return Lit(color);
                }
                m = new Material(template) { color = color, enableInstancing = true };
                m.SetColor("_EmissionColor", color * glow);
                emissive[key] = m;
            }
            return m;
        }

        /// <summary>URP Particles/Unlit material; particle colors come from vertex colors.</summary>
        public static Material Particle()
        {
            if (particle == null) particle = new Material(Shaders().particles);
            return particle;
        }

        static RenderingDefaults Shaders()
        {
            var d = RenderingDefaults.Current;
            if (d == null || d.lit == null || d.particles == null)
                Debug.LogError("[Rendering] Data/Resources/RenderingDefaults.asset is missing or incomplete; placeholder materials cannot be made.");
            return d;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            lit.Clear();
            emissive.Clear();
            particle = null;
        }
    }
}
