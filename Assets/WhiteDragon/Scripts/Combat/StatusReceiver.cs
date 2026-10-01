using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Holds active statuses on anything damageable: ticks their damage through IDamageable,
    /// exposes a combined SpeedMultiplier, tints the renderers while any status is active, and
    /// attaches each status's optional vfxPrefab for as long as it lasts.
    /// </summary>
    public class StatusReceiver : MonoBehaviour
    {
        public class ActiveStatus
        {
            public StatusEffectDefinition Definition;
            public int Stacks;
            public float Remaining;
            public float TickTimer;
            /// <summary>Spawned vfxPrefab instance, or null.</summary>
            public GameObject Vfx;
        }

        [Range(0f, 1f)] public float tintStrength = 0.6f;

        readonly List<ActiveStatus> active = new List<ActiveStatus>();
        IDamageable damageable;
        RendererTint tinter;

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
                var added = new ActiveStatus { Definition = definition, Stacks = amount, Remaining = definition.duration };
                if (definition.vfxPrefab != null)
                {
                    added.Vfx = Instantiate(definition.vfxPrefab, transform, false);
                    added.Vfx.name = definition.vfxPrefab.name;
                    added.Vfx.transform.localPosition = Vector3.zero;
                }
                active.Add(added);
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

        public void ClearAll()
        {
            foreach (var a in active.ToArray()) Remove(a);
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (damageable == null) damageable = GetComponent<IDamageable>();
            // Index loop (no per-frame copy). Damage can clear the list (death), so re-check as we go.
            for (int i = 0; i < active.Count; i++)
            {
                var a = active[i];
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
                        DamageNumber.Spawn(point, damage, def.tint, this);
                    }
                }

                if (a.Remaining <= 0f && active.Contains(a))
                {
                    Remove(a);
                    i--;
                }
            }
        }

        void LateUpdate()
        {
            if (tinter == null) tinter = RendererTint.For(gameObject);
            if (active.Count == 0) tinter.SetOverlay(Color.clear, 0f);
            else tinter.SetOverlay(active[active.Count - 1].Definition.tint, tintStrength);
        }

        void Remove(ActiveStatus a)
        {
            active.Remove(a);
            if (a.Vfx == null) return;
            if (Application.isPlaying) Destroy(a.Vfx);
            else DestroyImmediate(a.Vfx);
            a.Vfx = null;
        }

        ActiveStatus Find(StatusEffectDefinition definition)
        {
            foreach (var a in active)
                if (a.Definition == definition) return a;
            return null;
        }
    }
}
