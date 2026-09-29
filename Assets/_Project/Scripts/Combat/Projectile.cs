using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float gravity = 2f;
    [SerializeField] float maxLifetime = 6f;

    Vector3 velocity;
    float damage;
    float range;
    float traveled;
    float age;
    float radius;
    Transform owner;

    public void Init(Vector3 direction, float damage, float speed, float range, Transform owner)
    {
        this.velocity = direction.normalized * speed;
        this.damage = damage;
        this.range = range;
        this.owner = owner;
        this.radius = 0.5f * transform.lossyScale.x;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        velocity += Vector3.down * gravity * dt;

        Vector3 step = velocity * dt;
        float dist = step.magnitude;
        if (dist <= 0f)
        {
            return;
        }

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, radius, step.normalized, out hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            bool isOwner = owner != null && hit.transform.root == owner.root;
            if (!isOwner)
            {
                HitSomething(hit);
                return;
            }
        }

        transform.position += step;
        traveled += dist;

        if (traveled >= range || age >= maxLifetime || transform.position.y < -50f)
        {
            Destroy(gameObject);
        }
    }

    void HitSomething(RaycastHit hit)
    {
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage, hit.point);
        }
        else
        {
            GameFeel.Impact(hit.point);
        }
        Destroy(gameObject);
    }
}