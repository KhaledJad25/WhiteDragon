using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// An enemy's AI. Two kinds: StateMachineBrain (data: states of behaviors plus transitions), or a code brain
    /// (derive from EnemyBrainDefinition&lt;TState&gt; and write the AI with the EnemyContext helpers; for complex
    /// enemies and bosses). Like behaviors, brain assets are shared and stateless: per-enemy data lives in the
    /// state object from CreateState.
    /// </summary>
    public abstract class EnemyBrainDefinition : ScriptableObject
    {
        /// <summary>A new per-enemy state object (once per enemy, when the brain starts on it).</summary>
        public abstract object CreateState(EnemyContext ctx);
        public abstract void Begin(EnemyContext ctx, object state);
        public abstract void Tick(EnemyContext ctx, object state, float dt);
        public abstract void End(EnemyContext ctx, object state);
    }

    /// <summary>Base for a code brain with a typed per-enemy state. Override Begin, Tick and End.</summary>
    public abstract class EnemyBrainDefinition<TState> : EnemyBrainDefinition where TState : class, new()
    {
        public sealed override object CreateState(EnemyContext ctx) => new TState();
        public sealed override void Begin(EnemyContext ctx, object state) => Begin(ctx, (TState)state);
        public sealed override void Tick(EnemyContext ctx, object state, float dt) => Tick(ctx, (TState)state, dt);
        public sealed override void End(EnemyContext ctx, object state) => End(ctx, (TState)state);

        protected virtual void Begin(EnemyContext ctx, TState state) { }
        protected abstract void Tick(EnemyContext ctx, TState state, float dt);
        protected virtual void End(EnemyContext ctx, TState state) { }
    }
}
