using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Every EnemyDefinition and EnemyVariant under Resources/Enemies (any subfolder), loaded once, sorted by id.</summary>
    public static class EnemyCatalog
    {
        static EnemyDefinition[] definitions;
        static EnemyVariant[] variants;
        static Dictionary<EnemyDefinition, GameObject> prefabs;

        /// <summary>The prefab under Resources/Enemies whose Enemy points at this definition, or null.</summary>
        public static GameObject PrefabFor(EnemyDefinition definition)
        {
            if (definition == null) return null;
            if (prefabs == null)
            {
                prefabs = new Dictionary<EnemyDefinition, GameObject>();
                foreach (var go in Resources.LoadAll<GameObject>("Enemies"))
                {
                    var e = go.GetComponent<Enemy>();
                    if (e != null && e.definition != null && !prefabs.ContainsKey(e.definition)) prefabs[e.definition] = go;
                }
            }
            return prefabs.TryGetValue(definition, out var p) ? p : null;
        }

        public static IReadOnlyList<EnemyDefinition> All => definitions ??= Load<EnemyDefinition>(d => d.id);
        public static IReadOnlyList<EnemyVariant> Variants => variants ??= Load<EnemyVariant>(v => v.id);

        public static EnemyDefinition Find(string id) =>
            All.FirstOrDefault(d => string.Equals(d.id, id, StringComparison.OrdinalIgnoreCase));

        public static EnemyVariant FindVariant(string id) =>
            Variants.FirstOrDefault(v => string.Equals(v.id, id, StringComparison.OrdinalIgnoreCase));

        public static void Reload()
        {
            definitions = null;
            variants = null;
            prefabs = null;
        }

        static T[] Load<T>(Func<T, string> id) where T : ScriptableObject =>
            Resources.LoadAll<T>("Enemies")
                .Where(a => a != null)
                .OrderBy(a => id(a) ?? "", StringComparer.Ordinal)
                .ThenBy(a => a.name, StringComparer.Ordinal)
                .ToArray();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reload();
    }
}
