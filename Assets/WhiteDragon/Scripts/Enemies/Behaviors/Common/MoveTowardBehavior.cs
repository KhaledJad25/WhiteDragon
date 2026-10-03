using UnityEngine;

namespace WhiteDragon
{
    [EnemyBehaviorInfo("Walk or fly at the player at the enemy's speed, facing it. Finishes within stop distance if asked.", "Movement")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Move Toward", fileName = "MoveToward")]
    public class MoveTowardBehavior : EnemyBehavior<MoveTowardBehavior.State>
    {
        public class State { }

        [Tooltip("Multiplies the enemy's move speed.")]
        [Min(0f)] public float speedScale = 1f;
        [Tooltip("Stop this close to the player (horizontal meters). 0 = keep pushing into it.")]
        [Min(0f)] public float stopDistance;
        [Tooltip("Report Finished once within stop distance.")]
        public bool finishWhenReached;
        [Tooltip("Steer around obstacles with a few raycasts (off = straight line, like the first enemies).")]
        public bool avoidObstacles = true;

        const float MinDistance = 0.05f;

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            if (!ctx.HasTarget) return false;
            float distance = ctx.DistanceToTarget;
            bool reached = distance <= Mathf.Max(stopDistance, MinDistance);
            if (reached)
            {
                ctx.Move(Vector3.zero);
                return finishWhenReached;
            }
            Vector3 dir = ctx.DirectionToTarget;
            if (avoidObstacles) dir = EnemySteering.Avoid(ctx, dir);
            ctx.Face(dir);
            Vector3 velocity = dir * (ctx.Stats.MoveSpeed * speedScale);
            if (ctx.Movement == MovementMode.Flying) velocity.y = (ctx.TargetCenter.y - ctx.Center.y) * 2f;
            ctx.Move(velocity);
            return false;
        }
    }
}
