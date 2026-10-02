using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Applies the ArtPalette's fog, ambient and sun light to the scene, in the editor and at runtime.
    /// Grading (vignette, color adjustments, tone mapping) is the Volume profile on this same object.
    /// </summary>
    [ExecuteAlways]
    public class LookRig : MonoBehaviour
    {
        [Tooltip("Empty uses Data/Resources/ArtPalette.")]
        public ArtPalette palette;
        [Tooltip("The main light. Empty uses RenderSettings.sun.")]
        public Light sun;

        public ArtPalette Palette => palette != null ? palette : ArtPalette.Current;

        void OnEnable() => Apply();
        void OnValidate() => Apply();

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
