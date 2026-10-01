using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiteDragon
{
    /// <summary>
    /// Held items in pickup order plus the synergies they activate. Applies and removes stat
    /// modifiers on a StatBlock. Plain C# so it can be unit tested.
    /// </summary>
    public class ItemLoadout
    {
        readonly StatBlock stats;
        readonly List<ItemDefinition> items = new List<ItemDefinition>();
        readonly List<object> sources = new List<object>();
        readonly List<SynergyDefinition> synergies;
        readonly List<SynergyDefinition> activeSynergies = new List<SynergyDefinition>();

        public event Action Changed;

        public IReadOnlyList<ItemDefinition> Items => items;
        public IReadOnlyList<SynergyDefinition> ActiveSynergies => activeSynergies;
        public IReadOnlyList<SynergyDefinition> AllSynergies => synergies;
        public StatBlock Stats => stats;

        public ItemLoadout(StatBlock stats, IEnumerable<SynergyDefinition> synergies = null)
        {
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
            this.synergies = synergies == null
                ? new List<SynergyDefinition>()
                : synergies.Where(s => s != null).Distinct().ToList();
        }

        public bool Add(ItemDefinition item)
        {
            if (item == null) return false;
            var source = new object();
            items.Add(item);
            sources.Add(source);
            if (item.statModifiers != null)
                foreach (var m in item.statModifiers) stats.AddModifier(m, source);
            OnChanged();
            return true;
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= items.Count) return false;
            stats.RemoveModifiersFromSource(sources[index]);
            items.RemoveAt(index);
            sources.RemoveAt(index);
            OnChanged();
            return true;
        }

        public bool RemoveLast() => RemoveAt(items.Count - 1);

        public void Clear()
        {
            foreach (var s in sources) stats.RemoveModifiersFromSource(s);
            items.Clear();
            sources.Clear();
            OnChanged();
        }

        public int CountWithTag(string tag)
        {
            int n = 0;
            foreach (var item in items)
                if (item.HasTag(tag)) n++;
            return n;
        }

        void OnChanged()
        {
            RefreshSynergies();
            Changed?.Invoke();
        }

        void RefreshSynergies()
        {
            foreach (var s in synergies)
            {
                bool shouldBeActive = !string.IsNullOrEmpty(s.tag) && CountWithTag(s.tag) >= Math.Max(1, s.requiredCount);
                bool isActive = activeSynergies.Contains(s);
                if (shouldBeActive && !isActive)
                {
                    activeSynergies.Add(s);
                    if (s.statModifiers != null)
                        foreach (var m in s.statModifiers) stats.AddModifier(m, s);
                }
                else if (!shouldBeActive && isActive)
                {
                    activeSynergies.Remove(s);
                    stats.RemoveModifiersFromSource(s);
                }
            }
        }
    }
}
