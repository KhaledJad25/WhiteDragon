using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// The one place that tints renderers. Uses a MaterialPropertyBlock (never edits materials), setting
    /// _BaseColor and _Color where the shader has them, and _EmissionColor during hit flashes.
    /// Layers, in order: base tint (placeholders), hit flash, status overlay. With nothing active the
    /// block is removed, so the materials render exactly as authored.
    /// </summary>
    public class RendererTint : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Renderer[] renderers;
        MaterialPropertyBlock block;
        Color? baseColor;
        Color flashColor;
        float flashTime, flashTimer;
        Color overlayColor;
        float overlayStrength;
        bool applied;

        /// <summary>The helper on go, added if missing.</summary>
        public static RendererTint For(GameObject go)
        {
            var t = go.GetComponent<RendererTint>();
            return t != null ? t : go.AddComponent<RendererTint>();
        }

        /// <summary>One-shot tint of a single renderer (no layers), e.g. a placeholder rock.</summary>
        public static void SetColor(Renderer r, Color color)
        {
            var b = new MaterialPropertyBlock();
            Write(r, b, color, 0f, color);
        }

        /// <summary>Which renderers to tint. Default: all child renderers at first use.</summary>
        public void SetRenderers(Renderer[] value)
        {
            RemoveBlocks();
            renderers = value;
            Refresh();
        }

        /// <summary>Placeholder tint. null = keep the material's own color.</summary>
        public void SetBaseColor(Color? color)
        {
            baseColor = color;
            Refresh();
        }

        public void Flash(Color color, float duration)
        {
            flashColor = color;
            flashTime = flashTimer = Mathf.Max(0f, duration);
            Refresh();
        }

        public void StopFlash()
        {
            flashTimer = 0f;
            Refresh();
        }

        /// <summary>Status tint blended over everything else. strength 0 = off.</summary>
        public void SetOverlay(Color color, float strength)
        {
            if (overlayColor == color && Mathf.Approximately(overlayStrength, strength)) return;
            overlayColor = color;
            overlayStrength = Mathf.Clamp01(strength);
            Refresh();
        }

        /// <summary>Removes every layer; materials render untouched again.</summary>
        public void ClearAll()
        {
            baseColor = null;
            flashTimer = 0f;
            overlayStrength = 0f;
            Refresh();
        }

        void LateUpdate() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (flashTimer <= 0f) return;
            flashTimer -= dt;
            Refresh();
        }

        public void Refresh()
        {
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            bool any = baseColor.HasValue || flashTimer > 0f || overlayStrength > 0f;
            if (!any)
            {
                if (applied) RemoveBlocks();
                return;
            }

            if (block == null) block = new MaterialPropertyBlock();
            float flash = flashTimer > 0f && flashTime > 0f ? Mathf.Clamp01(flashTimer / flashTime) : 0f;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                Color c = baseColor ?? OriginalColor(r);
                c = Color.Lerp(c, flashColor, flash);
                c = Color.Lerp(c, overlayColor, overlayStrength);
                Write(r, block, c, flash, flashColor);
            }
            applied = true;
        }

        void RemoveBlocks()
        {
            if (renderers != null)
                foreach (var r in renderers)
                    if (r != null) r.SetPropertyBlock(null);
            applied = false;
        }

        static void Write(Renderer r, MaterialPropertyBlock b, Color color, float flash, Color flashColor)
        {
            var m = r.sharedMaterial;
            b.Clear();
            if (m == null || m.HasProperty(BaseColorId)) b.SetColor(BaseColorId, color);
            if (m == null || m.HasProperty(ColorId)) b.SetColor(ColorId, color);
            if (flash > 0f && m != null && m.HasProperty(EmissionId)) b.SetColor(EmissionId, flashColor * flash);
            r.SetPropertyBlock(b);
        }

        static Color OriginalColor(Renderer r)
        {
            var m = r.sharedMaterial;
            if (m == null) return Color.white;
            if (m.HasProperty(BaseColorId)) return m.GetColor(BaseColorId);
            if (m.HasProperty(ColorId)) return m.GetColor(ColorId);
            return Color.white;
        }
    }
}
