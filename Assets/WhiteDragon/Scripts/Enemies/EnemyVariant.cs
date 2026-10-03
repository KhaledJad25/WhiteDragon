using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// A variant of an enemy: multipliers and optional overrides applied on top of its EnemyDefinition
    /// (for example Armored Zombie or Fast Bat). Data only.
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Variant", fileName = "Variant")]
    public class EnemyVariant : ScriptableObject
    {
        [Tooltip("Unique lowercase id, e.g. \"armored_zombie\".")]
        public string id;
        [Tooltip("Added after the base enemy's name in tools, e.g. \"(Armored)\".")]
        public string displaySuffix;
        [Tooltip("The enemy this varies. Empty = may be applied to any enemy.")]
        public EnemyDefinition baseEnemy;

        [Header("Multipliers")]
        [Min(0.01f)] public float healthMultiplier = 1f;
        [Min(0f)] public float speedMultiplier = 1f;
        [Tooltip("Multiplies contact and attack damage (rounded to half hearts, at least 1).")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0.1f)] public float scaleMultiplier = 1f;

        [Header("Overrides (optional)")]
        public bool overrideTint;
        public Color tint = Color.grey;
        [Tooltip("Empty = the base enemy's model.")]
        public GameObject visualPrefab;
        [Tooltip("Empty = the base enemy's brain.")]
        public EnemyBrainDefinition brain;

        [Header("Rolling")]
        [Tooltip("Relative chance when variants are rolled later.")]
        [Min(0f)] public float weight = 1f;
    }
}
