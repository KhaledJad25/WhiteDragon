using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Locks the player's position when its state starts, then charges there in a straight line at speed for the
    /// duration (exact distance at any frame rate), hurting the player once on contact. Stops early at an obstacle.
    /// Precede it with a Telegraph state.
    /// </summary>
    [EnemyBehaviorInfo("Charge in a straight line at the player's locked position; contact damage once; stops at obstacles. Finishes after the duration.", "Attack")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Dash", fileName = "Dash")]
    public class DashBehavior : EnemyBehavior<DashBehavior.State>
    {
        public class State
        {
            public Vector3 Direction;
            public float Time;
            public bool Hit;
        }

        [Min(0f)] public float speed = 14f;
        [Min(0f)] public float duration = 0.4f;
        [Tooltip("Half hearts on contact (times the variant's damage multiplier).")]
        [Min(0f)] public float damage = 1f;
        [Tooltip("Extra reach beyond both bodies' radii (meters).")]
        [Min(0f)] public float extraReach = 0.3f;

        protected override void Enter(EnemyContext ctx, State s)
        {
            s.Time = 0f;
            s.Hit = false;
            Vector3 to = ctx.HasTarget ? ctx.TargetCenter - ctx.Center : ctx.Transform.forward;
            if (ctx.Movement == MovementMode.Ground) to.y = 0f;
            s.Direction = to.sqrMagnitude > 1e-6f ? to.normalized : ctx.Transform.forward;
            ctx.Face(s.Direction);
            ctx.Events.Raise(ActorState.Attack);
        }

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            // Blocked last frame: stop the dash.
            if (s.Time > 0f && (ctx.LastMoveFlags & CollisionFlags.Sides) != 0) return true;
            if (!s.Hit && ctx.HasTarget && ctx.InContactRange(extraReach))
            {
                s.Hit = true;
                ctx.Damage(ctx.Target, damage * ctx.Stats.DamageMultiplier, ctx.Position);
            }
            if (dt <= 0f) return s.Time >= duration - 1e-5f;
            float step = Mathf.Min(dt, duration - s.Time);
            s.Time += step;
            if (step > 0f) ctx.Move(s.Direction * (speed * step / dt));
            return s.Time >= duration - 1e-5f;
        }
    }
}
