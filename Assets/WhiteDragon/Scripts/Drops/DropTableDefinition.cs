using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// What a source (enemy, variant, room) can drop. Create an asset in Data/Resources/DropTables; no code needed.
    /// Each roll picks one entry or nothing by weight; player modifiers scale the weights.
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Drop Table", fileName = "DropTable")]
    public class DropTableDefinition : ScriptableObject
    {
        [Tooltip("Unique lowercase id, e.g. \"enemy_common\".")]
        public string id;
        public List<DropEntry> entries = new List<DropEntry>();
        [Tooltip("Weight of dropping nothing on a roll. Luck lowers it (to at most 80% less).")]
        [Min(0f)]
        public float nothingWeight;
        [Tooltip("Separate picks per drop (each can be nothing).")]
        [Min(1)]
        public int rolls = 1;
    }
}
