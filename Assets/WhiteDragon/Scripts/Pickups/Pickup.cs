using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// A pickup lying in the world. No Update of its own: PickupManager moves, magnets, ages and collects every
    /// live pickup in one loop. Pooled; Begin resets everything.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public const float PlaceholderSize = 0.3f;

        public PickupDefinition Definition { get; private set; }
        public float Age { get; internal set; }
        public bool IsDespawned { get; internal set; } = true;
        /// <summary>The toss arc is over and it rests on the floor.</summary>
        public bool Landed { get; internal set; }
        public bool Visible { get; private set; } = true;
        /// <summary>Spawn order; the oldest is recycled at the cap.</summary>
        public long Serial { get; internal set; }

        internal Vector3 velocity;
        internal float groundY;
        internal float retryAt;
        internal int activeIndex = -1;

        GameObject visual;
        GameObject visualPrefab;
        Renderer[] renderers = new Renderer[0];

        public GameObject Visual => visual;

        internal void Begin(PickupDefinition definition, Vector3 position, Vector3 tossVelocity, float floorY, long serial)
        {
            Definition = definition;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            velocity = tossVelocity;
            groundY = floorY;
            Landed = tossVelocity == Vector3.zero && position.y <= floorY;
            Age = 0f;
            retryAt = 0f;
            Serial = serial;
            IsDespawned = false;
            UpdateVisual();
            SetVisible(true);
        }

        /// <summary>Child "Visual": the definition's worldPrefab, or a small sphere tinted from its effects.</summary>
        void UpdateVisual()
        {
            var prefab = Definition.worldPrefab;
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
                    foreach (var c in visual.GetComponentsInChildren<Collider>(true)) DestroySafe(c);
                }
                else
                {
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroySafe(visual.GetComponent<Collider>());
                    visual.transform.SetParent(transform, false);
                    visual.transform.localScale = Vector3.one * PlaceholderSize;
                }
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.up * (PlaceholderSize * 0.5f);
                renderers = visual.GetComponentsInChildren<Renderer>(true);
            }
            if (prefab == null) renderers[0].sharedMaterial = PlaceholderMaterials.Lit(Definition.EffectiveTint);
        }

        /// <summary>Shows or hides the visual (blinking before it despawns). Only touches renderers on a change.</summary>
        internal void SetVisible(bool visible)
        {
            if (visible == Visible) return;
            Visible = visible;
            foreach (var r in renderers)
                if (r != null) r.enabled = visible;
        }

        internal static void DestroySafe(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
