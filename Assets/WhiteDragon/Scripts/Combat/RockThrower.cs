using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Hold left mouse to throw rocks at FireRate, aimed from the screen center.</summary>
    [RequireComponent(typeof(PlayerStats))]
    public class RockThrower : MonoBehaviour
    {
        public Camera aimCamera;
        [Tooltip("Spawn offset from the camera: right, up, forward.")]
        public Vector3 handOffset = new Vector3(0.2f, -0.25f, 0.6f);
        public float aimDistance = 200f;

        PlayerStats stats;
        float nextThrowTime;

        public ShotRecipe LastRecipe { get; private set; }
        public event Action<ShotRecipe> ShotBuilt;

        void Awake()
        {
            stats = GetComponent<PlayerStats>();
            if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            if (!CursorState.Locked || !GameInput.Fire.IsPressed() || Time.time < nextThrowTime) return;
            float rate = Mathf.Max(0.1f, stats.Stats.Get(StatType.FireRate));
            nextThrowTime = Time.time + 1f / rate;
            Throw();
        }

        public ShotRecipe BuildRecipe()
        {
            var inventory = GetComponent<PlayerInventory>();
            return ShotRecipeBuilder.Build(stats.Stats, inventory != null ? inventory.Loadout : null);
        }

        public void Throw()
        {
            var recipe = BuildRecipe();
            LastRecipe = recipe;
            ShotBuilt?.Invoke(recipe);

            Transform cam = aimCamera.transform;
            Vector3 aimPoint = FindAimPoint(cam.position, cam.forward);
            Vector3 spawn = cam.position + cam.right * handOffset.x + cam.up * handOffset.y + cam.forward * handOffset.z;
            Vector3 direction = (aimPoint - spawn).normalized;

            for (int i = 0; i < recipe.Count; i++)
            {
                float yaw = ShotRecipe.FanAngle(i, recipe.Count, recipe.SpreadDegrees);
                Projectile.Spawn(recipe, spawn, Quaternion.AngleAxis(yaw, cam.up) * direction, transform);
            }
            GameFeel.OnThrow(spawn);
        }

        Vector3 FindAimPoint(Vector3 origin, Vector3 forward)
        {
            var hits = Physics.RaycastAll(origin, forward, aimDistance, ~0, QueryTriggerInteraction.Ignore);
            float best = aimDistance;
            foreach (var h in hits)
                if (h.distance < best && !h.collider.transform.IsChildOf(transform))
                    best = h.distance;
            return origin + forward * best;
        }
    }
}
