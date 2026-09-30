using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float gravity = 2f;
    [SerializeField] float maxLifetime = 6f;
    [SerializeField] float homingRange = 15f;
    [SerializeField] float homingCone = 70f;

    static readonly Collider[] buffer = new Collider[32];

    Vector3 velocity;
    ShotRecipe recipe;
    int pierceLeft;
    float traveled;
    float age;
    float radius;
    float retargetTimer;
    Collider target;
    Transform owner;
    readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

    public void Init(Vector3 direction, ShotRecipe recipe, Transform owner)
    {
        this.recipe = recipe;
        this.owner = owner;
        pierceLeft = recipe.Pierce;
        velocity = direction.normalized * recipe.Speed;

        transform.localScale *= recipe.SizeScale;
        radius = 0.5f * transform.lossyScale.x;

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = ColorFor(recipe.DamageType);
        }
    }

    static Color ColorFor(DamageType t)
    {
        switch (t)
        {
            case DamageType.Fire: return new Color(1f, 0.45f, 0.1f);
            case DamageType.Dark: return new Color(0.45f, 0.1f, 0.6f);
            case DamageType.Holy: return new Color(1f, 0.92f, 0.55f);
            case DamageType.Blood: return new Color(0.7f, 0.05f, 0.08f);
            default: return new Color(0.55f, 0.55f, 0.55f);
        }
    }

    void Update()
    {
        if (recipe == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        age += dt;

        float g = gravity;
        if (recipe.Homing > 0f)
        {
            g *= 0.3f;
            Steer(dt);
        }
        velocity += Vector3.down * g * dt;

        Vector3 step = velocity * dt;
        float dist = step.magnitude;
        if (dist <= 0f)
        {
            return;
        }

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, step.normalized, dist, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (HandleHit(hits[i]))
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        transform.position += step;
        traveled += dist;

        if (traveled >= recipe.Range || age >= maxLifetime || transform.position.y < -50f)
        {
            Destroy(gameObject);
        }
    }

    void Steer(float dt)
    {
        retargetTimer -= dt;
        if (retargetTimer <= 0f || target == null || !target.enabled)
        {
            retargetTimer = 0.1f;
            target = FindTarget();
        }

        if (target == null)
        {
            return;
        }

        Vector3 to = target.bounds.center - transform.position;
        float speed = velocity.magnitude;
        float maxRadians = recipe.Homing * Mathf.Deg2Rad * dt;
        velocity = Vector3.RotateTowards(velocity, to.normalized * speed, maxRadians, 0f);
    }

    Collider FindTarget()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, homingRange, buffer, ~0, QueryTriggerInteraction.Ignore);
        Collider best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < n; i++)
        {
            Collider c = buffer[i];
            IDamageable d = c.GetComponentInParent<IDamageable>();
            if (d == null || alreadyHit.Contains(d))
            {
                continue;
            }

            Vector3 to = c.bounds.center - transform.position;
            if (Vector3.Angle(velocity, to) > homingCone)
            {
                continue;
            }

            float dist = to.sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = c;
            }
        }
        return best;
    }

    bool HandleHit(RaycastHit hit)
    {
        if (owner != null && hit.transform.root == owner.root)
        {
            return false;
        }

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target == null)
        {
            GameFeel.Impact(hit.point);
            return true;
        }

        if (alreadyHit.Contains(target))
        {
            return false;
        }
        alreadyHit.Add(target);

        target.TakeDamage(recipe.Damage, hit.point);
        GameEvents.RaiseEnemyHit(hit.point, recipe.Damage, recipe);

        if (recipe.BurnDps > 0f && recipe.BurnDuration > 0f)
        {
            IStatusTarget status = hit.collider.GetComponentInParent<IStatusTarget>();
            if (status != null)
            {
                status.ApplyBurn(recipe.BurnDps, recipe.BurnDuration);
            }
        }

        if (pierceLeft > 0)
        {
            pierceLeft--;
            return false;
        }
        return true;
    }
}