#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreateBatch2Content
{
    const string ItemFolder = "Assets/_Project/Data/Resources/Items";
    const string SynergyFolder = "Assets/_Project/Data/Resources/Synergies";

    [MenuItem("Tools/WhiteDragon/Create Batch 2 Content")]
    public static void Run()
    {
        Directory.CreateDirectory(ItemFolder);
        Directory.CreateDirectory(SynergyFolder);

        MakeItem("cinder_ash", "Cinder Ash", "Rocks set enemies ablaze. Burn 2/sec for 3 sec.",
            new[] { "fire" }, None(),
            i => { i.burnDpsAdd = 2f; i.burnDurationAdd = 3f; });

        MakeItem("smoldering_coal", "Smoldering Coal", "+0.5 damage. Slow burn: 1/sec for 4 sec.",
            new[] { "fire" },
            new[] { Stat(StatType.Damage, ModifierKind.Flat, 0.5f) },
            i => { i.burnDpsAdd = 1f; i.burnDurationAdd = 4f; });

        MakeItem("hawks_eye", "Hawk's Eye", "Rocks curve toward nearby enemies.",
            new[] { "holy" }, None(),
            i => { i.homingAdd = 120f; });

        MakeItem("wolfbone_charm", "Wolfbone Charm", "+20% move speed, +15% fire rate.",
            new[] { "swift" },
            new[] { Stat(StatType.MoveSpeed, ModifierKind.PercentAdd, 0.2f), Stat(StatType.FireRate, ModifierKind.PercentAdd, 0.15f) },
            null);

        MakeItem("oathstone", "Oathstone", "+1 damage, +10% range.",
            new[] { "holy" },
            new[] { Stat(StatType.Damage, ModifierKind.Flat, 1f), Stat(StatType.Range, ModifierKind.PercentAdd, 0.1f) },
            null);

        MakeItem("iron_knuckle", "Iron Knuckle", "+15% damage, rocks 15% larger.",
            new[] { "heavy" },
            new[] { Stat(StatType.Damage, ModifierKind.PercentAdd, 0.15f) },
            i => { i.shotSizeMultiplier = 1.15f; });

        MakeItem("black_tallow", "Black Tallow", "Dark rocks. +20% damage.",
            new[] { "dark" },
            new[] { Stat(StatType.Damage, ModifierKind.PercentAdd, 0.2f) },
            i => { i.overrideDamageType = true; i.damageType = DamageType.Dark; });

        MakeSynergy("gloom", "Gloom", "2+ dark items: +30% damage.", "dark", 2,
            new[] { Stat(StatType.Damage, ModifierKind.PercentAdd, 0.3f) }, null);

        MakeSynergy("inferno", "Inferno", "3+ fire items: +25% damage, rocks 30% larger.", "fire", 3,
            new[] { Stat(StatType.Damage, ModifierKind.PercentAdd, 0.25f) },
            s => { s.shotSizeMultiplier = 1.3f; });

        MakeSynergy("juggernaut", "Juggernaut", "2+ heavy items: +1 pierce, +20% damage.", "heavy", 2,
            new[] { Stat(StatType.Damage, ModifierKind.PercentAdd, 0.2f) },
            s => { s.pierceAdd = 1; });

        MakeSynergy("kindling", "Kindling", "2+ fire items: burn +2/sec and +1 sec.", "fire", 2,
            None(),
            s => { s.burnDpsAdd = 2f; s.burnDurationAdd = 1f; });

        MakeSynergy("radiance", "Radiance", "2+ holy items: rocks home in on enemies.", "holy", 2,
            None(),
            s => { s.homingAdd = 90f; });

        MakeSynergy("wind_runner", "Wind Runner", "3+ swift items: +25% move speed and jump.", "swift", 3,
            new[] { Stat(StatType.MoveSpeed, ModifierKind.PercentAdd, 0.25f), Stat(StatType.JumpHeight, ModifierKind.PercentAdd, 0.25f) },
            null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Batch 2 items and synergies created.");
    }

    static ItemDefinition.StatEntry[] None()
    {
        return new ItemDefinition.StatEntry[0];
    }

    static ItemDefinition.StatEntry Stat(StatType stat, ModifierKind kind, float value)
    {
        ItemDefinition.StatEntry e = new ItemDefinition.StatEntry();
        e.stat = stat;
        e.kind = kind;
        e.value = value;
        return e;
    }

    static void MakeItem(string id, string displayName, string description, string[] tags,
        ItemDefinition.StatEntry[] mods, Action<ItemDefinition> edit)
    {
        string path = ItemFolder + "/" + id + ".asset";
        if (File.Exists(path))
        {
            return;
        }

        ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.id = id;
        item.displayName = displayName;
        item.description = description;
        item.tags = tags;
        item.statModifiers = mods;
        if (edit != null)
        {
            edit(item);
        }
        AssetDatabase.CreateAsset(item, path);
    }

    static void MakeSynergy(string id, string displayName, string description, string tag, int count,
        ItemDefinition.StatEntry[] mods, Action<SynergyDefinition> edit)
    {
        string path = SynergyFolder + "/" + id + ".asset";
        if (File.Exists(path))
        {
            return;
        }

        SynergyDefinition s = ScriptableObject.CreateInstance<SynergyDefinition>();
        s.id = id;
        s.displayName = displayName;
        s.description = description;
        s.tag = tag;
        s.requiredCount = count;
        s.statModifiers = mods;
        if (edit != null)
        {
            edit(s);
        }
        AssetDatabase.CreateAsset(s, path);
    }
}
#endif