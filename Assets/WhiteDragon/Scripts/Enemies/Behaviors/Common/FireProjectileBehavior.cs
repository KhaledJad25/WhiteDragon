using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Fires once when its state is entered (precede it with a Telegraph state), then reports Finished.</summary>
    [EnemyBehaviorInfo("Fire pooled projectiles at the player (count, even spread, speed, damage in half hearts). Finishes after firing.", "Attack")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Fire Projectile", fileName = "FireProjectile")]
    public class FireProjectileBehavior : EnemyBehavior<FireProjectileBehavior.State>
    {
        public class State
        {
            public bool Fired;
            public ShotRecipe Recipe;
        }

        [Min(1)] public int count = 1;
        [Tooltip("Total fan angle for several shots (degrees), spread evenly.")]
        [Min(0f)] public float spreadDegrees = 20f;
        [Min(0.1f)] public float speed = 12f;
        [Tooltip("Half hearts per hit (times the variant's damage multiplier).")]
        [Min(0f)] public float damage = 1f;
        [Min(0.1f)] public float range = 30f;
        public DamageType damageType = DamageType.Dark;
        [Min(0.1f)] public float sizeScale = 1.5f;
        [Tooltip("0 = flies straight; 1 = drops like a rock.")]
        [Min(0f)] public float gravityScale;
        [Tooltip("Spawn point relative to the enemy (local space; +Z is forward).")]
        public Vector3 muzzleOffset = new Vector3(0f, 1.4f, 0.6f);

        protected override void Enter(EnemyContext ctx, State s)
        {
            s.Fired = false;
            s.Recipe ??= new ShotRecipe();
            s.Recipe.Damage = damage * ctx.Stats.DamageMultiplier;
            s.Recipe.Speed = speed;
            s.Recipe.Range = range;
            s.Recipe.DamageType = damageType;
            s.Recipe.SizeScale = sizeScale;
        }

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            if (s.Fired) return true;
            s.Fired = true;
            Vector3 origin = ctx.Transform.TransformPoint(muzzleOffset);
            Vector3 aim = ctx.HasTarget ? ctx.TargetCenter - origin : ctx.Transform.forward;
            if (aim.sqrMagnitude < 1e-6f) aim = ctx.Transform.forward;
            aim.Normalize();
            for (int i = 0; i < count; i++)
            {
                float yaw = count > 1 ? Mathf.Lerp(-spreadDegrees * 0.5f, spreadDegrees * 0.5f, i / (float)(count - 1)) : 0f;
                var p = ctx.FireProjectile(s.Recipe, origin, Quaternion.AngleAxis(yaw, Vector3.up) * aim);
                p.GravityScale = gravityScale;
            }
            ctx.Events.Raise(ActorState.Attack);
            return true;
        }
    }
}
