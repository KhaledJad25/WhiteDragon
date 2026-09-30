using System.Collections.Generic;
using UnityEngine;

// A simple trigger volume that damages IDamageable entities standing inside it (e.g. spikes, lava, corruption pool).
[RequireComponent(typeof(Collider))]
public class DamageHazard : MonoBehaviour
{
    [Header("Hazard Settings")]
    [Tooltip("Amount of damage dealt per tick (1 = 1 half-heart)")]
    [SerializeField] float damageAmount = 1f;

    [Tooltip("Time between damage ticks in seconds")]
    [SerializeField] float tickInterval = 1f;

    readonly Dictionary<IDamageable, float> nextDamageTime = new();
    readonly List<IDamageable> toRemove = new();

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            if (!nextDamageTime.ContainsKey(damageable) || Time.time >= nextDamageTime[damageable])
            {
                damageable.TakeDamage(damageAmount, other.ClosestPoint(transform.position));
                nextDamageTime[damageable] = Time.time + tickInterval;
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            if (!nextDamageTime.TryGetValue(damageable, out float nextTime) || Time.time >= nextTime)
            {
                damageable.TakeDamage(damageAmount, other.ClosestPoint(transform.position));
                nextDamageTime[damageable] = Time.time + tickInterval;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            nextDamageTime.Remove(damageable);
        }
    }

    void Update()
    {
        // Clean up destroyed or null references
        if (nextDamageTime.Count > 0)
        {
            toRemove.Clear();
            foreach (var kvp in nextDamageTime)
            {
                if (kvp.Key == null || (kvp.Key is Component c && c == null))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                nextDamageTime.Remove(toRemove[i]);
            }
        }
    }
}
