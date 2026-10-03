using UnityEngine;

namespace WhiteDragon
{
    [EnemyBehaviorInfo("Flying only: hold a height above the floor with a gentle bob while drifting around the player. Never finishes.", "Movement")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Hover", fileName = "Hover")]
    public class HoverBehavior : EnemyBehavior<HoverBehavior.State>
    {
        public class State
        {
            public float Angle;
            public float Direction = 1f;
            public float Time;
        }

        [Tooltip("Meters above the floor.")]
        [Min(0f)] public float height = 2.5f;
        [Tooltip("Bob up and down by this much (meters).")]
        [Min(0f)] public float bobAmplitude = 0.2f;
        [Tooltip("Bobs per second.")]
        [Min(0f)] public float bobFrequency = 0.8f;
        [Tooltip("Circle the player at this distance (meters).")]
        [Min(0f)] public float driftRadius = 5f;
        [Tooltip("Degrees per second around the player.")]
        public float driftSpeed = 30f;
        [Tooltip("Multiplies the enemy's move speed when catching up with its drift point.")]
        [Min(0f)] public float speedScale = 1f;
        [Tooltip("How quickly it returns to its height (per second).")]
        [Min(0f)] public float heightStiffness = 4f;

        protected override void Enter(EnemyContext ctx, State s)
        {
            // Start where it is around the player, circling either way (per-enemy randomness).
            Vector3 from = ctx.Position - ctx.TargetPosition;
            s.Angle = Mathf.Atan2(from.z, from.x) * Mathf.Rad2Deg;
            s.Direction = ctx.Random != null && ctx.Random.Chance(RandomStream.Combat, 0.5f) ? -1f : 1f;
            s.Time = 0f;
        }

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            s.Time += dt;
            s.Angle += driftSpeed * s.Direction * dt;
            float rad = s.Angle * Mathf.Deg2Rad;
            Vector3 center = ctx.HasTarget ? ctx.TargetPosition : ctx.Position;
            Vector3 drift = center + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * driftRadius;

            Vector3 horizontal = drift - ctx.Position;
            horizontal.y = 0f;
            float maxSpeed = ctx.Stats.MoveSpeed * speedScale;
            Vector3 velocity = Vector3.ClampMagnitude(horizontal * 2f, maxSpeed);

            float floor = FloorHeight(ctx);
            float wanted = floor + height + Mathf.Sin(s.Time * bobFrequency * 2f * Mathf.PI) * bobAmplitude;
            velocity.y = (wanted - ctx.Position.y) * heightStiffness;

            if (ctx.HasTarget) ctx.FaceTarget();
            ctx.Move(velocity);
            return false;
        }

        static float FloorHeight(EnemyContext ctx)
        {
            Vector3 origin = ctx.Center;
            return Physics.Raycast(origin, Vector3.down, out var hit, 50f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point.y
                : origin.y - 50f;
        }
    }
}
