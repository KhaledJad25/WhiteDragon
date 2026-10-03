using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// What behaviors and brains see of one enemy: its parts, effective stats, the player, per-enemy randomness,
    /// and helpers for moving, facing, sensing and attacking. One per enemy, created once; no per-frame garbage.
    /// Movement requested with Move is applied once at the end of the enemy's frame, after every behavior ran.
    /// </summary>
    public class EnemyContext
    {
        const float DefaultTargetRadius = 0.35f;
        static readonly RaycastHit[] losHits = new RaycastHit[8];

        public readonly Enemy Enemy;
        public readonly Transform Transform;
        public readonly CharacterController Controller;
        public readonly StatusReceiver Statuses;
        public readonly ActorStateEvents Events;

        /// <summary>Effective numbers (definition with variant applied).</summary>
        public EnemyStats Stats;
        public MovementMode Movement;
        /// <summary>The player, or null.</summary>
        public PlayerHealth Target;
        /// <summary>This enemy's own deterministic generator (run seed + spawn key). Use it for all its gameplay randomness.</summary>
        public RunRandom Random;
        /// <summary>Shown above the enemy by debug tools; brains set it to the state name.</summary>
        public string DebugLabel = "";
        /// <summary>How many times this enemy has taken damage (the TookDamage condition compares it).</summary>
        public int DamageTakenCount;

        // Frame movement, applied by Enemy after the brain ran.
        internal Vector3 RequestedVelocity;
        internal bool MoveRequested;
        internal bool Frozen;
        /// <summary>Collision flags of the last applied move (Dash stops on Sides).</summary>
        public CollisionFlags LastMoveFlags;

        CharacterController targetController;

        public EnemyContext(Enemy enemy)
        {
            Enemy = enemy;
            Transform = enemy.transform;
            Controller = enemy.GetComponent<CharacterController>();
            Statuses = enemy.GetComponent<StatusReceiver>();
            Events = ActorStateEvents.For(enemy.gameObject);
        }

        // ---------- Target ----------

        public void SetTarget(PlayerHealth player)
        {
            Target = player;
            targetController = player != null ? player.GetComponent<CharacterController>() : null;
        }

        public bool HasTarget => Target != null && !Target.State.IsDead;
        public Vector3 Position => Transform.position;
        public Vector3 TargetPosition => Target != null ? Target.transform.position : Position;

        /// <summary>Middle of the player's body (aim point).</summary>
        public Vector3 TargetCenter
        {
            get
            {
                if (Target == null) return Position;
                return targetController != null ? Target.transform.TransformPoint(targetController.center) : Target.transform.position + Vector3.up;
            }
        }

        /// <summary>Middle of this enemy's body.</summary>
        public Vector3 Center => Transform.TransformPoint(Controller.center);

        /// <summary>Horizontal distance to the player (meters).</summary>
        public float DistanceToTarget
        {
            get
            {
                Vector3 to = TargetPosition - Position;
                to.y = 0f;
                return to.magnitude;
            }
        }

        /// <summary>Horizontal unit direction to the player (zero if on top of it).</summary>
        public Vector3 DirectionToTarget
        {
            get
            {
                Vector3 to = TargetPosition - Position;
                to.y = 0f;
                float d = to.magnitude;
                return d > 1e-4f ? to / d : Vector3.zero;
            }
        }

        public float HealthFraction => Stats.MaxHealth > 0f ? Enemy.Health / Stats.MaxHealth : 0f;

        /// <summary>Nothing but the player between this enemy's eyes and the player's body.</summary>
        public bool HasLineOfSight()
        {
            if (Target == null) return false;
            Vector3 eye = Transform.TransformPoint(Controller.center + Vector3.up * Controller.height * 0.3f);
            Vector3 to = TargetCenter - eye;
            float distance = to.magnitude;
            if (distance < 1e-3f) return true;
            int count = Physics.RaycastNonAlloc(eye, to / distance, losHits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var t = losHits[i].collider.transform;
                if (t.IsChildOf(Transform) || t.IsChildOf(Target.transform)) continue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Touching the player: horizontal distance within both body radii plus extraReach, and the height difference
        /// of their feet no more than this enemy's height. Contact damage and dashes use this.
        /// </summary>
        public bool InContactRange(float extraReach)
        {
            if (Target == null) return false;
            float reach = Controller.radius * Transform.lossyScale.x
                          + (targetController != null ? targetController.radius : DefaultTargetRadius) + extraReach;
            if (DistanceToTarget > reach) return false;
            float verticalGap = Mathf.Abs(TargetPosition.y - Position.y);
            return verticalGap <= Controller.height;
        }

        // ---------- Moving ----------

        /// <summary>
        /// Ask to move at this world velocity this frame (meters per second). Status slows apply. Ground enemies
        /// ignore the vertical part (gravity handles it); Flying enemies use it. Several calls add up.
        /// </summary>
        public void Move(Vector3 velocity)
        {
            velocity *= Statuses != null ? Statuses.SpeedMultiplier : 1f;
            if (Movement == MovementMode.Ground) velocity.y = 0f;
            RequestedVelocity += velocity;
            MoveRequested = true;
            Events.Raise(ActorStateEvents.Locomotion(new Vector2(velocity.x, velocity.z).magnitude));
        }

        /// <summary>Walk or fly toward a point at speed, steering around obstacles when asked.</summary>
        public void MoveToward(Vector3 point, float speed, bool avoidObstacles)
        {
            Vector3 to = point - Position;
            if (Movement == MovementMode.Ground) to.y = 0f;
            float d = to.magnitude;
            if (d < 1e-4f)
            {
                Move(Vector3.zero);
                return;
            }
            Vector3 dir = to / d;
            if (avoidObstacles) dir = EnemySteering.Avoid(this, dir);
            Move(dir * speed);
        }

        /// <summary>No movement this frame, whatever other behaviors ask (telegraphs use it).</summary>
        public void Freeze() => Frozen = true;

        /// <summary>Turn to face a direction (horizontal). maxDegrees limits the turn this frame; negative = instant.</summary>
        public void Face(Vector3 direction, float maxDegrees = -1f)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-8f) return;
            var look = Quaternion.LookRotation(direction);
            Transform.rotation = maxDegrees < 0f ? look : Quaternion.RotateTowards(Transform.rotation, look, maxDegrees);
        }

        public void FaceTarget(float maxDegrees = -1f) => Face(TargetPosition - Position, maxDegrees);

        // ---------- Attacking ----------

        /// <summary>
        /// Deal damage from this enemy (team checked). Damage to the player is in half hearts (Teams.ToHalfHearts).
        /// Contact, projectiles and area attacks all end in IDamageable.TakeDamage.
        /// </summary>
        public bool Damage(IDamageable target, float amount, Vector3 point)
        {
            if (target == null || !Teams.CanDamage(Team.Enemy, target.Team)) return false;
            target.TakeDamage(amount, point);
            return true;
        }

        /// <summary>Fire a pooled enemy projectile (team Enemy: hurts only the player, passes other enemies).</summary>
        public Projectile FireProjectile(ShotRecipe recipe, Vector3 origin, Vector3 direction)
        {
            return Projectile.Spawn(recipe, origin, direction, Transform, Team.Enemy);
        }

        internal void BeginFrame()
        {
            RequestedVelocity = Vector3.zero;
            MoveRequested = false;
            Frozen = false;
        }
    }
}
