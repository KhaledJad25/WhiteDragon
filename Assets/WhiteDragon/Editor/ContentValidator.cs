using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Tools/WhiteDragon/Validate Content. Scans every content asset in the project; never modifies anything.</summary>
    public static class ContentValidator
    {
        [MenuItem("Tools/WhiteDragon/Validate Content", priority = 101)]
        public static void Run()
        {
            var issues = Collect();
            foreach (var issue in issues)
            {
                if (issue.Severity == IssueSeverity.Error) Debug.LogError("[Validate] " + issue.Message, issue.Asset);
                else Debug.LogWarning("[Validate] " + issue.Message, issue.Asset);
            }
            int errors = issues.Count(i => i.Severity == IssueSeverity.Error);
            Debug.Log($"[Validate] Done: {errors} error(s), {issues.Count - errors} warning(s).");
            ValidationResultsWindow.ShowResults(issues);
        }

        /// <summary>Builds the content set from the project and open scenes, and returns the issues without logging.</summary>
        public static List<ContentIssue> Collect()
        {
            var set = new ContentSet();
            Add(set, set.Items);
            Add(set, set.Synergies);
            Add(set, set.Effects);
            Add(set, set.Statuses);
            Add(set, set.Enemies);
            Add(set, set.Characters);
            Add(set, set.Brains);
            Add(set, set.Variants);
            Add(set, set.Behaviors);
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null && prefab.GetComponent<Enemy>() != null) set.EnemyPrefabs.Add(prefab);
            }
            foreach (var p in Object.FindObjectsByType<ItemPedestal>(FindObjectsInactive.Include))
                set.PedestalPools.Add((p.pool, p));
            set.SceneEnemies.AddRange(Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include));
            return ContentRules.Validate(set);
        }

        static void Add<T>(ContentSet set, List<T> list) where T : ScriptableObject
        {
            foreach (var asset in ContentCreator.FindAll<T>())
            {
                list.Add(asset);
                set.Paths[asset] = AssetDatabase.GetAssetPath(asset);
            }
        }
    }
}
