using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Passive touch damage (the first enemies' contact hit), not a swung attack, so it needs no telegraph.
    /// The first touch hits at once; then at most one hit per cooldown (leftover time carries over).
    /// </summary>
    [EnemyBehaviorInfo("Hurt the player on touch: contact damage within reach, at most once per cooldown. Never finishes.", "Attack")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Melee Contact", fileName = "MeleeContact")]
    public class MeleeContactBehavior : EnemyBehavior<MeleeContactBehavior.State>
    {
        public class State
        {
            public float Cooldown;
        }

        [Tooltip("Seconds between hits.")]
        [Min(0f)] public float cooldown = 1f;
        [Tooltip("Extra reach beyond both bodies' radii (meters).")]
        [Min(0f)] public float extraReach = 0.25f;
        [Tooltip("Multiplies the enemy's contact damage (half hearts).")]
        [Min(0f)] public float damageScale = 1f;

        protected override void Enter(EnemyContext ctx, State s) => s.Cooldown = 0f;

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            // Counts down to (at most one frame below) zero, so the overshoot carries into the next cooldown.
            if (s.Cooldown > 0f) s.Cooldown -= dt;
            if (!ctx.HasTarget || s.Cooldown > 1e-4f || !ctx.InContactRange(extraReach)) return false;
            s.Cooldown += cooldown;
            ctx.Events.Raise(ActorState.Attack);
            ctx.Damage(ctx.Target, ctx.Stats.ContactDamage * damageScale, ctx.Position);
            return false;
        }
    }
}
