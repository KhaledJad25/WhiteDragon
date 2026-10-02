using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace WhiteDragon
{
    /// <summary>
    /// Applies the ArtPalette's fog, ambient and sun light to the scene, in the editor and at runtime.
    /// Grading (vignette, color adjustments, tone mapping) is the Volume profile on this same object.
    /// In Play mode it also turns the outline renderer feature off at Low effects quality.
    /// </summary>
    [ExecuteAlways]
    public class LookRig : MonoBehaviour
    {
        [Tooltip("Empty uses Data/Resources/ArtPalette.")]
        public ArtPalette palette;
        [Tooltip("The main light. Empty uses RenderSettings.sun.")]
        public Light sun;
        [Tooltip("The Outline renderer feature on WhiteDragon_URP_Renderer. Off at Low effects quality.")]
        public ScriptableRendererFeature outline;

        bool outlineApplied;
        EffectsQuality appliedQuality;

        public ArtPalette Palette => palette != null ? palette : ArtPalette.Current;

        void OnEnable() => Apply();
        void OnValidate() => Apply();

        void Update()
        {
            if (!Application.isPlaying) return;
            if (outlineApplied && appliedQuality == GameFeel.Quality) return;
            ApplyQuality(GameFeel.Quality);
        }

        void OnDisable()
        {
            // The feature is an asset: leave it on so Play mode never changes it on disk.
            if (outlineApplied && outline != null) outline.SetActive(true);
            outlineApplied = false;
        }

        /// <summary>Outline on unless quality is Low.</summary>
        public void ApplyQuality(EffectsQuality quality)
        {
            if (outline != null) outline.SetActive(quality != EffectsQuality.Low);
            appliedQuality = quality;
            outlineApplied = true;
        }

        public void Apply()
        {
            var p = Palette;
            if (p == null) return;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = p.fogDensity;
            RenderSettings.fogColor = p.nearBlack;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = p.ambient;

            var light = sun != null ? sun : RenderSettings.sun;
            if (light != null)
            {
                light.color = p.sunColor;
                light.intensity = p.sunIntensity;
                light.shadows = p.softShadows ? LightShadows.Soft : LightShadows.Hard;
            }

            // Whatever fog fully hides should match what is behind it.
            var cam = Camera.main;
            if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor) cam.backgroundColor = p.nearBlack;
        }
    }
}
