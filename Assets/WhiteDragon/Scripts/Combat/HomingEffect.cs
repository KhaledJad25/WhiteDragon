using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Curves the shot toward the nearest damageable target in a forward cone.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Effects/Homing", fileName = "Homing")]
    public class HomingEffect : ShotEffect
    {
        [Header("Homing")]
        [Tooltip("How fast rocks turn, in degrees per second. Multiplied by stacks: two sources turn twice as fast.")]
        [Min(0f)]
        public float turnRateDegrees = 180f;
        [Tooltip("Rocks only chase targets within this many degrees of their flight direction. 45 = a 90 degree cone.")]
        [Range(0f, 180f)]
        public float coneHalfAngle = 45f;
        [Tooltip("How far away (meters) a rock looks for targets.")]
        [Min(0f)]
        public float searchRadius = 15f;
        [Tooltip("Seconds between target searches. Lower = snappier, slightly more cost.")]
        [Min(0.02f)]
        public float retargetInterval = 0.1f;
        [Tooltip("Gravity multiplier while a target is locked. 0 = flies straight, 1 = normal arc.")]
        [Range(0f, 1f)]
        public float gravityScaleWhileHoming = 0.2f;

        class State
        {
            public Transform Target;
            public float RetargetTimer;
        }

        static readonly Collider[] overlapBuffer = new Collider[128];

        public override object CreateState() => new State();

        public override bool ResetState(object state)
        {
            var s = (State)state;
            s.Target = null;
            s.RetargetTimer = 0f;
            return true;
        }

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
            int count = Physics.OverlapSphereNonAlloc(pos, searchRadius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = overlapBuffer[i];
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
