using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// A thrown rock. Moves itself and sweeps with SphereCast every frame so it never tunnels, and stops
    /// at exactly its range at any frame rate. Effects only plug in through ShotEffect hooks; nothing
    /// effect-specific belongs here. Rocks are pooled (fully reset on reuse) and capped: when
    /// GameFeel.MaxProjectiles are alive, the oldest is recycled.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public const float BaseRadius = 0.12f;
        public const float DefaultLifetime = 6f;
        const int HitBufferSize = 64;

        public float gravity = 2f;
        public float lifetime = DefaultLifetime;

        static readonly RaycastHit[] hitBuffer = new RaycastHit[HitBufferSize];
        static readonly IComparer<RaycastHit> byDistance = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
        static readonly Stack<Projectile> pool = new Stack<Projectile>();
        static readonly List<Projectile> active = new List<Projectile>();
        static long spawnCounter;
        static bool capReported;

        ShotRecipe recipe;
        Transform owner;
        Team team;
        Vector3 velocity;
        float travelled;
        float age;
        int pierceLeft;
        bool despawned = true;
        int activeIndex = -1;
        long serial;
        GameObject visual;
        GameObject visualPrefab;
        Renderer sphereRenderer;
        // Targets already hit (pierce) or explicitly ignored; never damaged again by this projectile.
        readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        // Reused between lives; only the first effectCount entries are in use.
        readonly List<ShotEffectInstance> effects = new List<ShotEffectInstance>();
        int effectCount;

        public ShotRecipe Recipe => recipe;
        public Transform Owner => owner;
        /// <summary>Who fired it; Teams.CanDamage decides what it can hurt.</summary>
        public Team Team => team;
        public float Radius => BaseRadius * recipe.SizeScale;
        public Vector3 Velocity { get => velocity; set => velocity = value; }
        /// <summary>Multiplier on gravity; effects may change it.</summary>
        public float GravityScale { get; set; } = 1f;
        public bool IsDespawned => despawned;
        /// <summary>Path length flown so far.</summary>
        public float Travelled => travelled;
        public int EffectCount => effectCount;
        public ShotEffectInstance GetEffect(int index) => effects[index];

        /// <summary>Live projectiles (debug/stress readout).</summary>
        public static int LiveCount => active.Count;
        /// <summary>How many rocks were recycled because the live cap was reached.</summary>
        public static int RecycledByCap { get; private set; }
        /// <summary>Lets tests use the pool outside play mode.</summary>
        public static bool PoolInEditMode;

        static bool Pooling => Application.isPlaying || PoolInEditMode;

        public static Projectile Spawn(ShotRecipe recipe, Vector3 position, Vector3 direction, Transform owner, Team team = Team.Player)
        {
            if (active.Count >= GameFeel.MaxProjectiles) RecycleOldest();

            var p = Pooling ? TakeFromPool() : null;
            if (p == null) p = new GameObject("Rock").AddComponent<Projectile>();
            p.transform.SetPositionAndRotation(position, Quaternion.identity);
            p.gameObject.SetActive(true);
            p.Begin(recipe, direction, owner, team);
            return p;
        }

        void Begin(ShotRecipe shotRecipe, Vector3 direction, Transform shotOwner, Team shotTeam)
        {
            recipe = shotRecipe;
            owner = shotOwner;
            team = shotTeam;
            velocity = direction.normalized * shotRecipe.Speed;
            pierceLeft = shotRecipe.Pierce;
            travelled = 0f;
            age = 0f;
            lifetime = DefaultLifetime;
            GravityScale = 1f;
            despawned = false;
            hitTargets.Clear();
            UpdateVisual();

            effectCount = 0;
            foreach (var stack in shotRecipe.Effects)
            {
                if (effectCount < effects.Count) effects[effectCount].Reset(stack.Effect, stack.Stacks, this);
                else effects.Add(new ShotEffectInstance(stack.Effect, stack.Stacks, this));
                effectCount++;
            }

            serial = ++spawnCounter;
            activeIndex = active.Count;
            active.Add(this);

            for (int i = 0; i < effectCount; i++)
                effects[i].Effect.OnSpawn(effects[i]);
        }

        /// <summary>Child "Visual": the ProjectileVisuals model for the damage type, or the placeholder sphere
        /// with the shared material of that damage type (no property block, so it batches).</summary>
        void UpdateVisual()
        {
            var prefab = ProjectileVisuals.Find(recipe.DamageType);
            if (visual != null && prefab != visualPrefab)
            {
                DestroySafe(visual);
                visual = null;
            }
            if (visual == null)
            {
                visualPrefab = prefab;
                if (prefab != null)
                {
                    visual = Instantiate(prefab, transform, false);
                    visual.transform.localPosition = Vector3.zero;
                    foreach (var c in visual.GetComponentsInChildren<Collider>(true)) DestroySafe(c);
                    foreach (var rb in visual.GetComponentsInChildren<Rigidbody>(true)) DestroySafe(rb);
                    sphereRenderer = null;
                }
                else
                {
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroySafe(visual.GetComponent<Collider>());
                    visual.transform.SetParent(transform, false);
                    sphereRenderer = visual.GetComponent<Renderer>();
                }
                visual.name = "Visual";
            }

            if (prefab != null)
            {
                visual.transform.localScale = prefab.transform.localScale * recipe.SizeScale;
            }
            else
            {
                visual.transform.localScale = Vector3.one * (BaseRadius * 2f * recipe.SizeScale);
                sphereRenderer.sharedMaterial = PlaceholderMaterials.Lit(DamageTypeColors.Tint(recipe.DamageType));
            }
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (despawned) return;
            age += dt;
            if (age >= lifetime) { Despawn(); return; }

            for (int i = 0; i < effectCount; i++)
                effects[i].Effect.OnUpdate(effects[i], dt);
            if (despawned) return;

            velocity += Vector3.down * (gravity * GravityScale * dt);
            Vector3 step = velocity * dt;
            float distance = step.magnitude;
            if (distance <= 0f) return;

            // Never fly past the range, whatever the frame rate.
            float remaining = recipe.Range - travelled;
            bool lastStep = distance >= remaining;
            if (lastStep)
            {
                step *= remaining / distance;
                distance = remaining;
            }

            Vector3 position = transform.position;
            Vector3 direction = step / distance;
            Color tint = DamageTypeColors.Tint(recipe.DamageType);

            int count = Physics.SphereCastNonAlloc(position, Radius, direction, hitBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hitBuffer, 0, count, byDistance);
            for (int h = 0; h < count; h++)
            {
                var hit = hitBuffer[h];
                if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;
                Vector3 point = hit.distance <= 0f && hit.point == Vector3.zero ? position : hit.point;
                var target = hit.collider.GetComponentInParent<IDamageable>();

                if (target == null)
                {
                    GameFeel.OnWallImpact(point, tint);
                    Despawn();
                    return;
                }
                // Wrong team: fly through it (enemy shots pass other enemies; player rocks pass the player).
                if (!Teams.CanDamage(team, target.Team)) continue;
                if (!hitTargets.Add(target)) continue;

                target.TakeDamage(recipe.Damage, point);
                // The player shows its own hurt feedback; floating numbers and bursts are for what the player hits.
                if (target.Team != Team.Player)
                {
                    DamageNumber.Spawn(point, recipe.Damage, DamageTypeColors.NumberColor(recipe.DamageType), target);
                    GameFeel.OnHit(point, tint);
                }
                for (int i = 0; i < effectCount; i++)
                    effects[i].Effect.OnHit(effects[i], target, point);

                if (pierceLeft-- <= 0)
                {
                    Despawn();
                    return;
                }
            }

            transform.position = position + step;
            travelled += distance;
            if (lastStep) Despawn();
        }

        /// <summary>This projectile will pass through target without damaging it or triggering OnHit.</summary>
        public void IgnoreTarget(IDamageable target)
        {
            if (target != null) hitTargets.Add(target);
        }

        public void Despawn()
        {
            if (despawned) return;
            despawned = true;
            RemoveFromActive();
            if (Pooling)
            {
                gameObject.SetActive(false);
                pool.Push(this);
            }
            else
            {
                DestroySafe(gameObject);
            }
        }

        void OnDestroy() => RemoveFromActive();

        void RemoveFromActive()
        {
            if (activeIndex < 0) return;
            int last = active.Count - 1;
            if (activeIndex <= last && active[activeIndex] == this)
            {
                var moved = active[last];
                active[activeIndex] = moved;
                if (moved != null) moved.activeIndex = activeIndex;
                active.RemoveAt(last);
            }
            activeIndex = -1;
        }

        static Projectile TakeFromPool()
        {
            while (pool.Count > 0)
            {
                var p = pool.Pop();
                if (p != null) return p;
            }
            return null;
        }

        static void RecycleOldest()
        {
            Projectile oldest = null;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p == null)
                {
                    active.RemoveAt(i);
                    continue;
                }
                if (oldest == null || p.serial < oldest.serial) oldest = p;
            }
            if (oldest == null) return;
            oldest.Despawn();
            RecycledByCap++;
            if (capReported) return;
            capReported = true;
            Debug.Log($"[Projectile] Live projectile cap ({GameFeel.MaxProjectiles}) reached; recycling the oldest rocks. See GameFeelSettings.maxProjectiles.");
        }

        /// <summary>Destroys pooled and live projectiles and forgets them (tests and scene cleanup).</summary>
        public static void ClearAll()
        {
            foreach (var p in active.ToArray()) if (p != null) DestroySafe(p.gameObject);
            while (pool.Count > 0)
            {
                var p = pool.Pop();
                if (p != null) DestroySafe(p.gameObject);
            }
            active.Clear();
        }

        static void DestroySafe(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            pool.Clear();
            active.Clear();
            spawnCounter = 0;
            RecycledByCap = 0;
            capReported = false;
            PoolInEditMode = false;
        }
    }
}
