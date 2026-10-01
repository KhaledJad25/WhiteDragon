using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Enemy stats. Data only; the Enemy component reads it.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique lowercase id, e.g. \"ghoul\".")]
        public string id;
        [Tooltip("Name for tools and future UI.")]
        public string displayName;

        [Header("Stats")]
        [Tooltip("Damage needed to kill it. A starting rock does 3.5.")]
        [Min(1f)]
        public float maxHealth = 20f;
        [Tooltip("Meters per second while chasing. The player walks at 5.")]
        [Min(0f)]
        public float moveSpeed = 2.5f;
        [Tooltip("Damage on touch, in half hearts. 2 = one full heart.")]
        [Min(0)]
        public int contactDamage = 1;

        [Header("Look")]
        [Tooltip("Placeholder body color.")]
        public Color tint = Color.grey;

        [Header("Art (optional)")]
        [Tooltip("Model spawned on the enemy, replacing the placeholder shapes. Pivot at the feet, facing +Z. Empty = placeholder shapes tinted with Tint.")]
        public GameObject visualPrefab;
        [Tooltip("Seconds the body stays after death so a death animation can play. It stops moving and stops blocking rocks at once. 0 = removed immediately.")]
        [Min(0f)]
        public float deathDelay;
    }
}
