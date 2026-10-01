using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>One change to one stat. Serializable so items and synergies can list them.</summary>
    [Serializable]
    public struct StatModifier
    {
        [Tooltip("Which stat to change.")]
        public StatType stat;
        [Tooltip("Flat adds the value. PercentAdd adds a percent (0.25 = +25%). Multiply multiplies (1.5 = x1.5).")]
        public ModifierKind kind;
        [Tooltip("Amount. Negative values lower the stat.")]
        public float value;

        public StatModifier(StatType stat, ModifierKind kind, float value)
        {
            this.stat = stat;
            this.kind = kind;
            this.value = value;
        }
    }
}
