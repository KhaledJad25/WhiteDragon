using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Stationary test target. Never attacks; respawns at full health after a delay.</summary>
    public class TargetDummy : MonoBehaviour, IDamageable
    {
        public float maxHealth = 50f;
        public float respawnDelay = 2f;
        public Color tint = new Color(0.45f, 0.32f, 0.25f);
        public Color flashColor = Color.white;
        public float flashTime = 0.1f;

        float health;
        bool dead;
        Renderer[] renderers;
        Collider[] colliders;
        RendererTint tinter;

        public float Health => health;
        public bool IsDead => dead;

        void Awake()
        {
            health = maxHealth;
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            tinter = RendererTint.For(gameObject);
            tinter.SetBaseColor(tint);
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            if (dead) return;
            health -= amount;
            tinter.Flash(flashColor, flashTime);
            if (health <= 0f) Die();
        }

        void Die()
        {
            dead = true;
            var statuses = GetComponent<StatusReceiver>();
            if (statuses != null) statuses.ClearAll();
            GameFeel.OnKill(transform.position + Vector3.up, tint);
            SetVisible(false);
            Invoke(nameof(Respawn), respawnDelay);
        }

        void Respawn()
        {
            health = maxHealth;
            dead = false;
            tinter.StopFlash();
            SetVisible(true);
        }

        void SetVisible(bool visible)
        {
            foreach (var r in renderers) r.enabled = visible;
            foreach (var c in colliders) c.enabled = visible;
        }
    }
}
