using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Shows one rolled item. Press E while close and looking at it to take it. Taking one empties
    /// every pedestal with the same groupId. All pedestals roll together, sorted by hierarchy path,
    /// whenever a run starts.
    /// </summary>
    public class ItemPedestal : MonoBehaviour
    {
        public string pool = "normal";
        [Tooltip("Empty means standalone. Taking one item empties every pedestal in the group.")]
        public string groupId = "";
        public float interactRange = 3f;
        [Tooltip("Minimum dot product between view direction and the item to count as looking at it.")]
        public float lookDot = 0.7f;

        static readonly List<ItemPedestal> enabledPedestals = new List<ItemPedestal>();

        ItemDefinition item;
        Transform orb;
        Renderer orbRenderer;
        TextMesh label;
        PlayerInventory player;
        Camera playerCamera;

        public ItemDefinition Item => item;
        public string Path => HierarchyPath(transform);

        // ---------- Rolling ----------

        /// <summary>Rolls every pedestal in the scene (including inactive ones) in one deterministic pass.</summary>
        public static void RollAll()
        {
            if (!Application.isPlaying || RunSession.Rng == null) return;
            var inventory = FindAnyObjectByType<PlayerInventory>();
            var stats = FindAnyObjectByType<PlayerStats>();
            float luck = stats != null ? stats.Stats.Get(StatType.Luck) : 0f;
            var held = inventory != null ? inventory.Loadout.Items.Select(i => i.id) : Enumerable.Empty<string>();

            var pedestals = FindObjectsByType<ItemPedestal>(FindObjectsInactive.Include);
            foreach (var (pedestal, rolled) in AssignItems(pedestals, new ItemPoolRoller(ItemCatalog.All), RunSession.Rng, luck, held))
                pedestal.SetItem(rolled);
        }

        /// <summary>Pure assignment: sorts by hierarchy path so input order never matters.</summary>
        public static List<(ItemPedestal pedestal, ItemDefinition item)> AssignItems(
            IEnumerable<ItemPedestal> pedestals, ItemPoolRoller roller, RunRandom rng, float luck, IEnumerable<string> excludedIds)
        {
            var excluded = new HashSet<string>(excludedIds ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var result = new List<(ItemPedestal, ItemDefinition)>();
            foreach (var p in pedestals.Where(p => p != null).OrderBy(p => p.Path, StringComparer.Ordinal))
            {
                var rolled = roller.Roll(p.pool, luck, rng, excluded);
                if (rolled != null && !string.IsNullOrEmpty(rolled.id)) excluded.Add(rolled.id);
                result.Add((p, rolled));
            }
            return result;
        }

        public static string HierarchyPath(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent)
                parts.Add($"{t.name}[{t.GetSiblingIndex()}]");
            parts.Reverse();
            return string.Join("/", parts);
        }

        // ---------- Item ----------

        public void SetItem(ItemDefinition newItem)
        {
            item = newItem;
            EnsureVisuals();
            orb.gameObject.SetActive(item != null);
            if (item != null) orbRenderer.sharedMaterial = PlaceholderMaterials.Lit(RarityColor(item.rarity));
            label.text = item == null ? "" : $"{item.displayName}\n{item.description}\nE: take";
            label.gameObject.SetActive(false);
        }

        public void Take()
        {
            if (item == null || player == null) return;
            var taken = item;
            player.Add(taken);
            RunSession.NotifyItemPicked(taken);
            GameFeel.Burst(orb.position, RarityColor(taken.rarity), 20, 3f);
            SetItem(null);

            if (string.IsNullOrEmpty(groupId)) return;
            foreach (var other in FindObjectsByType<ItemPedestal>(FindObjectsInactive.Include))
                if (other != this && string.Equals(other.groupId, groupId, StringComparison.OrdinalIgnoreCase))
                    other.SetItem(null);
        }

        public static Color RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return new Color(0.25f, 0.75f, 0.3f);
                case ItemRarity.Rare: return new Color(0.25f, 0.45f, 1f);
                case ItemRarity.Legendary: return new Color(1f, 0.78f, 0.2f);
                default: return new Color(0.6f, 0.6f, 0.6f);
            }
        }

        // ---------- Lifecycle ----------

        void Awake() => EnsureVisuals();
        void OnEnable() => enabledPedestals.Add(this);
        void OnDisable() => enabledPedestals.Remove(this);

        void Update()
        {
            if (orb.gameObject.activeSelf)
            {
                orb.localPosition = new Vector3(0f, 1.4f + Mathf.Sin(Time.time * 2f) * 0.08f, 0f);
                orb.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
            }

            if (player == null) player = FindAnyObjectByType<PlayerInventory>();
            if (player != null && playerCamera == null) playerCamera = player.GetComponentInChildren<Camera>();

            bool focused = IsFocused();
            label.gameObject.SetActive(focused);
            if (focused)
                label.transform.rotation = playerCamera.transform.rotation;

            if (focused && CursorState.Locked && GameInput.Interact.WasPressedThisFrame())
                Take();
        }

        /// <summary>-1 when not interactable, otherwise how directly the player looks at the item.</summary>
        float FocusScore()
        {
            if (item == null || playerCamera == null) return -1f;
            Vector3 to = orb.position - playerCamera.transform.position;
            if (to.magnitude > interactRange) return -1f;
            float dot = Vector3.Dot(playerCamera.transform.forward, to.normalized);
            return dot >= lookDot ? dot : -1f;
        }

        bool IsFocused()
        {
            float mine = FocusScore();
            if (mine < 0f) return false;
            foreach (var other in enabledPedestals)
                if (other != this && other.FocusScore() > mine) return false;
            return true;
        }

        void EnsureVisuals()
        {
            if (orb != null) return;

            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Base";
            pillar.transform.SetParent(transform, false);
            pillar.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            pillar.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
            pillar.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.Lit(new Color(0.32f, 0.31f, 0.3f));

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Item";
            DestroyCollider(sphere);
            orb = sphere.transform;
            orb.SetParent(transform, false);
            orb.localPosition = new Vector3(0f, 1.4f, 0f);
            orb.localScale = Vector3.one * 0.45f;
            orbRenderer = sphere.GetComponent<Renderer>();
            sphere.SetActive(false);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label = labelGo.AddComponent<TextMesh>();
            label.font = font;
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.fontSize = 48;
            label.characterSize = 0.035f;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.92f, 0.88f, 0.8f);
            labelGo.SetActive(false);
        }

        static void DestroyCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => enabledPedestals.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookRunEvents() => RunSession.RunStarted += RollAll;
    }
}
