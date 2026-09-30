using System;
using UnityEngine;

// Attached to the Player GameObject. Owns HealthState and handles i-frames, damage intake, and Dark heart bursts.
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [Tooltip("Starting health containers in half-heart units (6 = 3 full hearts)")]
    [SerializeField] int startingContainers = 6;

    [Tooltip("Invulnerability duration in seconds after taking damage")]
    [SerializeField] float invulnerabilityDuration = 1.0f;

    [Header("Dark Heart Corruption Burst")]
    [Tooltip("Radius of the offensive burst when a Dark heart breaks")]
    [SerializeField] float darkBurstRadius = 6.0f;

    [Tooltip("Damage dealt to nearby IDamageables when a Dark heart breaks")]
    [SerializeField] float darkBurstDamage = 15.0f;

    public HealthState Health { get; private set; } = new HealthState();

    public bool IsInvulnerable => iFrameTimer > 0f;
    public float InvulnerableTimer => iFrameTimer;
    public bool IsDead => Health != null && Health.IsDead;

    public event Action<float, Vector3> Damaged;
    public event Action Died;
    public event Action Revived;

    float iFrameTimer;
    PlayerController playerController;
    PlayerThrower playerThrower;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerThrower = GetComponent<PlayerThrower>();

        Health.AddContainer(startingContainers);
        Health.OverlayBroke += HandleOverlayBroke;
    }

    void OnDestroy()
    {
        if (Health != null)
        {
            Health.OverlayBroke -= HandleOverlayBroke;
        }
    }

    void Update()
    {
        if (iFrameTimer > 0f)
        {
            iFrameTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        if (IsDead || IsInvulnerable)
        {
            return;
        }

        int halfHearts = Mathf.Max(1, Mathf.RoundToInt(amount));
        iFrameTimer = invulnerabilityDuration;

        Health.Damage(halfHearts);
        GameFeel.Hit(hitPoint);
        Damaged?.Invoke(amount, hitPoint);

        if (Health.IsDead)
        {
            Die();
        }
    }

    void HandleOverlayBroke(HeartType type)
    {
        if (type == HeartType.Dark)
        {
            TriggerDarkBurst();
        }
    }

    void TriggerDarkBurst()
    {
        Vector3 origin = transform.position;
        GameFeel.Kill(origin + Vector3.up);

        Collider[] hits = Physics.OverlapSphere(origin, darkBurstRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col.gameObject == gameObject)
            {
                continue;
            }

            if (col.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector3 contact = col.ClosestPoint(origin);
                damageable.TakeDamage(darkBurstDamage, contact);
            }
        }
    }

    void Die()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        if (playerThrower != null)
        {
            playerThrower.enabled = false;
        }

        GameFeel.Kill(transform.position);
        Died?.Invoke();
    }

    public void Heal(int halfHearts)
    {
        Health.Heal(halfHearts);
    }

    public void AddContainers(int halfHearts = 2)
    {
        Health.AddContainer(halfHearts);
    }

    public void AddOverlay(HeartType type, int halfHearts = 1)
    {
        Health.AddOverlay(type, halfHearts);
    }

    public void Revive(int refillContainers = 6)
    {
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        if (playerThrower != null)
        {
            playerThrower.enabled = true;
        }

        iFrameTimer = 0f;
        Health.Overlay.Clear();
        Health.RedCurrent = Mathf.Min(Health.RedContainers, refillContainers);
        Health.Heal(refillContainers);

        Revived?.Invoke();
    }
}
