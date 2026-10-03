using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Minimal code brain: the AI written directly in C# with the EnemyContext helpers, for enemies (and later
    /// bosses) where a state list is too rigid. This one walks at the player, then stops to rest, and repeats.
    /// A real one can also run behavior assets itself (CreateState once, then Enter / Tick / Exit).
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Brain (Code Example)", fileName = "ExampleCodeBrain")]
    public class ExampleCodeBrain : EnemyBrainDefinition<ExampleCodeBrain.State>
    {
        public class State
        {
            public bool Resting;
            public float Time;
        }

        [Min(0f)] public float chaseSeconds = 3f;
        [Min(0f)] public float restSeconds = 1f;

        protected override void Begin(EnemyContext ctx, State s)
        {
            s.Resting = false;
            s.Time = 0f;
        }

        protected override void Tick(EnemyContext ctx, State s, float dt)
        {
            s.Time += dt;
            float phase = s.Resting ? restSeconds : chaseSeconds;
            if (s.Time >= phase - 1e-4f)
            {
                s.Time -= phase;
                s.Resting = !s.Resting;
            }
            ctx.DebugLabel = s.Resting ? "Rest" : "Chase";
            if (s.Resting || !ctx.HasTarget) return;
            ctx.FaceTarget();
            ctx.MoveToward(ctx.TargetPosition, ctx.Stats.MoveSpeed, avoidObstacles: true);
        }
    }
}
