using UnityEngine;

namespace WhiteDragon
{
    /// <summary>An enemy's effective numbers: its EnemyDefinition with the EnemyVariant (if any) applied.</summary>
    public class EnemyStats
    {
        public float MaxHealth = 10f;
        public float MoveSpeed = 2f;
        /// <summary>Contact damage in half hearts.</summary>
        public int ContactDamage = 1;
        /// <summary>Multiplies every attack's damage (contact damage already includes it).</summary>
        public float DamageMultiplier = 1f;
        public float Scale = 1f;
        public Color Tint = Color.grey;

        public static EnemyStats From(EnemyDefinition definition, EnemyVariant variant)
        {
            var s = new EnemyStats();
            if (definition != null)
            {
                s.MaxHealth = definition.maxHealth;
                s.MoveSpeed = definition.moveSpeed;
                s.ContactDamage = definition.contactDamage;
                s.Tint = definition.tint;
            }
            if (variant != null)
            {
                s.MaxHealth *= variant.healthMultiplier;
                s.MoveSpeed *= variant.speedMultiplier;
                s.DamageMultiplier = variant.damageMultiplier;
                s.ContactDamage = s.ContactDamage > 0 ? Mathf.Max(1, Mathf.RoundToInt(s.ContactDamage * variant.damageMultiplier)) : 0;
                s.Scale = variant.scaleMultiplier;
                if (variant.overrideTint) s.Tint = variant.tint;
            }
            return s;
        }
    }
}
