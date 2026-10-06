using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>One line of a drop table: a pickup, its weight, how many, and when it may drop.</summary>
    [Serializable]
    public class DropEntry
    {
        public PickupDefinition pickup;
        [Tooltip("Relative chance against the other entries and the nothing weight.")]
        [Min(0f)]
        public float weight = 1f;
        [Min(1)]
        public int minCount = 1;
        [Tooltip("How many drop: a whole number from minCount to maxCount.")]
        [Min(1)]
        public int maxCount = 1;
        [Tooltip("Always, or PlayerHurt (only while red health is below max).")]
        public DropCondition condition = DropCondition.Always;
    }
}
