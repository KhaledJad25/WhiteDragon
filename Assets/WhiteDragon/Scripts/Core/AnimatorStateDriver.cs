using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Optional: put on a model prefab that has an Animator. Maps actor states (from the nearest
    /// ActorStateEvents above it) to Animator parameters. Without this component nothing is animated.
    /// Tip: drive Idle/Move with a Bool and Attack/Hit/Die with Triggers so one-shots are not cut short.
    /// </summary>
    public class AnimatorStateDriver : MonoBehaviour
    {
        public enum ParameterKind
        {
            Trigger,
            Bool,
            Int,
            Float,
            PlayState,
        }

        [Serializable]
        public struct Mapping
        {
            [Tooltip("When this state is raised...")]
            public ActorState state;
            [Tooltip("...do this: set a Trigger, Bool (Value 0 = false, else true), Int or Float parameter, or cross-fade straight to a state by name (PlayState).")]
            public ParameterKind kind;
            [Tooltip("Animator parameter name (or state name for PlayState).")]
            public string parameter;
            [Tooltip("Value for Bool, Int and Float.")]
            public float value;
        }

        [Tooltip("Empty = the first Animator on this object or its children.")]
        public Animator animator;
        [Tooltip("Cross-fade time in seconds, used by PlayState mappings.")]
        [Min(0f)]
        public float crossFade = 0.1f;
        public List<Mapping> mappings = new List<Mapping>();

        ActorStateEvents source;

        void OnEnable()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            source = GetComponentInParent<ActorStateEvents>();
            if (source == null) return;
            source.StateRaised += Apply;
            Apply(source.Current);
        }

        void OnDisable()
        {
            if (source != null) source.StateRaised -= Apply;
        }

        public void Apply(ActorState state)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var m in mappings)
            {
                if (m.state != state || string.IsNullOrEmpty(m.parameter)) continue;
                switch (m.kind)
                {
                    case ParameterKind.Trigger: animator.SetTrigger(m.parameter); break;
                    case ParameterKind.Bool: animator.SetBool(m.parameter, m.value != 0f); break;
                    case ParameterKind.Int: animator.SetInteger(m.parameter, Mathf.RoundToInt(m.value)); break;
                    case ParameterKind.Float: animator.SetFloat(m.parameter, m.value); break;
                    case ParameterKind.PlayState: animator.CrossFadeInFixedTime(m.parameter, crossFade); break;
                }
            }
        }
    }
}
