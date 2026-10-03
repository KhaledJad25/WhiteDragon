using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Data brain: an ordered list of states; the first one is the start. Each frame every behavior of the
    /// active state runs (in list order), then its transitions are checked in order and the first that holds
    /// is taken (at most one per frame). BehaviorFinished = ANY behavior of the state has reported finished
    /// since the state was entered. TimeInState carries its leftover time into the next state, so loops keep
    /// the same rhythm at any frame rate.
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Brain (State Machine)", fileName = "Brain")]
    public class StateMachineBrain : EnemyBrainDefinition
    {
        const float TimeEpsilon = 1e-4f;

        public List<BrainState> states = new List<BrainState>();

        /// <summary>Per-enemy data of one enemy running this brain.</summary>
        class Runtime
        {
            public int current = -1;
            public float time;
            public bool anyFinished;
            public int damageAtEnter;
            public object[][] behaviorStates;
        }

        public override object CreateState(EnemyContext ctx)
        {
            var r = new Runtime { behaviorStates = new object[states.Count][] };
            for (int s = 0; s < states.Count; s++)
            {
                var list = states[s].behaviors;
                r.behaviorStates[s] = new object[list.Count];
                for (int b = 0; b < list.Count; b++)
                    if (list[b] != null) r.behaviorStates[s][b] = list[b].CreateState();
            }
            return r;
        }

        public override void Begin(EnemyContext ctx, object state)
        {
            if (states.Count > 0) Enter(ctx, (Runtime)state, 0, 0f);
        }

        public override void End(EnemyContext ctx, object state) => ExitCurrent(ctx, (Runtime)state);

        public override void Tick(EnemyContext ctx, object state, float dt)
        {
            var r = (Runtime)state;
            if (r.current < 0) return;
            var st = states[r.current];
            r.time += dt;
            ctx.DebugLabel = st.name;

            var behaviors = st.behaviors;
            var behaviorStates = r.behaviorStates[r.current];
            if (behaviorStates.Length != behaviors.Count) return; // asset edited during play; restart the enemy to apply

            for (int i = 0; i < behaviors.Count; i++)
                if (behaviors[i] != null && behaviors[i].Tick(ctx, behaviorStates[i], dt)) r.anyFinished = true;

            var transitions = st.transitions;
            for (int i = 0; i < transitions.Count; i++)
            {
                var t = transitions[i];
                if (!Holds(ctx, r, t)) continue;
                int target = IndexOf(t.target);
                if (target < 0) continue;
                float leftover = t.condition == TransitionCondition.TimeInState ? Mathf.Max(0f, r.time - t.value) : 0f;
                ExitCurrent(ctx, r);
                Enter(ctx, r, target, leftover);
                return;
            }
        }

        /// <summary>Name of the state this enemy is in (debug and tests).</summary>
        public string CurrentStateName(object state)
        {
            var r = state as Runtime;
            return r != null && r.current >= 0 ? states[r.current].name : "";
        }

        public int IndexOf(string stateName)
        {
            for (int i = 0; i < states.Count; i++)
                if (states[i].name == stateName) return i;
            return -1;
        }

        bool Holds(EnemyContext ctx, Runtime r, BrainTransition t)
        {
            switch (t.condition)
            {
                case TransitionCondition.TimeInState: return r.time >= t.value - TimeEpsilon;
                case TransitionCondition.DistanceToPlayerBelow: return ctx.HasTarget && ctx.DistanceToTarget < t.value;
                case TransitionCondition.DistanceToPlayerAbove: return ctx.HasTarget && ctx.DistanceToTarget > t.value;
                case TransitionCondition.BehaviorFinished: return r.anyFinished;
                case TransitionCondition.HealthBelowPercent: return ctx.HealthFraction * 100f < t.value;
                case TransitionCondition.TookDamage: return ctx.DamageTakenCount != r.damageAtEnter;
                case TransitionCondition.HasLineOfSight: return ctx.HasTarget && ctx.HasLineOfSight();
                case TransitionCondition.Always: return true;
                default: return false;
            }
        }

        void Enter(EnemyContext ctx, Runtime r, int index, float time)
        {
            r.current = index;
            r.time = time;
            r.anyFinished = false;
            r.damageAtEnter = ctx.DamageTakenCount;
            var st = states[index];
            ctx.DebugLabel = st.name;
            for (int i = 0; i < st.behaviors.Count; i++)
                if (st.behaviors[i] != null) st.behaviors[i].Enter(ctx, r.behaviorStates[index][i]);
        }

        void ExitCurrent(EnemyContext ctx, Runtime r)
        {
            if (r.current < 0) return;
            var st = states[r.current];
            for (int i = 0; i < st.behaviors.Count; i++)
                if (st.behaviors[i] != null) st.behaviors[i].Exit(ctx, r.behaviorStates[r.current][i]);
            r.current = -1;
        }
    }
}
