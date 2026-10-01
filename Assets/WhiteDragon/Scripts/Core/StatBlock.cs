using System;
using System.Collections.Generic;

namespace WhiteDragon
{
    /// <summary>
    /// Base values plus modifiers. Value = (base + sum Flat) x (1 + sum PercentAdd) x product Multiply.
    /// Every modifier has a Source so it can be removed again.
    /// </summary>
    public class StatBlock
    {
        struct Entry
        {
            public StatModifier Modifier;
            public object Source;
        }

        readonly float[] baseValues;
        readonly List<Entry> entries = new List<Entry>();

        /// <summary>Raised once per stat whose value may have changed.</summary>
        public event Action<StatType> Changed;

        public int ModifierCount => entries.Count;

        public StatBlock()
        {
            int count = Enum.GetValues(typeof(StatType)).Length;
            baseValues = new float[count];
            for (int i = 0; i < count; i++)
                baseValues[i] = DefaultBase((StatType)i);
        }

        public static float DefaultBase(StatType stat)
        {
            switch (stat)
            {
                case StatType.MoveSpeed: return 5f;
                case StatType.JumpHeight: return 1.2f;
                case StatType.CharacterSize: return 1f;
                case StatType.FireRate: return 2f;
                case StatType.Damage: return 3.5f;
                case StatType.ProjectileSpeed: return 18f;
                case StatType.Range: return 20f;
                case StatType.Luck: return 0f;
                default: return 0f;
            }
        }

        public float GetBase(StatType stat) => baseValues[(int)stat];

        public void SetBase(StatType stat, float value)
        {
            baseValues[(int)stat] = value;
            Changed?.Invoke(stat);
        }

        public float Get(StatType stat)
        {
            float flat = 0f, percent = 0f, multiply = 1f;
            foreach (var e in entries)
            {
                if (e.Modifier.stat != stat) continue;
                switch (e.Modifier.kind)
                {
                    case ModifierKind.Flat: flat += e.Modifier.value; break;
                    case ModifierKind.PercentAdd: percent += e.Modifier.value; break;
                    case ModifierKind.Multiply: multiply *= e.Modifier.value; break;
                }
            }
            return (baseValues[(int)stat] + flat) * (1f + percent) * multiply;
        }

        public void AddModifier(StatModifier modifier, object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            entries.Add(new Entry { Modifier = modifier, Source = source });
            Changed?.Invoke(modifier.stat);
        }

        /// <summary>Removes every modifier from this source. Returns how many were removed.</summary>
        public int RemoveModifiersFromSource(object source)
        {
            var touched = new HashSet<StatType>();
            int removed = 0;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(entries[i].Source, source)) continue;
                touched.Add(entries[i].Modifier.stat);
                entries.RemoveAt(i);
                removed++;
            }
            foreach (var stat in touched)
                Changed?.Invoke(stat);
            return removed;
        }
    }
}
