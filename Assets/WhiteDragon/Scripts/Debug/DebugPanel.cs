using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// F1 debug panel (top-right, IMGUI). Separate from player UI. Expensive lookups happen in Update
    /// or on events, never inside OnGUI.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        public float width = 440f;

        bool open;
        Vector2 scroll, itemScroll;
        string seedText = "";
        string statusMessage = "";

        PlayerStats stats;
        PlayerInventory inventory;
        PlayerHealth health;
        RockThrower thrower;
        StatusEffectDefinition[] statuses;
        SortedDictionary<string, int> poolCounts;

        readonly List<(ItemPedestal pedestal, string path)> pedestals = new List<(ItemPedestal, string)>();
        bool pedestalsDirty = true;
        StatusEffectDefinition pendingStatus;
        int? pendingSeed;

        GUIStyle seedStyle, headerStyle;

        /// <summary>Parses a typed seed. Rejects empty, non-numeric, overflowing and zero input.</summary>
        public static bool TryParseSeed(string text, out int seed)
        {
            seed = 0;
            return !string.IsNullOrWhiteSpace(text) && int.TryParse(text.Trim(), out seed) && seed != 0;
        }

        void Awake()
        {
            statuses = Resources.LoadAll<StatusEffectDefinition>("Statuses")
                .OrderBy(s => s.id ?? "", StringComparer.Ordinal).ToArray();
            poolCounts = ItemCatalog.CountByPool();
            if (GetComponent<StressTest>() == null) gameObject.AddComponent<StressTest>();
            if (GetComponent<FrameRateCheck>() == null) gameObject.AddComponent<FrameRateCheck>();
        }

        void OnEnable()
        {
            RunSession.RunStarted += MarkPedestalsDirty;
            RunSession.ItemPicked += OnItemPicked;
        }

        void OnDisable()
        {
            RunSession.RunStarted -= MarkPedestalsDirty;
            RunSession.ItemPicked -= OnItemPicked;
        }

        void MarkPedestalsDirty() => pedestalsDirty = true;
        void OnItemPicked(ItemDefinition _) => pedestalsDirty = true;

        void Update()
        {
            if (GameInput.ToggleDebug.WasPressedThisFrame())
            {
                open = !open;
                CursorState.SetLocked(!open);
                if (open) pedestalsDirty = true;
            }
            if (!open) return;

            if (stats == null) stats = FindAnyObjectByType<PlayerStats>();
            if (stats != null)
            {
                if (inventory == null) inventory = stats.GetComponent<PlayerInventory>();
                if (health == null) health = stats.GetComponent<PlayerHealth>();
                if (thrower == null) thrower = stats.GetComponent<RockThrower>();
            }
            if (pendingSeed.HasValue)
            {
                RunSession.StartRun(pendingSeed.Value);
                pendingSeed = null;
            }
            if (pedestalsDirty) RefreshPedestals();
            if (pendingStatus != null)
            {
                ApplyToNearest(pendingStatus);
                pendingStatus = null;
            }
        }

        void RefreshPedestals()
        {
            pedestalsDirty = false;
            pedestals.Clear();
            foreach (var p in FindObjectsByType<ItemPedestal>(FindObjectsInactive.Include))
                pedestals.Add((p, p.Path));
            pedestals.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
        }

        void ApplyToNearest(StatusEffectDefinition status)
        {
            Vector3 from = stats != null ? stats.transform.position : Vector3.zero;
            StatusReceiver nearest = null;
            float best = float.MaxValue;
            foreach (var r in FindObjectsByType<StatusReceiver>())
            {
                float d = (r.transform.position - from).sqrMagnitude;
                if (d < best) { best = d; nearest = r; }
            }
            if (nearest == null)
            {
                statusMessage = "No enemy with a StatusReceiver found.";
                return;
            }
            nearest.Apply(status, 1);
            statusMessage = $"Applied {status.displayName} to {nearest.name}.";
        }

        void OnGUI()
        {
            if (!open) return;
            if (seedStyle == null)
            {
                seedStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                headerStyle.normal.textColor = new Color(1f, 0.75f, 0.4f);
            }

            // Top-right; drops below the hearts if the screen is too narrow for both side by side.
            float w = Mathf.Min(width, Screen.width - 20f);
            float x = Screen.width - w - 10f;
            float y = x < HeartsHUD.RightX + 10f ? HeartsHUD.BottomY + 10f : 10f;
            GUILayout.BeginArea(new Rect(x, y, w, Screen.height - y - 10f), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("DEBUG (F1 to close)", headerStyle);
            DrawRun();
            DrawStress();
            DrawStats();
            DrawRecipe();
            DrawItems();
            DrawHealth();
            DrawStatuses();
            DrawPools();
            DrawPedestals();
            DrawCatalog();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawRun()
        {
            GUILayout.Label($"Seed {RunSession.Seed}", seedStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("New run seed")) pendingSeed = 0;
            seedText = GUILayout.TextField(seedText, 11, GUILayout.Width(110f));
            if (GUILayout.Button("Start run with seed") && TryParseSeed(seedText, out int seed))
                pendingSeed = seed;
            GUILayout.EndHorizontal();
            GUILayout.Label($"Rooms cleared: {RunSession.RoomsCleared}   Running: {RunSession.IsRunning}");
        }

        void DrawStats()
        {
            Header("Stats");
            if (stats == null) { GUILayout.Label("(no player)"); return; }
            foreach (StatType s in Enum.GetValues(typeof(StatType)))
                GUILayout.Label($"{s}: {stats.Stats.Get(s):0.##}   (base {stats.Stats.GetBase(s):0.##})");
        }

        void DrawRecipe()
        {
            Header("Last shot recipe");
            var r = thrower != null ? thrower.LastRecipe : null;
            if (r == null) { GUILayout.Label("(no throw yet)"); return; }
            GUILayout.Label($"Damage {r.Damage:0.##}  Speed {r.Speed:0.#}  Range {r.Range:0.#}  Size {r.SizeScale:0.##}");
            GUILayout.Label($"Count {r.Count}  Spread {r.SpreadDegrees:0.#}°  Pierce {r.Pierce}  Type {r.DamageType}");
            GUILayout.Label("Tags: " + (r.Tags.Count == 0 ? "-" : string.Join(", ", r.Tags)));
            GUILayout.Label("Effects: " + (r.Effects.Count == 0 ? "-" : string.Join(", ", r.Effects.Select(e => $"{e.Effect.name} x{e.Stacks}"))));
        }

        void DrawItems()
        {
            Header("Held items");
            if (inventory == null) { GUILayout.Label("(no inventory)"); return; }
            var items = inventory.Loadout.Items;
            GUILayout.Label(items.Count == 0 ? "-" : string.Join(", ", items.Select(i => i.displayName)));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove last")) inventory.RemoveLast();
            if (GUILayout.Button("Clear all")) inventory.Clear();
            GUILayout.EndHorizontal();

            Header("Active synergies");
            var syn = inventory.Loadout.ActiveSynergies;
            GUILayout.Label(syn.Count == 0 ? "-" : string.Join(", ", syn.Select(s => s.displayName)));
        }

        void DrawHealth()
        {
            Header("Health (half hearts)");
            if (health == null) { GUILayout.Label("(no health)"); return; }
            var h = health.State;
            GUILayout.Label($"Red {h.Red}/{h.MaxRed}   Soul {h.Soul}   Dark {h.Dark}   Dead {h.IsDead}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage ½")) health.Damage(1);
            if (GUILayout.Button("Damage 1")) health.Damage(2);
            if (GUILayout.Button("Heal 1")) h.Heal(2);
            if (GUILayout.Button("Kill")) h.Kill();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+Container")) h.AddContainers(1);
            if (GUILayout.Button("+Soul")) h.AddSoul(2);
            if (GUILayout.Button("+Dark")) h.AddDark(2);
            GUILayout.EndHorizontal();
        }

        void DrawStatuses()
        {
            Header("Apply status to nearest enemy");
            if (statuses.Length == 0) GUILayout.Label("(no status assets)");
            GUILayout.BeginHorizontal();
            foreach (var s in statuses)
                if (GUILayout.Button(s.displayName)) pendingStatus = s;
            GUILayout.EndHorizontal();
            if (statusMessage.Length > 0) GUILayout.Label(statusMessage);
        }

        void DrawPools()
        {
            Header("Items per pool");
            foreach (var kv in poolCounts) GUILayout.Label($"{kv.Key}: {kv.Value}");
        }

        void DrawPedestals()
        {
            GUILayout.BeginHorizontal();
            Header("Pedestals");
            if (GUILayout.Button("Refresh", GUILayout.Width(80f))) pedestalsDirty = true;
            GUILayout.EndHorizontal();
            foreach (var (p, path) in pedestals)
            {
                if (p == null) continue;
                string group = string.IsNullOrEmpty(p.groupId) ? "-" : p.groupId;
                string item = p.Item != null ? p.Item.displayName : "(empty)";
                GUILayout.Label($"{path}\n   pool {p.pool}  group {group}  item {item}");
            }
        }

        void DrawCatalog()
        {
            Header($"All items ({ItemCatalog.All.Count})");
            itemScroll = GUILayout.BeginScrollView(itemScroll, GUILayout.Height(240f));
            foreach (var item in ItemCatalog.All)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.displayName}  [{item.rarity}]");
                if (GUILayout.Button("Add", GUILayout.Width(50f)) && inventory != null) inventory.Add(item);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        void DrawStress()
        {
            var stress = GetComponent<StressTest>();
            if (stress == null || !stress.enabled) return;
            Header("Stress test / performance");
            stress.DrawGui();
        }

        void Header(string text) => GUILayout.Label(text, headerStyle);
    }
}
