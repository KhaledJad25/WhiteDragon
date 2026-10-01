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
        [Tooltip("Optional hand/socket transform where rocks appear (e.g. on a first-person arms model). Empty = Hand Offset from the camera.")]
        public Transform handSocket;

        PlayerStats stats;
        readonly FireTimer fireTimer = new FireTimer();

        public ShotRecipe LastRecipe { get; private set; }
        public event Action<ShotRecipe> ShotBuilt;

        void Awake()
        {
            stats = GetComponent<PlayerStats>();
            if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            bool held = CursorState.Locked && GameInput.Fire.IsPressed();
            float rate = Mathf.Max(0.1f, Stats.Stats.Get(StatType.FireRate));
            int throws = fireTimer.Tick(Time.time, held, rate);
            for (int i = 0; i < throws; i++) Throw();
        }

        public ShotRecipe BuildRecipe()
        {
            var inventory = GetComponent<PlayerInventory>();
            return ShotRecipeBuilder.Build(Stats.Stats, inventory != null ? inventory.Loadout : null);
        }

        PlayerStats Stats => stats != null ? stats : (stats = GetComponent<PlayerStats>());

        public void Throw()
        {
            var recipe = BuildRecipe();
            LastRecipe = recipe;
            ShotBuilt?.Invoke(recipe);
            ActorStateEvents.For(gameObject).Raise(ActorState.Attack);

            Transform cam = aimCamera.transform;
            Vector3 aimPoint = FindAimPoint(cam.position, cam.forward);
            Vector3 spawn = HandPosition();
            Vector3 direction = (aimPoint - spawn).normalized;

            for (int i = 0; i < recipe.Count; i++)
            {
                float yaw = ShotRecipe.FanAngle(i, recipe.Count, recipe.SpreadDegrees);
                Projectile.Spawn(recipe, spawn, Quaternion.AngleAxis(yaw, cam.up) * direction, transform);
            }
            GameFeel.OnThrow(spawn);
        }

        /// <summary>Where rocks appear: the hand socket if assigned, otherwise Hand Offset from the camera.</summary>
        public Vector3 HandPosition()
        {
            if (handSocket != null) return handSocket.position;
            Transform cam = aimCamera.transform;
            return cam.position + cam.right * handOffset.x + cam.up * handOffset.y + cam.forward * handOffset.z;
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
