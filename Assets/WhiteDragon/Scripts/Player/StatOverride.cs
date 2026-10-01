using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Replaces one stat's base value (used by characters). Items still modify on top of it.</summary>
    [Serializable]
    public struct StatOverride
    {
        [Tooltip("Which stat's base value to replace.")]
        public StatType stat;
        [Tooltip("New base value. Defaults: MoveSpeed 5, JumpHeight 1.2, CharacterSize 1, FireRate 2, Damage 3.5, ProjectileSpeed 18, Range 20, Luck 0.")]
        public float value;

        public StatOverride(StatType stat, float value)
        {
            this.stat = stat;
            this.value = value;
        }
    }
}
