using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// An enemy driven by an EnemyDefinition (plus an optional EnemyVariant). Its brain (definition.brain, or the
    /// variant's override) runs the AI through an EnemyContext; a definition without a brain uses the built-in
    /// chase-and-touch of the first enemies. Flashes when hit and dies with feedback.
    /// Randomness: each enemy has its own generator from the run seed and a stable spawn key (room id + index in
    /// the room's enemy list, else the hierarchy path), derived when it wakes and again when a run starts.
    /// Reordering a room's enemy list changes the keys, so it changes which enemy rolls what.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(StatusReceiver))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        public EnemyDefinition definition;
        [Tooltip("Built-in chase only (no brain): seconds between contact hits. Brains use MeleeContact instead.")]
        public float contactCooldown = 1f;
        [Tooltip("Built-in chase only (no brain): extra reach beyond both colliders' radii. Brains use MeleeContact instead.")]
        public float contactReach = 0.25f;
        public float gravity = -25f;
        public Color flashColor = Color.white;
        public float flashTime = 0.1f;
        [Tooltip("Optional variant applied on top of the definition (multipliers and overrides).")]
        public EnemyVariant variant;

        CharacterController controller;
        StatusReceiver statuses;
        RendererTint tint;
        ActorStateEvents events;
        GameObject visual;
        PlayerHealth target;
        CharacterController targetController;
        EnemyContext ctx;
        EnemyStats stats;
        EnemyBrainDefinition brain;
        object brainState;
        string spawnKey;
        float health;
        float verticalVelocity;
        float nextContactTime;
        bool dead;
        bool dormant;
        bool initialized;
        readonly List<float> thresholds = new List<float>();
        readonly List<bool> thresholdCrossed = new List<bool>();

        public bool IsDead => dead;
        public bool IsDormant => dormant;
        public float Health => health;
        /// <summary>The spawned visual prefab instance, or null when using placeholder shapes.</summary>
        public GameObject Visual => visual;
        /// <summary>Idle / Move / Attack / Hit / Die / Windup, for animation hooks.</summary>
        public ActorStateEvents Events => events;
        public Team Team => Team.Enemy;
        /// <summary>Effective stats (definition with variant applied).</summary>
        public EnemyStats Stats { get { Initialize(); return stats; } }
        public EnemyContext Context { get { Initialize(); return ctx; } }
        /// <summary>The running brain (null = built-in chase).</summary>
        public EnemyBrainDefinition Brain { get { Initialize(); return brain; } }
        /// <summary>This enemy's per-enemy brain data (debug and tests).</summary>
        public object BrainState => brainState;
        /// <summary>Its own deterministic generator; null until it first wakes.</summary>
        public RunRandom Random => ctx?.Random;
        /// <summary>Takes no damage while set (boss phases).</summary>
        public bool Invulnerable { get; set; }
        public string DebugLabel => ctx != null ? ctx.DebugLabel : "";

        public event Action<Enemy> Died;
        /// <summary>Health fell below a fraction registered with AddHealthThreshold (fires once per threshold).</summary>
        public event Action<Enemy, float> HealthThresholdCrossed;

        static readonly List<Enemy> live = new List<Enemy>();

        /// <summary>Enabled enemies (debug/stress readout only).</summary>
        public static IReadOnlyList<Enemy> Live => live;

        /// <summary>Debug: every enemy stands still and thinks nothing (F1 panel).</summary>
        public static bool FreezeAI;

        void OnEnable() => live.Add(this);
        void OnDisable() => live.Remove(this);
        void OnDestroy() => RunSession.RunStarted -= OnRunStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            live.Clear();
            FreezeAI = false;
        }

        void Awake() => Initialize();

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            controller = GetComponent<CharacterController>();
            statuses = GetComponent<StatusReceiver>();
            stats = EnemyStats.From(definition, variant);
            health = stats.MaxHealth;
            tint = RendererTint.For(gameObject);
            events = ActorStateEvents.For(gameObject);
            ctx = new EnemyContext(this)
            {
                Stats = stats,
                Movement = definition != null ? definition.movement : MovementMode.Ground,
            };
            brain = variant != null && variant.brain != null ? variant.brain : definition != null ? definition.brain : null;
            RunSession.RunStarted += OnRunStarted;

            if (variant != null && !Mathf.Approximately(stats.Scale, 1f)) transform.localScale *= stats.Scale;

            var prefab = variant != null && variant.visualPrefab != null ? variant.visualPrefab : definition != null ? definition.visualPrefab : null;
            if (prefab != null)
            {
                foreach (var placeholder in GetComponentsInChildren<Renderer>()) placeholder.enabled = false;
                visual = Instantiate(prefab, transform, false);
                visual.name = prefab.name;
                visual.transform.localPosition = Vector3.zero;
                tint.SetRenderers(visual.GetComponentsInChildren<Renderer>(true));
            }
            else if (Application.isPlaying)
            {
                tint.SetBaseColor(stats.Tint);
            }
        }

        /// <summary>Dormant enemies stand still until their room activates them.</summary>
        public void SetDormant(bool value)
        {
            Initialize();
            bool waking = dormant && !value;
            dormant = value;
            // Dormant (room not entered) and dead enemies run no Update at all.
            enabled = !dormant && !dead;
            if (waking && !dead) DeriveRandom();
        }

        /// <summary>Chase this player instead of searching the scene for one.</summary>
        public void SetTarget(PlayerHealth player)
        {
            Initialize();
            target = player;
            targetController = player != null ? player.GetComponent<CharacterController>() : null;
            ctx.SetTarget(player);
        }

        /// <summary>Stable key for this enemy's random generator (RoomController sets room id + list index).</summary>
        public void SetSpawnKey(string key) => spawnKey = key;

        public string SpawnKey => !string.IsNullOrEmpty(spawnKey) ? spawnKey : HierarchyPath(transform);

        /// <summary>Swap the brain at runtime (boss phases): the old one ends, the new one begins next frame.</summary>
        public void SetBrain(EnemyBrainDefinition newBrain)
        {
            Initialize();
            if (brain != null && brainState != null) brain.End(ctx, brainState);
            brain = newBrain;
            brainState = null;
        }

        /// <summary>Raise HealthThresholdCrossed once when health falls below this fraction (0-1) of max.</summary>
        public void AddHealthThreshold(float fraction)
        {
            thresholds.Add(fraction);
            thresholdCrossed.Add(false);
        }

        void OnRunStarted()
        {
            if (this == null)
            {
                RunSession.RunStarted -= OnRunStarted;
                return;
            }
            DeriveRandom();
        }

        void DeriveRandom()
        {
            var run = RunSession.Rng ?? new RunRandom(RunSession.Seed);
            ctx.Random = run.Derive(SpawnKey);
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>One frame of AI, movement and contact damage (Update calls this every frame).</summary>
        public void Tick(float dt)
        {
            Initialize();
            if (dead) return;
            if (dormant || FreezeAI)
            {
                events.Raise(ActorState.Idle);
                return;
            }

            if (target == null) SetTarget(FindAnyObjectByType<PlayerHealth>());
            if (brain == null)
            {
                BuiltInChase(dt);
                return;
            }

            if (ctx.Random == null) DeriveRandom();
            if (brainState == null)
            {
                brainState = brain.CreateState(ctx);
                brain.Begin(ctx, brainState);
            }
            ctx.BeginFrame();
            brain.Tick(ctx, brainState, dt);
            if (dead) return;
            ApplyMovement(dt);
        }

        void ApplyMovement(float dt)
        {
            Vector3 velocity = ctx.Frozen ? Vector3.zero : ctx.RequestedVelocity;
            if (!ctx.MoveRequested || ctx.Frozen) events.Raise(ActorState.Idle);
            if (ctx.Movement == MovementMode.Ground)
            {
                if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
                verticalVelocity += gravity * dt;
                velocity.y = verticalVelocity;
            }
            else
            {
                verticalVelocity = 0f;
            }
            ctx.LastMoveFlags = controller.Move(velocity * dt);
        }

        /// <summary>The first enemies' AI, kept for definitions without a brain: walk at the player, touch for damage.</summary>
        void BuiltInChase(float dt)
        {
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
                    move = dir * (stats.MoveSpeed * statuses.SpeedMultiplier);
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
            ctx.Damage(target, stats.ContactDamage, transform.position);
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            Initialize();
            if (dead || Invulnerable) return;
            health -= amount;
            ctx.DamageTakenCount++;
            tint.Flash(flashColor, flashTime);
            for (int i = 0; i < thresholds.Count; i++)
            {
                if (thresholdCrossed[i] || health >= thresholds[i] * stats.MaxHealth) continue;
                thresholdCrossed[i] = true;
                HealthThresholdCrossed?.Invoke(this, thresholds[i]);
            }
            if (health <= 0f) Kill();
            else events.Raise(ActorState.Hit);
        }

        public void Kill()
        {
            Initialize();
            if (dead) return;
            dead = true;
            enabled = false;
            health = 0f;
            if (brain != null && brainState != null) brain.End(ctx, brainState);
            brainState = null;
            statuses.ClearAll();
            events.Raise(ActorState.Die);
            GameFeel.OnKill(transform.position + Vector3.up, stats.Tint);
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

        static string HierarchyPath(Transform t) => t.parent == null ? t.name : HierarchyPath(t.parent) + "/" + t.name;
    }
}
