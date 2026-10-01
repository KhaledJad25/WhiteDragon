using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Holds active statuses on anything damageable: ticks their damage through IDamageable,
    /// exposes a combined SpeedMultiplier, and tints the renderers while any status is active.
    /// </summary>
    public class StatusReceiver : MonoBehaviour
    {
        public class ActiveStatus
        {
            public StatusEffectDefinition Definition;
            public int Stacks;
            public float Remaining;
            public float TickTimer;
        }

        [Range(0f, 1f)] public float tintStrength = 0.6f;

        readonly List<ActiveStatus> active = new List<ActiveStatus>();
        IDamageable damageable;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        bool tinted;

        public IReadOnlyList<ActiveStatus> Active => active;

        public float SpeedMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var a in active) m *= a.Definition.speedMultiplier;
                return m;
            }
        }

        public int GetStacks(StatusEffectDefinition definition)
        {
            var a = Find(definition);
            return a == null ? 0 : a.Stacks;
        }

        public void Apply(StatusEffectDefinition definition, int intensity)
        {
            if (definition == null) return;
            int maxStacks = Mathf.Max(1, definition.maxStacks);
            int amount = Mathf.Clamp(intensity, 1, maxStacks);
            var a = Find(definition);
            if (a == null)
            {
                active.Add(new ActiveStatus { Definition = definition, Stacks = amount, Remaining = definition.duration });
                return;
            }
            switch (definition.stacking)
            {
                case StatusStacking.RefreshDuration:
                    a.Remaining = definition.duration;
                    a.Stacks = Mathf.Max(a.Stacks, amount);
                    break;
                case StatusStacking.StackIntensity:
                    a.Remaining = definition.duration;
                    a.Stacks = Mathf.Min(maxStacks, a.Stacks + amount);
                    break;
                case StatusStacking.Ignore:
                    break;
            }
        }

        public void ClearAll() => active.Clear();

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (damageable == null) damageable = GetComponent<IDamageable>();
            foreach (var a in active.ToArray())
            {
                if (!active.Contains(a)) continue;
                var def = a.Definition;
                float step = Mathf.Min(dt, a.Remaining);
                a.Remaining -= dt;

                if (def.damagePerSecond > 0f && damageable != null)
                {
                    float interval = Mathf.Max(0.05f, def.tickInterval);
                    a.TickTimer += step;
                    while (a.TickTimer >= interval - 1e-4f && active.Contains(a))
                    {
                        a.TickTimer -= interval;
                        float damage = def.damagePerSecond * interval * a.Stacks;
                        Vector3 point = transform.position + Vector3.up;
                        damageable.TakeDamage(damage, point);
                        DamageNumber.Spawn(point, damage, def.tint);
                    }
                }

                if (a.Remaining <= 0f) active.Remove(a);
            }
        }

        void LateUpdate()
        {
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>();
            if (block == null) block = new MaterialPropertyBlock();

            if (active.Count == 0)
            {
                if (!tinted) return;
                foreach (var r in renderers) r.SetPropertyBlock(null);
                tinted = false;
                return;
            }

            Color tint = active[active.Count - 1].Definition.tint;
            foreach (var r in renderers)
            {
                if (r.sharedMaterial == null) continue;
                Color c = Color.Lerp(r.sharedMaterial.color, tint, tintStrength);
                block.Clear();
                block.SetColor("_Color", c);
                block.SetColor("_BaseColor", c);
                r.SetPropertyBlock(block);
            }
            tinted = true;
        }

        ActiveStatus Find(StatusEffectDefinition definition)
        {
            foreach (var a in active)
                if (a.Definition == definition) return a;
            return null;
        }
    }
}
