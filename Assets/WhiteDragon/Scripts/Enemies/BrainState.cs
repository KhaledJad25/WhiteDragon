using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>One StateMachineBrain state: behaviors that run together, and the ways out.</summary>
    [Serializable]
    public class BrainState
    {
        public string name = "State";
        [Tooltip("All of these run every frame, in this order, while the state is active.")]
        public List<EnemyBehavior> behaviors = new List<EnemyBehavior>();
        [Tooltip("Checked in order after the behaviors run; the first one that holds is taken.")]
        public List<BrainTransition> transitions = new List<BrainTransition>();
    }
}
