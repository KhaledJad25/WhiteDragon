using System;
using System.Collections.Generic;

// Adding a new stat later = add one line here. Nothing else needs to change.
public enum StatType
{
    MoveSpeed, JumpHeight, CharacterSize,
    FireRate, Damage, ProjectileSpeed, Range, Luck
}

public enum ModifierKind { Flat, PercentAdd, Multiply }

public struct StatModifier
{
    public StatType Stat;
    public ModifierKind Kind;
    public float Value;      // PercentAdd: 0.5 means +50%. Multiply: 2 means x2.
    public object Source;    // the item that applied it, so it can be removed later
}

// Value = (base + flats) * (1 + sum of percents) * product of multipliers
public class StatBlock
{
    readonly Dictionary<StatType, float> baseValues = new();
    readonly List<StatModifier> modifiers = new();

    public event Action<StatType> Changed;

    public void SetBase(StatType s, float v)
    {
        baseValues[s] = v;
        Changed?.Invoke(s);
    }

    public void Add(StatModifier m)
    {
        modifiers.Add(m);
        Changed?.Invoke(m.Stat);
    }

    public void RemoveFrom(object source)
    {
        var affected = new HashSet<StatType>();
        foreach (var m in modifiers) if (m.Source == source) affected.Add(m.Stat);

        modifiers.RemoveAll(m => m.Source == source);
        foreach (var s in affected) Changed?.Invoke(s);
    }

    public float Get(StatType s)
    {
        float value = baseValues.TryGetValue(s, out var b) ? b : 0f;
        float pct = 0f, mult = 1f;

        foreach (var m in modifiers)
        {
            if (m.Stat != s) continue;
            switch (m.Kind)
            {
                case ModifierKind.Flat: value += m.Value; break;
                case ModifierKind.PercentAdd: pct += m.Value; break;
                case ModifierKind.Multiply: mult *= m.Value; break;
            }
        }
        return value * (1f + pct) * mult;
    }
}