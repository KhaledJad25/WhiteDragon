using System;
using System.Collections.Generic;

public class PedestalDescriptor
{
    public string Key { get; }
    public ItemPoolType Pool { get; }
    public ItemDefinition FixedItem { get; }
    public string GroupId { get; }

    public PedestalDescriptor(string key, ItemPoolType pool, ItemDefinition fixedItem = null, string groupId = "")
    {
        Key = key ?? "";
        Pool = pool;
        FixedItem = fixedItem;
        GroupId = groupId ?? "";
    }
}

public static class PedestalGroupRoller
{
    public static Dictionary<string, ItemDefinition> AssignItems(
        IEnumerable<PedestalDescriptor> pedestals,
        IReadOnlyList<ItemDefinition> catalog,
        RunRandom rng,
        float luck,
        ISet<string> initialExclusions = null,
        Func<string, bool> unlockPredicate = null)
    {
        var result = new Dictionary<string, ItemDefinition>();
        if (pedestals == null)
        {
            return result;
        }

        // Sort by stable deterministic key
        var sorted = new List<PedestalDescriptor>(pedestals);
        sorted.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));

        var shown = initialExclusions != null
            ? new HashSet<string>(initialExclusions)
            : new HashSet<string>();

        for (int i = 0; i < sorted.Count; i++)
        {
            var p = sorted[i];
            if (p.FixedItem != null)
            {
                result[p.Key] = p.FixedItem;
                if (!string.IsNullOrEmpty(p.FixedItem.id))
                {
                    shown.Add(p.FixedItem.id);
                }
                continue;
            }

            ItemDefinition rolled = ItemPoolRoller.Roll(catalog, p.Pool, rng, luck, shown, unlockPredicate);
            result[p.Key] = rolled;
            if (rolled != null && !string.IsNullOrEmpty(rolled.id))
            {
                shown.Add(rolled.id);
            }
        }

        return result;
    }
}
