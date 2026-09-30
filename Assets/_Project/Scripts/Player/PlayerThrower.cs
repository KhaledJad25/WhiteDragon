using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
public class PlayerThrower : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] Projectile rockPrefab;
    [SerializeField] float spawnForward = 0.6f;
    [SerializeField] float spawnRight = 0.2f;
    [SerializeField] float spawnDown = 0.25f;
    [SerializeField] float aimMaxDistance = 100f;

    PlayerStats stats;
    PlayerInventory inventory;
    InputAction fire;
    float nextThrowTime;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        inventory = GetComponent<PlayerInventory>();
        fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
    }

    void OnEnable()
    {
        fire.Enable();
    }

    void OnDisable()
    {
        fire.Disable();
    }

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        if (fire.IsPressed() && Time.time >= nextThrowTime)
        {
            Throw();
            float rate = Mathf.Max(0.1f, stats.Stats.Get(StatType.FireRate));
            nextThrowTime = Time.time + 1f / rate;
        }
    }

    void Throw()
    {
        Transform ct = cam.transform;
        Ray ray = new Ray(ct.position, ct.forward);

        Vector3 aimPoint = ray.origin + ray.direction * aimMaxDistance;
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, aimMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            aimPoint = hit.point;
        }

        Vector3 spawnPos = ct.position + ct.forward * spawnForward + ct.right * spawnRight - ct.up * spawnDown;
        Vector3 baseDir = (aimPoint - spawnPos).normalized;

        ShotRecipe recipe = ShotRecipe.FromStats(stats.Stats);
        if (inventory != null)
        {
            inventory.ModifyRecipe(recipe);
        }
        recipe.ClampToCaps();
        GameEvents.RaiseShotBuilt(recipe);

        int n = recipe.Count;
        for (int i = 0; i < n; i++)
        {
            float offset = 0f;
            if (n > 1)
            {
                offset = Mathf.Lerp(-recipe.SpreadDegrees * 0.5f, recipe.SpreadDegrees * 0.5f, i / (float)(n - 1));
            }

            Vector3 dir = Quaternion.AngleAxis(offset, ct.up) * baseDir;
            Projectile rock = Instantiate(rockPrefab, spawnPos, Quaternion.identity);
            rock.Init(dir, recipe, transform);
        }

        GameFeel.Throw();
    }
}