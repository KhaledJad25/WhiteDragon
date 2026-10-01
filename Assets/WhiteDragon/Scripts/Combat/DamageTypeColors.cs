using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Placeholder tint per damage type.</summary>
    public static class DamageTypeColors
    {
        public static Color Tint(DamageType type)
        {
            switch (type)
            {
                case DamageType.Fire: return new Color(1f, 0.45f, 0.1f);
                case DamageType.Dark: return new Color(0.35f, 0.1f, 0.5f);
                case DamageType.Holy: return new Color(1f, 0.9f, 0.5f);
                case DamageType.Blood: return new Color(0.6f, 0.04f, 0.04f);
                default: return new Color(0.55f, 0.5f, 0.45f);
            }
        }
    }
}
