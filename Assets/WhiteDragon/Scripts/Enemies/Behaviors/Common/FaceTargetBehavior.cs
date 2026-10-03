using UnityEngine;

namespace WhiteDragon
{
    [EnemyBehaviorInfo("Turn to face the player (instantly or at a turn speed). Never finishes.", "Movement")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Face Target", fileName = "FaceTarget")]
    public class FaceTargetBehavior : EnemyBehavior<FaceTargetBehavior.State>
    {
        public class State { }

        [Tooltip("Degrees per second. 0 = instant.")]
        [Min(0f)] public float turnSpeed;

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            if (ctx.HasTarget) ctx.FaceTarget(turnSpeed > 0f ? turnSpeed * dt : -1f);
            return false;
        }
    }
}
