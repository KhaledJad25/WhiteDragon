using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// A thrown rock. Moves itself and sweeps with SphereCast every frame so it never tunnels.
    /// Effects only plug in through ShotEffect hooks; nothing effect-specific belongs here.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public const float BaseRadius = 0.12f;
        public float gravity = 2f;
        public float lifetime = 6f;

        ShotRecipe recipe;
        Transform owner;
        Vector3 velocity;
        float travelled;
        float age;
        int pierceLeft;
        bool despawned;
        // Targets already hit (pierce) or explicitly ignored; never damaged again by this projectile.
        readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        readonly List<ShotEffectInstance> effects = new List<ShotEffectInstance>();

        public ShotRecipe Recipe => recipe;
        public Transform Owner => owner;
        public float Radius => BaseRadius * recipe.SizeScale;
        public Vector3 Velocity { get => velocity; set => velocity = value; }
        /// <summary>Multiplier on gravity; effects may change it.</summary>
        public float GravityScale { get; set; } = 1f;
        public bool IsDespawned => despawned;

        public static Projectile Spawn(ShotRecipe recipe, Vector3 position, Vector3 direction, Transform owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Rock";
            DestroySafe(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (BaseRadius * 2f * recipe.SizeScale);
            go.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.Lit(DamageTypeColors.Tint(recipe.DamageType));

            var p = go.AddComponent<Projectile>();
            p.recipe = recipe;
            p.owner = owner;
            p.velocity = direction.normalized * recipe.Speed;
            p.pierceLeft = recipe.Pierce;
            foreach (var stack in recipe.Effects)
                p.effects.Add(new ShotEffectInstance(stack.Effect, stack.Stacks, p));
            foreach (var e in p.effects)
                e.Effect.OnSpawn(e);
            return p;
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (despawned) return;
            age += dt;
            if (age >= lifetime) { Despawn(); return; }

            foreach (var e in effects)
                e.Effect.OnUpdate(e, dt);
            if (despawned) return;

            velocity += Vector3.down * (gravity * GravityScale * dt);
            Vector3 step = velocity * dt;
            float distance = step.magnitude;
            if (distance <= 0f) return;
            Vector3 position = transform.position;
            Vector3 direction = step / distance;
            Color tint = DamageTypeColors.Tint(recipe.DamageType);

            var hits = Physics.SphereCastAll(position, Radius, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;
                Vector3 point = hit.distance <= 0f && hit.point == Vector3.zero ? position : hit.point;
                var target = hit.collider.GetComponentInParent<IDamageable>();

                if (target == null)
                {
                    GameFeel.OnWallImpact(point, tint);
                    Despawn();
                    return;
                }
                if (!hitTargets.Add(target)) continue;

                target.TakeDamage(recipe.Damage, point);
                DamageNumber.Spawn(point, recipe.Damage, tint);
                GameFeel.OnHit(point, tint);
                foreach (var e in effects)
                    e.Effect.OnHit(e, target, point);

                if (pierceLeft-- <= 0)
                {
                    Despawn();
                    return;
                }
            }

            transform.position = position + step;
            travelled += distance;
            if (travelled >= recipe.Range) Despawn();
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
            DestroySafe(gameObject);
        }

        static void DestroySafe(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
