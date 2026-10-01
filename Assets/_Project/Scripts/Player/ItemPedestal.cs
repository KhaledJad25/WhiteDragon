using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Pedestal in the world holding an item. Placed in Scripts/Player (WD.Player).
public class ItemPedestal : MonoBehaviour
{
    static readonly List<ItemPedestal> ActivePedestals = new List<ItemPedestal>();
    static readonly HashSet<string> ShownItemIdsInScene = new HashSet<string>();
    static bool sceneRollCompleted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        ActivePedestals.Clear();
        ShownItemIdsInScene.Clear();
        sceneRollCompleted = false;
    }

    [Header("Pedestal Configuration")]
    [SerializeField] ItemPoolType pool = ItemPoolType.Normal;
    [SerializeField] ItemDefinition fixedItem;
    [SerializeField] float interactRange = 3f;
    [SerializeField] string groupId = "";

    public ItemPoolType Pool => pool;
    public ItemDefinition CurrentItem { get; private set; }
    public string GroupId => groupId;
    public bool IsEmpty => CurrentItem == null;

    GameObject baseVisual;
    GameObject orbVisual;
    GameObject labelObject;
    TextMesh labelTextMesh;
    Transform mainCameraTransform;
    PlayerInventory cachedInventory;
    PlayerStats cachedStats;

    Vector3 initialOrbLocalPos;

    void Awake()
    {
        if (!ActivePedestals.Contains(this))
        {
            ActivePedestals.Add(this);
        }
    }

    void OnEnable()
    {
        RunSession.RunStarted += HandleRunStarted;
    }

    void OnDisable()
    {
        RunSession.RunStarted -= HandleRunStarted;
    }

    void OnDestroy()
    {
        ActivePedestals.Remove(this);
    }

    void HandleRunStarted(int seed)
    {
        // When a new run starts, clear shown items and trigger a fresh deterministic scene roll
        ShownItemIdsInScene.Clear();
        sceneRollCompleted = false;
        RollAllActivePedestals();
    }

    void Start()
    {
        FindPlayerReferences();
        EnsureVisualHierarchy();

        // Perform one coordinated deterministic roll across all pedestals in the scene
        if (!sceneRollCompleted)
        {
            RollAllActivePedestals();
        }
    }

    static void RollAllActivePedestals()
    {
        sceneRollCompleted = true;
        if (ActivePedestals.Count == 0)
        {
            return;
        }

        PlayerInventory inv = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        PlayerStats stats = inv != null ? inv.GetComponent<PlayerStats>() : UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
        float luck = stats != null ? stats.Stats.Get(StatType.Luck) : 0f;

        HashSet<string> exclusions = new HashSet<string>(ShownItemIdsInScene);
        if (inv != null)
        {
            for (int i = 0; i < inv.Count; i++)
            {
                ItemDefinition held = inv.GetItem(i);
                if (held != null && !string.IsNullOrEmpty(held.id))
                {
                    exclusions.Add(held.id);
                }
            }
        }

        // Build descriptors using stable hierarchical key
        List<PedestalDescriptor> descriptors = new List<PedestalDescriptor>();
        Dictionary<string, ItemPedestal> map = new Dictionary<string, ItemPedestal>();

        for (int i = 0; i < ActivePedestals.Count; i++)
        {
            ItemPedestal p = ActivePedestals[i];
            if (p == null) continue;

            p.FindPlayerReferences();
            p.EnsureVisualHierarchy();

            string key = GetHierarchyPathKey(p.transform);
            descriptors.Add(new PedestalDescriptor(key, p.pool, p.fixedItem, p.groupId));
            map[key] = p;
        }

        // Deterministic roll pass
        var assignments = PedestalGroupRoller.AssignItems(descriptors, ItemCatalog.AllItems, RunSession.Rng, luck, exclusions);

        foreach (var kvp in assignments)
        {
            if (map.TryGetValue(kvp.Key, out ItemPedestal pedestal))
            {
                if (kvp.Value != null)
                {
                    pedestal.SetItem(kvp.Value);
                }
                else
                {
                    pedestal.SetEmpty();
                }
            }
        }
    }

    public static string GetHierarchyPathKey(Transform t)
    {
        if (t == null) return "";
        string path = t.name + ":" + t.GetSiblingIndex();
        Transform parent = t.parent;
        while (parent != null)
        {
            path = parent.name + ":" + parent.GetSiblingIndex() + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    void FindPlayerReferences()
    {
        if (cachedInventory == null)
        {
            cachedInventory = UnityEngine.Object.FindAnyObjectByType<PlayerInventory>();
        }
        if (cachedStats == null && cachedInventory != null)
        {
            cachedStats = cachedInventory.GetComponent<PlayerStats>();
        }
        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void EnsureVisualHierarchy()
    {
        // 1. Base pedestal cylinder
        Transform existingBase = transform.Find("BaseVisual");
        if (existingBase != null)
        {
            baseVisual = existingBase.gameObject;
        }
        else
        {
            baseVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseVisual.name = "BaseVisual";
            baseVisual.transform.SetParent(transform, false);
            baseVisual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            baseVisual.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);

            // Pedestal collision only on the base
            Collider col = baseVisual.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
        }

        // 2. Floating orb
        Transform existingOrb = transform.Find("OrbVisual");
        if (existingOrb != null)
        {
            orbVisual = existingOrb.gameObject;
        }
        else
        {
            orbVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbVisual.name = "OrbVisual";
            orbVisual.transform.SetParent(transform, false);
            initialOrbLocalPos = new Vector3(0f, 1.45f, 0f);
            orbVisual.transform.localPosition = initialOrbLocalPos;
            orbVisual.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

            // Orb is purely visual; remove collider so it doesn't block player or throws
            Collider orbCol = orbVisual.GetComponent<Collider>();
            if (orbCol != null)
            {
                Destroy(orbCol);
            }
        }
        initialOrbLocalPos = orbVisual.transform.localPosition;

        // 3. Floating label above orb
        Transform existingLabel = transform.Find("Label");
        if (existingLabel != null)
        {
            labelObject = existingLabel.gameObject;
            labelTextMesh = labelObject.GetComponent<TextMesh>();
        }
        else
        {
            labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 2.05f, 0f);

            labelTextMesh = labelObject.AddComponent<TextMesh>();
            labelTextMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelTextMesh.fontSize = 32;
            labelTextMesh.characterSize = 0.045f;
            labelTextMesh.anchor = TextAnchor.MiddleCenter;
            labelTextMesh.alignment = TextAlignment.Center;
            labelTextMesh.color = Color.white;

            MeshRenderer mr = labelObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = labelTextMesh.font.material;
            }
        }

        if (labelObject != null)
        {
            labelObject.SetActive(false);
        }
    }

    public void SetItem(ItemDefinition item)
    {
        CurrentItem = item;
        if (item == null)
        {
            SetEmpty();
            return;
        }

        ShownItemIdsInScene.Add(item.id);

        if (orbVisual != null)
        {
            orbVisual.SetActive(true);
            Renderer r = orbVisual.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = GetRarityColor(item.rarity);
            }
        }
    }

    public void SetEmpty()
    {
        CurrentItem = null;
        if (orbVisual != null)
        {
            orbVisual.SetActive(false);
        }
        if (labelObject != null)
        {
            labelObject.SetActive(false);
        }
    }

    static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return new Color(0.72f, 0.72f, 0.72f, 1f);     // grey
            case ItemRarity.Uncommon:
                return new Color(0.22f, 0.82f, 0.35f, 1f);     // green
            case ItemRarity.Rare:
                return new Color(0.25f, 0.55f, 0.95f, 1f);     // blue
            case ItemRarity.Legendary:
                return new Color(0.95f, 0.75f, 0.15f, 1f);     // gold
            default:
                return Color.white;
        }
    }

    void Update()
    {
        if (mainCameraTransform == null && Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        if (cachedInventory == null)
        {
            FindPlayerReferences();
        }

        // Cosmetic idle animation for floating orb
        if (orbVisual != null && orbVisual.activeSelf)
        {
            float bob = Mathf.Sin(Time.time * 2.5f) * 0.08f;
            orbVisual.transform.localPosition = initialOrbLocalPos + new Vector3(0f, bob, 0f);
            orbVisual.transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
        }

        if (IsEmpty)
        {
            if (labelObject != null && labelObject.activeSelf)
            {
                labelObject.SetActive(false);
            }
            return;
        }

        if (mainCameraTransform == null)
        {
            return;
        }

        // Distance & Facing check: within interactRange and roughly looking toward pedestal (dot >= 0.7)
        Vector3 toPedestal = (transform.position + Vector3.up * 1.45f) - mainCameraTransform.position;
        float distance = toPedestal.magnitude;

        bool canInteract = false;
        if (distance <= interactRange && distance > 0.001f)
        {
            float lookDot = Vector3.Dot(mainCameraTransform.forward, toPedestal.normalized);
            if (lookDot >= 0.7f)
            {
                canInteract = true;
            }
        }

        if (canInteract)
        {
            if (labelObject != null)
            {
                if (!labelObject.activeSelf)
                {
                    labelObject.SetActive(true);
                }
                labelObject.transform.rotation = mainCameraTransform.rotation;
                if (labelTextMesh != null)
                {
                    // Item name, description, and interaction prompt (no rarity text, no unlock info)
                    labelTextMesh.text = $"{CurrentItem.displayName}\n{CurrentItem.description}\n[E: Take]";
                }
            }

            // Keyboard input check with cursor lock guard
            if (Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TakeItem();
            }
        }
        else
        {
            if (labelObject != null && labelObject.activeSelf)
            {
                labelObject.SetActive(false);
            }
        }
    }

    public void TakeItem()
    {
        if (IsEmpty || CurrentItem == null)
        {
            return;
        }

        ItemDefinition taken = CurrentItem;

        // Add to player inventory and record pick in run session
        if (cachedInventory != null)
        {
            cachedInventory.Add(taken);
        }
        RunSession.PickItem(taken.id);

        // Empty this pedestal
        SetEmpty();

        // If part of a choose-one group, empty all other pedestals in that group
        if (!string.IsNullOrEmpty(groupId))
        {
            for (int i = 0; i < ActivePedestals.Count; i++)
            {
                ItemPedestal other = ActivePedestals[i];
                if (other != null && other != this && other.groupId == groupId)
                {
                    other.SetEmpty();
                }
            }
        }
    }
}

