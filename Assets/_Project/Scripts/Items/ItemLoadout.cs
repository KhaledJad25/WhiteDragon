using System;
using System.Collections.Generic;

public class ItemLoadout
{
    class Entry
    {
        public ItemDefinition item;
        public object source;
    }

    class ActiveEntry
    {
        public SynergyDefinition synergy;
        public object source;
    }

    readonly StatBlock stats;
    readonly List<Entry> entries = new List<Entry>();
    readonly List<SynergyDefinition> catalog = new List<SynergyDefinition>();
    readonly List<ActiveEntry> active = new List<ActiveEntry>();

    public event Action Changed;

    public ItemLoadout(StatBlock stats)
    {
        this.stats = stats;
    }

    public int Count
    {
        get { return entries.Count; }
    }

    public int ActiveSynergyCount
    {
        get { return active.Count; }
    }

    public ItemDefinition GetItem(int index)
    {
        return entries[index].item;
    }

    public SynergyDefinition GetActiveSynergy(int index)
    {
        return active[index].synergy;
    }

    public void SetSynergies(IEnumerable<SynergyDefinition> synergies)
    {
        catalog.Clear();
        catalog.AddRange(synergies);
        catalog.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        RefreshSynergies();
        Raise();
    }

    public void Add(ItemDefinition item)
    {
        Entry e = new Entry();
        e.item = item;
        e.source = new object();
        entries.Add(e);
        ApplyStatEntries(item.statModifiers, e.source);
        RefreshSynergies();
        Raise();
    }

    public void RemoveLast()
    {
        if (entries.Count == 0)
        {
            return;
        }

        Entry e = entries[entries.Count - 1];
        entries.RemoveAt(entries.Count - 1);
        stats.RemoveFrom(e.source);
        RefreshSynergies();
        Raise();
    }

    public void Clear()
    {
        while (entries.Count > 0)
        {
            Entry e = entries[entries.Count - 1];
            entries.RemoveAt(entries.Count - 1);
            stats.RemoveFrom(e.source);
        }
        RefreshSynergies();
        Raise();
    }

    public int CountTag(string tag)
    {
        int n = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            string[] tags = entries[i].item.tags;
            for (int j = 0; j < tags.Length; j++)
            {
                if (tags[j] == tag)
                {
                    n++;
                    break;
                }
            }
        }
        return n;
    }

    public void ModifyRecipe(ShotRecipe recipe)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].item.EditRecipe(recipe);
        }

        for (int i = 0; i < active.Count; i++)
        {
            active[i].synergy.EditRecipe(recipe);
        }
    }

    void RefreshSynergies()
    {
        for (int i = 0; i < active.Count; i++)
        {
            stats.RemoveFrom(active[i].source);
        }
        active.Clear();

        for (int i = 0; i < catalog.Count; i++)
        {
            SynergyDefinition s = catalog[i];
            if (CountTag(s.tag) >= s.requiredCount)
            {
                ActiveEntry a = new ActiveEntry();
                a.synergy = s;
                a.source = new object();
                active.Add(a);
                ApplyStatEntries(s.statModifiers, a.source);
            }
        }
    }

    void ApplyStatEntries(ItemDefinition.StatEntry[] list, object source)
    {
        for (int i = 0; i < list.Length; i++)
        {
            StatModifier mod = new StatModifier();
            mod.Stat = list[i].stat;
            mod.Kind = list[i].kind;
            mod.Value = list[i].value;
            mod.Source = source;
            stats.Add(mod);
        }
    }

    void Raise()
    {
        if (Changed != null)
        {
            Changed();
        }
    }
}