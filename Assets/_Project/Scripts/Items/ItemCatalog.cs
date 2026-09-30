using System;
using System.Collections.Generic;
using UnityEngine;

public static class ItemCatalog
{
    static ItemDefinition[] allItems;

    public static IReadOnlyList<ItemDefinition> AllItems
    {
        get
        {
            if (allItems == null || allItems.Length == 0)
            {
                Load();
            }
            return allItems;
        }
    }

    public static void Load()
    {
        allItems = Resources.LoadAll<ItemDefinition>("Items");
        if (allItems != null)
        {
            Array.Sort(allItems, (a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));
        }
        else
        {
            allItems = Array.Empty<ItemDefinition>();
        }
    }

    public static void ClearCache()
    {
        allItems = null;
    }
}
