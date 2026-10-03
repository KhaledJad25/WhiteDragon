using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>A StateMachineBrain transition: when the condition holds, go to the target state.</summary>
    [Serializable]
    public class BrainTransition
    {
        public TransitionCondition condition;
        [Tooltip("Seconds, meters or percent, depending on the condition. Ignored by BehaviorFinished, TookDamage, HasLineOfSight and Always.")]
        public float value;
        [Tooltip("Name of the state to go to.")]
        public string target = "";
    }
}
