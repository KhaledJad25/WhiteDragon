using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// A building block of enemy AI. Assets are SHARED by every enemy that uses them and must stay STATELESS:
    /// fields are Inspector parameters only; anything that changes while an enemy runs lives in its per-enemy
    /// state object (created once per enemy, never per frame). Write new ones by deriving from
    /// EnemyBehavior&lt;TState&gt; and tagging the class with [EnemyBehaviorInfo].
    /// </summary>
    public abstract class EnemyBehavior : ScriptableObject
    {
        /// <summary>A new per-enemy state object (called once per enemy, when its brain starts).</summary>
        public abstract object CreateState();
        public abstract void Enter(EnemyContext ctx, object state);
        /// <summary>One frame; returns true when finished (the BehaviorFinished transition reads this).</summary>
        public abstract bool Tick(EnemyContext ctx, object state, float dt);
        public abstract void Exit(EnemyContext ctx, object state);
    }

    /// <summary>
    /// Base for a behavior with a typed per-enemy state. Override Enter, Tick (return true when done) and Exit.
    /// TState is a small class of the values one enemy needs (timers, locked target, ...); it is reused, so
    /// reset it in Enter.
    /// </summary>
    public abstract class EnemyBehavior<TState> : EnemyBehavior where TState : class, new()
    {
        public sealed override object CreateState() => new TState();
        public sealed override void Enter(EnemyContext ctx, object state) => Enter(ctx, (TState)state);
        public sealed override bool Tick(EnemyContext ctx, object state, float dt) => Tick(ctx, (TState)state, dt);
        public sealed override void Exit(EnemyContext ctx, object state) => Exit(ctx, (TState)state);

        protected virtual void Enter(EnemyContext ctx, TState state) { }
        protected abstract bool Tick(EnemyContext ctx, TState state, float dt);
        protected virtual void Exit(EnemyContext ctx, TState state) { }
    }
}
