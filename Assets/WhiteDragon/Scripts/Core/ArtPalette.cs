using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// The global look in one asset (Data/Resources/ArtPalette): named colors, fog, ambient and sun light
    /// (applied by LookRig), and the environment materials' colors (applied in the editor when this asset
    /// changes). Gameplay readability colors (rarity, damage types, statuses, hit flash) are NOT here.
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Art Palette", fileName = "ArtPalette")]
    public class ArtPalette : ScriptableObject
    {
        [Serializable]
        public class MaterialTint
        {
            public Material material;
            public PaletteColor color;
            [Tooltip("Multiplies the palette color (1 = as is).")]
            [Range(0f, 2f)] public float brightness = 1f;
        }

        [Header("Palette (dark, desaturated; blood red is the only strong accent)")]
        public Color nearBlack = new Color(0.045f, 0.043f, 0.047f);
        public Color ashGrey = new Color(0.36f, 0.35f, 0.34f);
        public Color bone = new Color(0.62f, 0.58f, 0.5f);
        public Color bloodRed = new Color(0.55f, 0.03f, 0.03f);
        public Color emberOrange = new Color(0.7f, 0.32f, 0.1f);
        public Color sicklyGreen = new Color(0.38f, 0.45f, 0.25f);
        public Color bruisePurple = new Color(0.3f, 0.18f, 0.33f);
        public Color coldBlue = new Color(0.25f, 0.32f, 0.42f);

        [Header("Fog (color is Near Black; also the camera background)")]
        [Tooltip("Exponential squared fog density. 0.036 hides everything past about 60 m.")]
        [Min(0f)] public float fogDensity = 0.036f;

        [Header("Light")]
        [Tooltip("Flat ambient light: dark, slightly cold.")]
        public Color ambient = new Color(0.055f, 0.06f, 0.075f);
        [Tooltip("Main (sun) light color: warm.")]
        public Color sunColor = new Color(1f, 0.84f, 0.66f);
        [Min(0f)] public float sunIntensity = 0.8f;
        [Tooltip("Main light casts soft shadows (off = hard).")]
        public bool softShadows = true;

        [Header("Environment materials (low smoothness, no specular)")]
        [Range(0f, 1f)] public float environmentSmoothness = 0.1f;
        public List<MaterialTint> environment = new List<MaterialTint>();

        static ArtPalette current;
        static bool loaded;

        /// <summary>The asset in Resources (loaded once).</summary>
        public static ArtPalette Current
        {
            get
            {
                if (!loaded)
                {
                    current = Resources.Load<ArtPalette>("ArtPalette");
                    loaded = true;
                }
                return current;
            }
        }

        public Color ColorOf(PaletteColor c)
        {
            switch (c)
            {
                case PaletteColor.NearBlack: return nearBlack;
                case PaletteColor.AshGrey: return ashGrey;
                case PaletteColor.Bone: return bone;
                case PaletteColor.BloodRed: return bloodRed;
                case PaletteColor.EmberOrange: return emberOrange;
                case PaletteColor.SicklyGreen: return sicklyGreen;
                case PaletteColor.BruisePurple: return bruisePurple;
                case PaletteColor.ColdBlue: return coldBlue;
                default: return Color.magenta;
            }
        }

        /// <summary>The color an environment entry gets: palette color times brightness, alpha 1.</summary>
        public Color TintOf(MaterialTint t)
        {
            var c = ColorOf(t.color) * t.brightness;
            c.a = 1f;
            return c;
        }

        /// <summary>Writes palette colors, low smoothness and no specular into the environment materials (editor authoring).</summary>
        public void ApplyToMaterials()
        {
            foreach (var t in environment)
            {
                if (t == null || t.material == null) continue;
                var m = t.material;
                m.color = TintOf(t);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", environmentSmoothness);
                if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 0f);
                m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                if (m.HasProperty("_EnvironmentReflections")) m.SetFloat("_EnvironmentReflections", 0f);
                m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            loaded = false;
        }
    }
}
