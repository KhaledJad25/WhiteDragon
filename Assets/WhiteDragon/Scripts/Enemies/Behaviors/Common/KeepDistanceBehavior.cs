using UnityEngine;

namespace WhiteDragon
{
    [EnemyBehaviorInfo("Hold a distance band from the player: back off when too close, approach when too far, optionally strafe. Never finishes.", "Movement")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Keep Distance", fileName = "KeepDistance")]
    public class KeepDistanceBehavior : EnemyBehavior<KeepDistanceBehavior.State>
    {
        public class State
        {
            public float StrafeSign = 1f;
            public float FlipTimer;
        }

        [Tooltip("Back away when closer than this (meters).")]
        [Min(0f)] public float minDistance = 8f;
        [Tooltip("Approach when farther than this (meters).")]
        [Min(0f)] public float maxDistance = 12f;
        [Tooltip("Multiplies the enemy's move speed.")]
        [Min(0f)] public float speedScale = 1f;
        [Tooltip("Sidestep around the player while inside the band.")]
        public bool strafe = true;
        [Tooltip("Multiplies speed while strafing.")]
        [Min(0f)] public float strafeSpeedScale = 0.6f;
        [Tooltip("Seconds between strafe direction changes.")]
        [Min(0.1f)] public float strafeFlipInterval = 2f;
        public bool avoidObstacles = true;

        protected override void Enter(EnemyContext ctx, State s)
        {
            s.StrafeSign = ctx.Random != null && ctx.Random.Chance(RandomStream.Combat, 0.5f) ? -1f : 1f;
            // Random first flip so a group does not change direction in step.
            s.FlipTimer = ctx.Random != null ? ctx.Random.Range(RandomStream.Combat, 0f, strafeFlipInterval) : 0f;
        }

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            if (!ctx.HasTarget) return false;
            ctx.FaceTarget();
            float distance = ctx.DistanceToTarget;
            Vector3 toPlayer = ctx.DirectionToTarget;
            float speed = ctx.Stats.MoveSpeed * speedScale;
            Vector3 dir;
            if (distance < minDistance) dir = -toPlayer;
            else if (distance > maxDistance) dir = toPlayer;
            else if (strafe)
            {
                s.FlipTimer += dt;
                while (s.FlipTimer >= strafeFlipInterval)
                {
                    s.FlipTimer -= strafeFlipInterval;
                    s.StrafeSign = -s.StrafeSign;
                }
                dir = Vector3.Cross(Vector3.up, toPlayer) * s.StrafeSign;
                speed *= strafeSpeedScale;
            }
            else
            {
                ctx.Move(Vector3.zero);
                return false;
            }
            if (avoidObstacles) dir = EnemySteering.Avoid(ctx, dir);
            ctx.Move(dir * speed);
            return false;
        }
    }
}
