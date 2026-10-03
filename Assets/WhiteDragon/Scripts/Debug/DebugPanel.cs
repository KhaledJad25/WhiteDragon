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

        GUIStyle seedStyle, headerStyle, enemyLabelStyle;

        // Enemies: spawn list (definitions and variants), kill all, freeze, state labels.
        readonly List<(EnemyDefinition definition, EnemyVariant variant, string label)> spawnOptions =
            new List<(EnemyDefinition, EnemyVariant, string)>();
        string enemySearch = "";
        string enemyMessage = "";
        Vector2 enemyScroll;
        bool showEnemyLabels;
        int pendingSpawn = -1;
        bool pendingKillAll;

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
            foreach (var d in EnemyCatalog.All)
                spawnOptions.Add((d, null, d.displayName));
            foreach (var v in EnemyCatalog.Variants)
                if (v.baseEnemy != null) spawnOptions.Add((v.baseEnemy, v, $"{v.baseEnemy.displayName} {v.displaySuffix}"));
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
            if (pendingSpawn >= 0)
            {
                SpawnAtCrosshair(spawnOptions[pendingSpawn]);
                pendingSpawn = -1;
            }
            if (pendingKillAll)
            {
                pendingKillAll = false;
                int killed = 0;
                foreach (var e in FindObjectsByType<Enemy>())
                    if (!e.IsDead) { e.Kill(); killed++; }
                enemyMessage = $"Killed {killed} enem{(killed == 1 ? "y" : "ies")}.";
            }
        }

        /// <summary>Spawns where the crosshair points (on the floor, or hovering for flyers). Debug key namespace, no room.</summary>
        void SpawnAtCrosshair((EnemyDefinition definition, EnemyVariant variant, string label) option)
        {
            var cam = Camera.main;
            if (cam == null) { enemyMessage = "No camera."; return; }
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            Vector3 point = Physics.Raycast(ray, out var hit, 40f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point + hit.normal * 0.6f
                : ray.GetPoint(8f);
            if (Physics.Raycast(point + Vector3.up, Vector3.down, out var floor, 50f, ~0, QueryTriggerInteraction.Ignore))
                point = floor.point;
            if (option.definition.movement == MovementMode.Flying) point += Vector3.up * 1.5f;
            var enemy = EnemySpawner.Spawn(option.definition, option.variant, point, EnemySpawner.NextDebugKey());
            enemyMessage = $"Spawned {option.label} ({enemy.SpawnKey}).";
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
            if (seedStyle == null)
            {
                seedStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                headerStyle.normal.textColor = new Color(1f, 0.75f, 0.4f);
                enemyLabelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
                enemyLabelStyle.normal.textColor = new Color(1f, 0.9f, 0.5f);
            }
            if (showEnemyLabels) DrawEnemyLabels();
            if (!open) return;

            // Top-right; drops below the hearts if the screen is too narrow for both side by side.
            float w = Mathf.Min(width, Screen.width - 20f);
            float x = Screen.width - w - 10f;
            float y = x < HeartsHUD.RightX + 10f ? HeartsHUD.BottomY + 10f : 10f;
            GUILayout.BeginArea(new Rect(x, y, w, Screen.height - y - 10f), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("DEBUG (F1 to close)", headerStyle);
            DrawRun();
            DrawEnemies();
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

        void DrawEnemies()
        {
            Header("Enemies");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill all")) pendingKillAll = true;
            Enemy.FreezeAI = GUILayout.Toggle(Enemy.FreezeAI, "Freeze AI");
            showEnemyLabels = GUILayout.Toggle(showEnemyLabels, "State labels");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Spawn at crosshair:", GUILayout.Width(130f));
            enemySearch = GUILayout.TextField(enemySearch);
            GUILayout.EndHorizontal();
            enemyScroll = GUILayout.BeginScrollView(enemyScroll, GUILayout.Height(110f));
            string q = enemySearch.Trim();
            for (int i = 0; i < spawnOptions.Count; i++)
            {
                if (q.Length > 0 && spawnOptions[i].label.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Button(spawnOptions[i].label)) pendingSpawn = i;
            }
            GUILayout.EndScrollView();
            if (enemyMessage.Length > 0) GUILayout.Label(enemyMessage);
        }

        /// <summary>Each awake enemy's brain state above its head (debug).</summary>
        void DrawEnemyLabels()
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var e in Enemy.Live)
            {
                if (e == null || e.IsDead) continue;
                var cc = e.GetComponent<CharacterController>();
                Vector3 top = e.transform.position + Vector3.up * ((cc != null ? cc.height * e.transform.lossyScale.y : 2f) + 0.3f);
                Vector3 screen = cam.WorldToScreenPoint(top);
                if (screen.z <= 0f) continue;
                GUI.Label(new Rect(screen.x - 80f, Screen.height - screen.y - 10f, 160f, 20f), $"{e.name}: {e.DebugLabel}", enemyLabelStyle);
            }
        }

        void Header(string text) => GUILayout.Label(text, headerStyle);
    }
}
