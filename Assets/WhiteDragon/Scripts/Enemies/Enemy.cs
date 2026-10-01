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
        Renderer[] renderers;
        PlayerHealth target;
        CharacterController targetController;
        float health;
        float verticalVelocity;
        float nextContactTime;
        float flashTimer;
        bool dead;
        bool dormant;
        bool initialized;

        public bool IsDead => dead;
        public bool IsDormant => dormant;
        public float Health => health;
        public event Action<Enemy> Died;

        Color Tint => definition != null ? definition.tint : Color.grey;

        void Awake() => Initialize();

        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            controller = GetComponent<CharacterController>();
            statuses = GetComponent<StatusReceiver>();
            renderers = GetComponentsInChildren<Renderer>();
            health = definition != null ? definition.maxHealth : 10f;
            if (Application.isPlaying) SetColor(Tint);
        }

        /// <summary>Dormant enemies stand still until their room activates them.</summary>
        public void SetDormant(bool value) => dormant = value;

        void Update()
        {
            if (dead) return;
            float dt = Time.deltaTime;

            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                SetColor(Color.Lerp(Tint, flashColor, Mathf.Clamp01(flashTimer / flashTime)));
            }
            if (dormant) return;

            if (target == null)
            {
                target = FindAnyObjectByType<PlayerHealth>();
                if (target != null) targetController = target.GetComponent<CharacterController>();
            }

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
                TryContact(distance, verticalGap);
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
            target.Damage(definition != null ? definition.contactDamage : 1);
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            Initialize();
            if (dead) return;
            health -= amount;
            flashTimer = flashTime;
            if (health <= 0f) Kill();
        }

        public void Kill()
        {
            Initialize();
            if (dead) return;
            dead = true;
            health = 0f;
            statuses.ClearAll();
            GameFeel.OnKill(transform.position + Vector3.up, Tint);
            Died?.Invoke(this);
            if (Application.isPlaying) Destroy(gameObject);
        }

        void SetColor(Color c)
        {
            foreach (var r in renderers)
                if (r != null) r.material.color = c;
        }
    }
}
