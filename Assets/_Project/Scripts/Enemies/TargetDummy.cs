using UnityEngine;

public class TargetDummy : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 30f;
    [SerializeField] float respawnDelay = 2f;
    [SerializeField] Color flashColor = Color.red;

    float health;
    float flashTimer;
    bool dead;
    Renderer rend;
    Collider col;
    Color baseColor;

    void Awake()
    {
        health = maxHealth;
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
        baseColor = rend.material.color;
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            rend.material.color = flashColor;
        }
        else
        {
            rend.material.color = baseColor;
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (dead)
        {
            return;
        }

        health -= amount;
        flashTimer = 0.1f;
        FloatingText.Spawn(hitPoint + Vector3.up * 0.3f, amount.ToString("0.#"));

        if (health <= 0f)
        {
            dead = true;
            GameFeel.Kill(hitPoint);
            rend.enabled = false;
            col.enabled = false;
            Invoke(nameof(Respawn), respawnDelay);
        }
        else
        {
            GameFeel.Hit(hitPoint);
        }
    }

    void Respawn()
    {
        health = maxHealth;
        dead = false;
        rend.enabled = true;
        col.enabled = true;
    }
}