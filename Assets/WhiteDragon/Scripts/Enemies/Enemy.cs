using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Placeholder enemy driven by an EnemyDefinition: walks at the player (slowed by statuses),
    /// deals contact damage on a cooldown, flashes when hit, and dies with feedback.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(StatusReceiver))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        public EnemyDefinition definition;
        public float contactCooldown = 1f;
        [Tooltip("Extra reach beyond both colliders' radii.")]
        public float contactReach = 0.25f;
        public float gravity = -25f;
        public Color flashColor = Color.white;
        public float flashTime = 0.1f;

        CharacterController controller;
        StatusReceiver statuses;
        RendererTint tint;
        ActorStateEvents events;
        GameObject visual;
        PlayerHealth target;
        CharacterController targetController;
        float health;
        float verticalVelocity;
        float nextContactTime;
        bool dead;
        bool dormant;
        bool initialized;

        public bool IsDead => dead;
        public bool IsDormant => dormant;
        public float Health => health;
        /// <summary>The spawned visualPrefab instance, or null when using placeholder shapes.</summary>
        public GameObject Visual => visual;
        /// <summary>Idle / Move / Attack / Hit / Die, for animation hooks.</summary>
        public ActorStateEvents Events => events;
        public event Action<Enemy> Died;

        Color Tint => definition != null ? definition.tint : Color.grey;

        static readonly System.Collections.Generic.List<Enemy> live = new System.Collections.Generic.List<Enemy>();

        /// <summary>Enabled enemies (debug/stress readout only).</summary>
        public static System.Collections.Generic.IReadOnlyList<Enemy> Live => live;

        void OnEnable() => live.Add(this);
        void OnDisable() => live.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => live.Clear();

        void Awake() => Initialize();

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            controller = GetComponent<CharacterController>();
            statuses = GetComponent<StatusReceiver>();
            health = definition != null ? definition.maxHealth : 10f;
            tint = RendererTint.For(gameObject);
            events = ActorStateEvents.For(gameObject);

            if (definition != null && definition.visualPrefab != null)
            {
                foreach (var placeholder in GetComponentsInChildren<Renderer>()) placeholder.enabled = false;
                visual = Instantiate(definition.visualPrefab, transform, false);
                visual.name = definition.visualPrefab.name;
                visual.transform.localPosition = Vector3.zero;
                tint.SetRenderers(visual.GetComponentsInChildren<Renderer>(true));
            }
            else if (Application.isPlaying)
            {
                tint.SetBaseColor(Tint);
            }
        }

        /// <summary>Dormant enemies stand still until their room activates them.</summary>
        public void SetDormant(bool value) => dormant = value;

        /// <summary>Chase this player instead of searching the scene for one.</summary>
        public void SetTarget(PlayerHealth player)
        {
            target = player;
            targetController = player != null ? player.GetComponent<CharacterController>() : null;
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>One step of chasing and contact damage (Update calls this every frame).</summary>
        public void Tick(float dt)
        {
            Initialize();
            if (dead) return;
            if (dormant)
            {
                events.Raise(ActorState.Idle);
                return;
            }

            if (target == null) SetTarget(FindAnyObjectByType<PlayerHealth>());

            Vector3 move = Vector3.zero;
            if (target != null && !target.State.IsDead)
            {
                Vector3 to = target.transform.position - transform.position;
                float verticalGap = Mathf.Abs(to.y);
                to.y = 0f;
                float distance = to.magnitude;
                if (distance > 0.05f)
                {
                    Vector3 dir = to / distance;
                    transform.rotation = Quaternion.LookRotation(dir);
                    float speed = (definition != null ? definition.moveSpeed : 2f) * statuses.SpeedMultiplier;
                    move = dir * speed;
                }
                events.Raise(ActorStateEvents.Locomotion(move.magnitude));
                TryContact(distance, verticalGap);
            }
            else
            {
                events.Raise(ActorState.Idle);
            }

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            verticalVelocity += gravity * dt;
            controller.Move((move + Vector3.up * verticalVelocity) * dt);
        }

        void TryContact(float horizontalDistance, float verticalGap)
        {
            float reach = controller.radius * transform.lossyScale.x
                          + (targetController != null ? targetController.radius : 0.35f)
                          + contactReach;
            if (horizontalDistance > reach || verticalGap > controller.height || Time.time < nextContactTime) return;
            nextContactTime = Time.time + contactCooldown;
            events.Raise(ActorState.Attack);
            target.Damage(definition != null ? definition.contactDamage : 1);
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            Initialize();
            if (dead) return;
            health -= amount;
            tint.Flash(flashColor, flashTime);
            if (health <= 0f) Kill();
            else events.Raise(ActorState.Hit);
        }

        public void Kill()
        {
            Initialize();
            if (dead) return;
            dead = true;
            health = 0f;
            statuses.ClearAll();
            events.Raise(ActorState.Die);
            GameFeel.OnKill(transform.position + Vector3.up, Tint);
            Died?.Invoke(this);
            if (!Application.isPlaying) return;

            float delay = definition != null ? definition.deathDelay : 0f;
            if (delay <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            controller.enabled = false;
            Destroy(gameObject, delay);
        }
    }
}
