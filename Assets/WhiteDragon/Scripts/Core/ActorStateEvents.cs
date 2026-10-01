using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Raises simple actor states (Idle, Move, Attack, Hit, Die) for animation and audio hooks.
    /// No animation system here: listeners such as AnimatorStateDriver decide what to do.
    /// Idle and Move fire only when they change; Attack and Hit fire every time; Die fires once and sticks.
    /// </summary>
    public class ActorStateEvents : MonoBehaviour
    {
        public const float MoveThreshold = 0.1f;

        public ActorState Current { get; private set; } = ActorState.Idle;
        public event Action<ActorState> StateRaised;

        /// <summary>The events component on go, added if missing.</summary>
        public static ActorStateEvents For(GameObject go)
        {
            var e = go.GetComponent<ActorStateEvents>();
            return e != null ? e : go.AddComponent<ActorStateEvents>();
        }

        /// <summary>Move when the horizontal speed is above the threshold, otherwise Idle.</summary>
        public static ActorState Locomotion(float horizontalSpeed) =>
            horizontalSpeed > MoveThreshold ? ActorState.Move : ActorState.Idle;

        public void Raise(ActorState state)
        {
            if (Current == ActorState.Die) return;
            if ((state == ActorState.Idle || state == ActorState.Move) && state == Current) return;
            Current = state;
            StateRaised?.Invoke(state);
        }
    }
}
