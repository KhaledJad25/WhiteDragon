using System;

namespace WhiteDragon
{
    /// <summary>One change to one stat. Serializable so items and synergies can list them.</summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public ModifierKind kind;
        public float value;

        public StatModifier(StatType stat, ModifierKind kind, float value)
        {
            this.stat = stat;
            this.kind = kind;
            this.value = value;
        }
    }
}
