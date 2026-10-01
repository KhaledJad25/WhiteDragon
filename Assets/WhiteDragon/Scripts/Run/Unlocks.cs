using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Hidden meta-progression hook. In-memory for now. Unlocks only widen what can appear.</summary>
    public static class Unlocks
    {
        static readonly HashSet<string> granted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Empty ID is always unlocked.</summary>
        public static bool Has(string id) => string.IsNullOrEmpty(id) || granted.Contains(id);

        public static void Grant(string id)
        {
            if (!string.IsNullOrEmpty(id)) granted.Add(id);
        }

        public static void Clear() => granted.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => granted.Clear();
    }
}
