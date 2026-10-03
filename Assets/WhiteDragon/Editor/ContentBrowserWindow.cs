using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WhiteDragon
{
    /// <summary>Browse, search and filter all content. Click a row to select and ping the asset.</summary>
    public class ContentBrowserWindow : EditorWindow
    {
        enum Tab { Items, Synergies, Effects, Statuses, Enemies, Brains, Variants, Behaviors, Characters }

        static readonly string[] TabNames = Enum.GetNames(typeof(Tab));
        static readonly string[] Columns = { "Name", "Id", "Rarity", "Tags", "Pools" };
        /// <summary>Column headers where a tab uses the columns differently (else Columns).</summary>
        static readonly Dictionary<Tab, string[]> TabColumns = new Dictionary<Tab, string[]>
        {
            { Tab.Enemies, new[] { "Name", "Id", "Family", "Tags", "Brain" } },
            { Tab.Brains, new[] { "Name", "Kind", "Family", "States", "Used by" } },
            { Tab.Variants, new[] { "Name", "Id", "Base enemy", "Overrides", "Brain" } },
            { Tab.Behaviors, new[] { "Name", "Type", "Category", "Family", "Used by" } },
        };
        static readonly float[] ColumnWidths = { 0.22f, 0.2f, 0.1f, 0.24f, 0.24f };
        const string All = "(all)";

        class Row
        {
            public Object Asset;
            public string Name, Id, Rarity, Tags, Pools, Summary;
            public string[] TagList = new string[0];
            public string[] PoolList = new string[0];
            public string Search;
        }

        Tab tab;
        string search = "";
        int tagFilter, poolFilter, rarityFilter;
        Vector2 scroll;
        readonly Dictionary<Tab, List<Row>> rows = new Dictionary<Tab, List<Row>>();
        string[] tagOptions = { All }, poolOptions = { All }, rarityOptions = { All };
        GUIStyle summaryStyle;

        [MenuItem("Tools/WhiteDragon/Content Browser", priority = 100)]
        public static void Open() => GetWindow<ContentBrowserWindow>("Content Browser");

        void OnEnable()
        {
            EditorApplication.projectChanged += Reload;
            Reload();
        }

        void OnDisable() => EditorApplication.projectChanged -= Reload;

        void Reload()
        {
            var items = ContentCreator.FindAll<ItemDefinition>().ToList();
            rows[Tab.Items] = items.Select(ItemRow).ToList();
            rows[Tab.Synergies] = ContentCreator.FindAll<SynergyDefinition>().Select(SynergyRow).ToList();
            rows[Tab.Effects] = ContentCreator.FindAll<ShotEffect>().Select(EffectRow).ToList();
            rows[Tab.Statuses] = ContentCreator.FindAll<StatusEffectDefinition>().Select(StatusRow).ToList();
            var enemies = ContentCreator.FindAll<EnemyDefinition>().ToList();
            var variants = ContentCreator.FindAll<EnemyVariant>().ToList();
            var brains = ContentCreator.FindAll<EnemyBrainDefinition>().ToList();
            rows[Tab.Enemies] = enemies.Select(EnemyRow).ToList();
            rows[Tab.Brains] = brains.Select(b => BrainRow(b, enemies, variants)).ToList();
            rows[Tab.Variants] = variants.Select(VariantRow).ToList();
            rows[Tab.Behaviors] = ContentCreator.FindAll<EnemyBehavior>().Select(b => BehaviorRow(b, brains)).ToList();
            rows[Tab.Characters] = ContentCreator.FindAll<CharacterDefinition>().Select(c => Finish(new Row
            {
                Asset = c, Name = Label(c.displayName, c), Id = c.id, Summary = c.Summary(),
            })).ToList();
            foreach (var list in rows.Values) list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            string oldTag = tagOptions[Mathf.Clamp(tagFilter, 0, tagOptions.Length - 1)];
            string oldPool = poolOptions[Mathf.Clamp(poolFilter, 0, poolOptions.Length - 1)];
            string oldRarity = rarityOptions[Mathf.Clamp(rarityFilter, 0, rarityOptions.Length - 1)];
            tagOptions = Options(items.SelectMany(i => i.tags ?? new string[0]));
            poolOptions = Options(items.SelectMany(i => i.poolIds ?? new string[0]));
            rarityOptions = new[] { All }.Concat(items.Select(i => i.rarity).Distinct().OrderBy(r => (int)r).Select(r => r.ToString())).ToArray();
            tagFilter = Math.Max(0, Array.IndexOf(tagOptions, oldTag));
            poolFilter = Math.Max(0, Array.IndexOf(poolOptions, oldPool));
            rarityFilter = Math.Max(0, Array.IndexOf(rarityOptions, oldRarity));
            Repaint();
        }

        static string[] Options(IEnumerable<string> values) =>
            new[] { All }.Concat(values.Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)).ToArray();

        // ---------- Rows ----------

        static Row ItemRow(ItemDefinition i) => Finish(new Row
        {
            Asset = i, Name = Label(i.displayName, i), Id = i.id, Rarity = i.rarity.ToString(),
            TagList = i.tags ?? new string[0], PoolList = i.poolIds ?? new string[0], Summary = i.Summary(),
        });

        static Row SynergyRow(SynergyDefinition s) => Finish(new Row
        {
            Asset = s, Name = Label(s.displayName, s), Id = s.id,
            TagList = string.IsNullOrEmpty(s.tag) ? new string[0] : new[] { s.tag }, Summary = s.Summary(),
        });

        static Row EffectRow(ShotEffect e)
        {
            string summary = ObjectNames.NicifyVariableName(e.GetType().Name);
            if (e is ApplyStatusEffect apply)
                summary += apply.status != null ? $" -> {Label(apply.status.displayName, apply.status)}" : " -> (no status!)";
            if (!string.IsNullOrEmpty(e.description)) summary += ": " + e.description;
            return Finish(new Row { Asset = e, Name = e.name, Summary = summary });
        }

        static Row StatusRow(StatusEffectDefinition s)
        {
            string summary = $"{s.duration:0.##}s";
            if (s.damagePerSecond > 0f) summary += $", {s.damagePerSecond:0.##} dmg/s per stack every {s.tickInterval:0.##}s";
            if (!Mathf.Approximately(s.speedMultiplier, 1f)) summary += $", speed x{s.speedMultiplier:0.##}";
            summary += $", {s.stacking} (max {s.maxStacks})";
            return Finish(new Row { Asset = s, Name = Label(s.displayName, s), Id = s.id, Summary = summary });
        }

        static Row EnemyRow(EnemyDefinition e) => Finish(new Row
        {
            Asset = e, Name = Label(e.displayName, e), Id = e.id, Rarity = Dash(e.family),
            TagList = e.tags ?? new string[0], PoolList = e.brain != null ? new[] { e.brain.name } : new[] { "(none: built-in chase)" },
            Summary = $"{e.movement}, {e.maxHealth:0.##} hp, speed {e.moveSpeed:0.##}, contact {e.contactDamage} half heart(s), threat {e.threatCost}" + (e.isBoss ? ", boss" : ""),
        });

        static Row BrainRow(EnemyBrainDefinition b, List<EnemyDefinition> enemies, List<EnemyVariant> variants)
        {
            var sm = b as StateMachineBrain;
            var users = enemies.Where(e => e.brain == b).Select(e => e.name)
                .Concat(variants.Where(v => v.brain == b).Select(v => v.name + " (variant)")).ToArray();
            return Finish(new Row
            {
                Asset = b, Name = b.name, Id = sm != null ? "State machine" : "Code: " + b.GetType().Name, Rarity = FolderName(b),
                TagList = sm != null ? new[] { sm.states.Count.ToString() } : new string[0], PoolList = users,
                Summary = sm != null ? string.Join("  ", sm.states.Select(StateSummary)) : "Code brain (AI written in C#).",
            });
        }

        static string StateSummary(BrainState s)
        {
            string behaviors = string.Join("+", s.behaviors.Select(x => x != null ? x.name : "(empty!)"));
            string exits = string.Join(", ", s.transitions.Select(t => $"{t.condition}{(NeedsValue(t.condition) ? " " + t.value.ToString("0.##") : "")}->{t.target}"));
            return $"[{s.name}: {(behaviors.Length > 0 ? behaviors : "-")}{(exits.Length > 0 ? " | " + exits : "")}]";
        }

        static bool NeedsValue(TransitionCondition c) =>
            c == TransitionCondition.TimeInState || c == TransitionCondition.DistanceToPlayerBelow
            || c == TransitionCondition.DistanceToPlayerAbove || c == TransitionCondition.HealthBelowPercent;

        static Row VariantRow(EnemyVariant v)
        {
            var overrides = new List<string>();
            if (v.overrideTint) overrides.Add("tint");
            if (v.visualPrefab != null) overrides.Add("visual");
            if (v.brain != null) overrides.Add("brain");
            return Finish(new Row
            {
                Asset = v, Name = v.name, Id = v.id, Rarity = v.baseEnemy != null ? v.baseEnemy.name : "(any)",
                TagList = overrides.ToArray(), PoolList = v.brain != null ? new[] { v.brain.name } : new string[0],
                Summary = $"hp x{v.healthMultiplier:0.##}, speed x{v.speedMultiplier:0.##}, damage x{v.damageMultiplier:0.##}, size x{v.scaleMultiplier:0.##}, weight {v.weight:0.##}",
            });
        }

        static Row BehaviorRow(EnemyBehavior b, List<EnemyBrainDefinition> brains)
        {
            var info = EnemyContentCreator.Info(b.GetType());
            var users = brains.OfType<StateMachineBrain>()
                .Where(sm => sm.states.Any(s => s.behaviors.Contains(b))).Select(sm => sm.name).ToArray();
            return Finish(new Row
            {
                Asset = b, Name = b.name, Id = b.GetType().Name, Rarity = info?.Category ?? "-",
                TagList = new[] { FolderName(b) }, PoolList = users.Length > 0 ? users : new[] { "(unused)" },
                Summary = info?.Description ?? "(no [EnemyBehaviorInfo] description)",
            });
        }

        static string FolderName(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? "-" : System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
        }

        static string Dash(string s) => string.IsNullOrEmpty(s) ? "-" : s;

        static string Label(string displayName, Object asset) => string.IsNullOrEmpty(displayName) ? asset.name : displayName;

        static Row Finish(Row r)
        {
            r.Id = string.IsNullOrEmpty(r.Id) ? "-" : r.Id;
            r.Rarity ??= "-";
            r.Tags = r.TagList.Length == 0 ? "-" : string.Join(", ", r.TagList);
            r.Pools = r.PoolList.Length == 0 ? "-" : string.Join(", ", r.PoolList);
            // Search covers name, id, tags, and the third column (rarity, family or category).
            r.Search = $"{r.Name} {r.Asset.name} {r.Id} {r.Tags} {r.Rarity}".ToLowerInvariant();
            return r;
        }

        // ---------- GUI ----------

        void OnGUI()
        {
            if (summaryStyle == null)
                summaryStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };

            tab = (Tab)GUILayout.Toolbar((int)tab, TabNames);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50f))) CreateNew();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f))) Reload();
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(60f))) ContentValidator.Run();
            }

            if (tab == Tab.Items)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    tagFilter = EditorGUILayout.Popup("Tag", tagFilter, tagOptions);
                    poolFilter = EditorGUILayout.Popup("Pool", poolFilter, poolOptions);
                    rarityFilter = EditorGUILayout.Popup("Rarity", rarityFilter, rarityOptions);
                }
            }

            var all = rows.TryGetValue(tab, out var list) ? list : new List<Row>();
            var shown = all.Where(Matches).ToList();
            EditorGUILayout.LabelField($"{shown.Count} shown of {all.Count}", EditorStyles.miniLabel);

            DrawColumns(EditorGUILayout.GetControlRect(), TabColumns.TryGetValue(tab, out var headers) ? headers : Columns, EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var row in shown) DrawRow(row);
            EditorGUILayout.EndScrollView();
        }

        bool Matches(Row r)
        {
            if (r.Asset == null) return false;
            string q = search.Trim().ToLowerInvariant();
            if (q.Length > 0 && !r.Search.Contains(q)) return false;
            if (tab != Tab.Items) return true;
            if (tagFilter > 0 && !r.TagList.Contains(tagOptions[tagFilter], StringComparer.OrdinalIgnoreCase)) return false;
            if (poolFilter > 0 && !r.PoolList.Contains(poolOptions[poolFilter], StringComparer.OrdinalIgnoreCase)) return false;
            if (rarityFilter > 0 && r.Rarity != rarityOptions[rarityFilter]) return false;
            return true;
        }

        void DrawRow(Row row)
        {
            var rect = EditorGUILayout.GetControlRect(false, 34f);
            if (Selection.activeObject == row.Asset)
                EditorGUI.DrawRect(rect, new Color(0.24f, 0.37f, 0.59f, 0.5f));
            DrawColumns(new Rect(rect.x, rect.y, rect.width, 18f),
                new[] { row.Name, row.Id, row.Rarity, row.Tags, row.Pools }, EditorStyles.label);
            EditorGUI.LabelField(new Rect(rect.x + 4f, rect.y + 17f, rect.width - 4f, 16f), row.Summary, summaryStyle);

            var e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                Selection.activeObject = row.Asset;
                EditorGUIUtility.PingObject(row.Asset);
                e.Use();
            }
        }

        static void DrawColumns(Rect rect, string[] values, GUIStyle style)
        {
            float x = rect.x;
            for (int i = 0; i < values.Length; i++)
            {
                float w = rect.width * ColumnWidths[i];
                EditorGUI.LabelField(new Rect(x, rect.y, w - 4f, rect.height), values[i], style);
                x += w;
            }
        }

        void CreateNew()
        {
            switch (tab)
            {
                case Tab.Items: ContentCreator.NewItem(); break;
                case Tab.Synergies: ContentCreator.NewSynergy(); break;
                case Tab.Effects: ContentCreator.NewShotEffect(); break;
                case Tab.Statuses: ContentCreator.NewStatus(); break;
                case Tab.Enemies: EnemyContentCreator.NewEnemy(); break;
                case Tab.Brains: EnemyContentCreator.NewBrain(); break;
                case Tab.Variants: EnemyContentCreator.NewVariant(); break;
                case Tab.Behaviors: EnemyContentCreator.NewBehaviorAsset(); break;
                case Tab.Characters: ContentCreator.NewCharacter(); break;
            }
        }
    }
}
