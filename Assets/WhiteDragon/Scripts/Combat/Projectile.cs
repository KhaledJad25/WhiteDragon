using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>A thrown rock. Moves itself and sweeps with SphereCast every frame so it never tunnels.</summary>
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
        readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        public ShotRecipe Recipe => recipe;
        public Transform Owner => owner;
        public float Radius => BaseRadius * recipe.SizeScale;
        public Vector3 Velocity { get => velocity; set => velocity = value; }

        public static Projectile Spawn(ShotRecipe recipe, Vector3 position, Vector3 direction, Transform owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Rock";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (BaseRadius * 2f * recipe.SizeScale);
            go.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.Lit(DamageTypeColors.Tint(recipe.DamageType));

            var p = go.AddComponent<Projectile>();
            p.recipe = recipe;
            p.owner = owner;
            p.velocity = direction.normalized * recipe.Speed;
            p.pierceLeft = recipe.Pierce;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (age >= lifetime) { Destroy(gameObject); return; }

            velocity += Vector3.down * (gravity * dt);
            Vector3 step = velocity * dt;
            float distance = step.magnitude;
            if (distance <= 0f) return;
            Vector3 position = transform.position;
            Vector3 direction = step / distance;

            var hits = Physics.SphereCastAll(position, Radius, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;
                Vector3 point = hit.distance <= 0f && hit.point == Vector3.zero ? position : hit.point;
                var target = hit.collider.GetComponentInParent<IDamageable>();
                Color tint = DamageTypeColors.Tint(recipe.DamageType);

                if (target == null)
                {
                    GameFeel.OnWallImpact(point, tint);
                    Destroy(gameObject);
                    return;
                }
                if (!hitTargets.Add(target)) continue;

                target.TakeDamage(recipe.Damage, point);
                DamageNumber.Spawn(point, recipe.Damage, tint);
                GameFeel.OnHit(point, tint);
                if (pierceLeft-- <= 0)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            transform.position = position + step;
            travelled += distance;
            if (travelled >= recipe.Range) Destroy(gameObject);
        }
    }
}
