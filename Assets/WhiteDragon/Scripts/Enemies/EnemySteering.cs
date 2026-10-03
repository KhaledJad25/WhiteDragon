using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// The one place enemies steer around obstacles. No pathfinding yet: a few short raycasts pick the clearest
    /// direction near the wanted one. A navigation mesh can replace this later without touching behaviors.
    /// </summary>
    public static class EnemySteering
    {
        const float LookAhead = 1.5f;
        static readonly float[] TryAngles = { 35f, -35f, 70f, -70f, 105f, -105f };

        /// <summary>The wanted direction if it is clear, else the nearest clear one (or the wanted one if all are blocked).</summary>
        public static Vector3 Avoid(EnemyContext ctx, Vector3 direction)
        {
            if (Clear(ctx, direction)) return direction;
            foreach (float angle in TryAngles)
            {
                Vector3 d = Quaternion.AngleAxis(angle, Vector3.up) * direction;
                if (Clear(ctx, d)) return d;
            }
            return direction;
        }

        static bool Clear(EnemyContext ctx, Vector3 direction)
        {
            Vector3 origin = ctx.Center;
            float radius = ctx.Controller.radius * ctx.Transform.lossyScale.x * 0.9f;
            if (!Physics.SphereCast(origin, radius, direction, out var hit, LookAhead, ~0, QueryTriggerInteraction.Ignore)) return true;
            var t = hit.collider.transform;
            // The player is the goal, not an obstacle; never "avoid" ourselves.
            return t.IsChildOf(ctx.Transform) || (ctx.Target != null && t.IsChildOf(ctx.Target.transform));
        }
    }
}
