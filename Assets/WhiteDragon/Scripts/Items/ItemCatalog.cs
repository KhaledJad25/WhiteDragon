using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Every ItemDefinition in Resources/Items, sorted by id so order is deterministic.</summary>
    public static class ItemCatalog
    {
        static ItemDefinition[] all;

        public static IReadOnlyList<ItemDefinition> All => all ??= Load();

        public static ItemDefinition Find(string id) =>
            All.FirstOrDefault(i => string.Equals(i.id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>Item count per pool ID, read from the items themselves.</summary>
        public static SortedDictionary<string, int> CountByPool()
        {
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in All)
            {
                if (item.poolIds == null) continue;
                foreach (var pool in item.poolIds.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.ToLowerInvariant()).Distinct())
                    counts[pool] = counts.TryGetValue(pool, out int n) ? n + 1 : 1;
            }
            return counts;
        }

        public static void Reload() => all = null;

        static ItemDefinition[] Load() =>
            Resources.LoadAll<ItemDefinition>("Items")
                .Where(i => i != null)
                .OrderBy(i => i.id ?? "", StringComparer.Ordinal)
                .ThenBy(i => i.name, StringComparer.Ordinal)
                .ToArray();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all = null;
    }
}
