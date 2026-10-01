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
        enum Tab { Items, Synergies, Effects, Statuses, Enemies, Characters }

        static readonly string[] TabNames = Enum.GetNames(typeof(Tab));
        static readonly string[] Columns = { "Name", "Id", "Rarity", "Tags", "Pools" };
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
            rows[Tab.Enemies] = ContentCreator.FindAll<EnemyDefinition>().Select(EnemyRow).ToList();
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
            Asset = e, Name = Label(e.displayName, e), Id = e.id,
            Summary = $"{e.maxHealth:0.##} hp, speed {e.moveSpeed:0.##}, contact {e.contactDamage} half heart(s)",
        });

        static string Label(string displayName, Object asset) => string.IsNullOrEmpty(displayName) ? asset.name : displayName;

        static Row Finish(Row r)
        {
            r.Id = string.IsNullOrEmpty(r.Id) ? "-" : r.Id;
            r.Rarity ??= "-";
            r.Tags = r.TagList.Length == 0 ? "-" : string.Join(", ", r.TagList);
            r.Pools = r.PoolList.Length == 0 ? "-" : string.Join(", ", r.PoolList);
            r.Search = $"{r.Name} {r.Asset.name} {r.Id} {r.Tags}".ToLowerInvariant();
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

            DrawColumns(EditorGUILayout.GetControlRect(), Columns, EditorStyles.boldLabel);
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
                case Tab.Enemies: ContentCreator.NewEnemy(); break;
                case Tab.Characters: ContentCreator.NewCharacter(); break;
            }
        }
    }
}
