using UnityEngine;

public class TargetDummy : MonoBehaviour, IDamageable, IStatusTarget
{
    [SerializeField] float maxHealth = 30f;
    [SerializeField] float respawnDelay = 2f;
    [SerializeField] Color flashColor = Color.red;
    [SerializeField] Color burnColor = new Color(1f, 0.5f, 0.1f);

    float health;
    float flashTimer;
    float burnDps;
    float burnLeft;
    float burnTick;
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
        if (dead)
        {
            return;
        }

        if (burnLeft > 0f)
        {
            burnLeft -= Time.deltaTime;
            burnTick -= Time.deltaTime;
            if (burnTick <= 0f)
            {
                burnTick += 0.5f;
                ApplyDamage(burnDps * 0.5f, transform.position + Vector3.up * 1.3f, false, burnColor);
            }
            if (burnLeft <= 0f)
            {
                burnDps = 0f;
            }
        }

        if (dead)
        {
            return;
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            rend.material.color = flashColor;
        }
        else if (burnLeft > 0f)
        {
            float flicker = 0.5f + 0.2f * Mathf.Sin(Time.time * 20f);
            rend.material.color = Color.Lerp(baseColor, burnColor, flicker);
        }
        else
        {
            rend.material.color = baseColor;
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        ApplyDamage(amount, hitPoint, true, Color.white);
    }

    public void ApplyBurn(float dps, float duration)
    {
        if (dead)
        {
            return;
        }

        burnDps = Mathf.Max(burnDps, dps);
        burnLeft = Mathf.Max(burnLeft, duration);
    }

    void ApplyDamage(float amount, Vector3 point, bool impact, Color textColor)
    {
        if (dead)
        {
            return;
        }

        health -= amount;
        if (impact)
        {
            flashTimer = 0.1f;
        }
        FloatingText.Spawn(point + Vector3.up * 0.3f, amount.ToString("0.#"), textColor);

        if (health <= 0f)
        {
            Die(point);
        }
        else if (impact)
        {
            GameFeel.Hit(point);
        }
    }

    void Die(Vector3 point)
    {
        dead = true;
        burnLeft = 0f;
        burnDps = 0f;
        GameFeel.Kill(point);
        GameEvents.RaiseEnemyKilled(point);
        rend.enabled = false;
        col.enabled = false;
        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn()
    {
        health = maxHealth;
        dead = false;
        burnLeft = 0f;
        burnDps = 0f;
        rend.enabled = true;
        col.enabled = true;
        rend.material.color = baseColor;
    }
}