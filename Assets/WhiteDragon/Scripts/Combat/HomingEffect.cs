using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Curves the shot toward the nearest damageable target in a forward cone.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Effects/Homing", fileName = "Homing")]
    public class HomingEffect : ShotEffect
    {
        [Tooltip("Degrees per second, multiplied by stacks.")]
        public float turnRateDegrees = 180f;
        [Tooltip("Half-angle of the search cone in degrees.")]
        public float coneHalfAngle = 45f;
        public float searchRadius = 15f;
        public float retargetInterval = 0.1f;
        [Tooltip("Gravity multiplier while a target is locked.")]
        public float gravityScaleWhileHoming = 0.2f;

        class State
        {
            public Transform Target;
            public float RetargetTimer;
        }

        public override object CreateState() => new State();

        public override void OnUpdate(ShotEffectInstance shot, float dt)
        {
            var state = (State)shot.State;
            var p = shot.Projectile;

            state.RetargetTimer -= dt;
            if (state.RetargetTimer <= 0f || state.Target == null || !state.Target.gameObject.activeInHierarchy)
            {
                state.RetargetTimer = retargetInterval;
                state.Target = FindTarget(p);
            }

            if (state.Target == null)
            {
                p.GravityScale = 1f;
                return;
            }

            p.GravityScale = gravityScaleWhileHoming;
            Vector3 v = p.Velocity;
            Vector3 toTarget = TargetPoint(state.Target) - p.transform.position;
            float maxRadians = turnRateDegrees * shot.Stacks * Mathf.Deg2Rad * dt;
            p.Velocity = Vector3.RotateTowards(v, toTarget.normalized * v.magnitude, maxRadians, 0f);
        }

        Transform FindTarget(Projectile p)
        {
            Vector3 pos = p.transform.position;
            Vector3 forward = p.Velocity.sqrMagnitude > 0f ? p.Velocity.normalized : p.transform.forward;
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (var col in Physics.OverlapSphere(pos, searchRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (p.Owner != null && col.transform.IsChildOf(p.Owner)) continue;
                var damageable = col.GetComponentInParent<IDamageable>() as Component;
                if (damageable == null) continue;
                Vector3 to = TargetPoint(damageable.transform) - pos;
                if (Vector3.Angle(forward, to) > coneHalfAngle) continue;
                float d = to.sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = damageable.transform;
                }
            }
            return best;
        }

        static Vector3 TargetPoint(Transform t)
        {
            var c = t.GetComponentInChildren<Collider>();
            return c != null && c.enabled ? c.bounds.center : t.position;
        }
    }
}
