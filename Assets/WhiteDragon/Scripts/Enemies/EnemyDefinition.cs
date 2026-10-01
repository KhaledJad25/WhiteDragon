using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Enemy stats. Data only; the Enemy component reads it.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public float maxHealth = 20f;
        public float moveSpeed = 2.5f;
        [Tooltip("In half hearts.")]
        public int contactDamage = 1;
        public Color tint = Color.grey;
    }
}
