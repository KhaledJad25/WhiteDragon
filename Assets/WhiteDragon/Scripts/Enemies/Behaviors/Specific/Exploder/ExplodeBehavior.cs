using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// The Exploder's blast: once, on the first frame of its state, it hurts the player if within the radius and
    /// not behind a wall (half hearts, through the normal team-checked damage path, so invincibility applies),
    /// plays a pooled particle burst and a shake, then the enemy dies. Needs a Telegraph state before it.
    /// </summary>
    [EnemyBehaviorInfo("Blow up once: area damage to the player within the radius (half hearts), burst and shake, then the enemy dies.", "Exploder")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Exploder/Explode", fileName = "Explode")]
    public class ExplodeBehavior : EnemyBehavior<ExplodeBehavior.State>
    {
        /// <summary>One per enemy, created once and reused: reset it in Enter.</summary>
        public class State
        {
            public bool Exploded;
        }

        [Tooltip("The player is hurt if the middle of its body is this close to the middle of the Exploder (meters).")]
        [Min(0f)] public float radius = 3f;
        [Tooltip("Half hearts (times the variant's damage multiplier). 2 = one full heart.")]
        [Min(0f)] public float damage = 2f;
        [Header("Feedback (pooled particles, no garbage)")]
        public Color burstColor = new Color(0.55f, 0.6f, 0.2f);
        [Min(0)] public int burstCount = 40;
        [Min(0f)] public float burstSpeed = 7f;
        [Range(0f, 1f)] public float shake = 0.5f;

        public override bool StartsAttack => true;

        protected override void Enter(EnemyContext ctx, State s)
        {
            s.Exploded = false;
        }

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            if (s.Exploded) return true;
            s.Exploded = true;
            Vector3 center = ctx.Center;
            // Walls shelter the player (other enemies do not): the blast needs a clear line to the player's body.
            if (ctx.HasTarget && (ctx.TargetCenter - center).sqrMagnitude <= radius * radius && ctx.HasLineOfSight())
                ctx.Damage(ctx.Target, damage * ctx.Stats.DamageMultiplier, center);
            GameFeel.Burst(center, burstColor, burstCount, burstSpeed);
            GameFeel.Shake(shake);
            ctx.Enemy.Kill();
            return true;
        }
    }
}
