using UnityEngine;

namespace WhiteDragon
{
    [EnemyBehaviorInfo("Do nothing for a while (recover, pause). Finishes after the duration.", "Timing")]
    [CreateAssetMenu(menuName = "WhiteDragon/Enemy Behaviors/Wait", fileName = "Wait")]
    public class WaitBehavior : EnemyBehavior<WaitBehavior.State>
    {
        public class State
        {
            public float Time;
        }

        [Min(0f)] public float duration = 1f;
        [Tooltip("Stand still while waiting.")]
        public bool freezeMovement = true;

        protected override void Enter(EnemyContext ctx, State s) => s.Time = 0f;

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            s.Time += dt;
            if (freezeMovement) ctx.Freeze();
            return s.Time >= duration - 1e-4f;
        }
    }
}
