#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreateStarterItems
{
    const string Folder = "Assets/_Project/Data/Resources/Items";

    [MenuItem("Tools/WhiteDragon/Create Starter Items")]
    public static void Run()
    {
        Directory.CreateDirectory(Folder);

        Make("iron_grip", "Iron Grip", "Heavier throws. +25% damage, -15% fire rate.",
            new[] { "heavy" },
            new[] { M(StatType.Damage, ModifierKind.PercentAdd, 0.25f), M(StatType.FireRate, ModifierKind.PercentAdd, -0.15f) },
            null);

        Make("swift_fingers", "Swift Fingers", "+30% fire rate.",
            new[] { "swift" },
            new[] { M(StatType.FireRate, ModifierKind.PercentAdd, 0.3f) },
            null);

        Make("ember_stone", "Ember Stone", "Rocks become fire. +1 damage.",
            new[] { "fire" },
            new[] { M(StatType.Damage, ModifierKind.Flat, 1f) },
            i => { i.overrideDamageType = true; i.damageType = DamageType.Fire; });

        Make("hawks_feather", "Hawk's Feather", "Holy rocks. +40% range, +20% projectile speed.",
            new[] { "holy" },
            new[] { M(StatType.Range, ModifierKind.PercentAdd, 0.4f), M(StatType.ProjectileSpeed, ModifierKind.PercentAdd, 0.2f) },
            i => { i.overrideDamageType = true; i.damageType = DamageType.Holy; });

        Make("twin_stones", "Twin Stones", "+1 rock per throw, spread out. Each does 70% damage.",
            new[] { "multi" },
            new[] { M(StatType.Damage, ModifierKind.Multiply, 0.7f) },
            i => { i.projectileCountAdd = 1; i.spreadAddDegrees = 8f; });

        Make("piercing_shard", "Piercing Shard", "Rocks pass through 1 extra enemy.",
            new[] { "sharp" },
            StatEntryArray(),
            i => { i.pierceAdd = 1; });

        Make("giants_marrow", "Giant's Marrow", "Bigger you, bigger rocks. +30% size, +20% damage, -10% speed.",
            new[] { "heavy" },
            new[] { M(StatType.CharacterSize, ModifierKind.PercentAdd, 0.3f), M(StatType.Damage, ModifierKind.PercentAdd, 0.2f), M(StatType.MoveSpeed, ModifierKind.PercentAdd, -0.1f) },
            i => { i.shotSizeMultiplier = 1.4f; });

        Make("grave_dust", "Grave Dust", "+50% jump height, +15% move speed.",
            new[] { "swift" },
            new[] { M(StatType.JumpHeight, ModifierKind.PercentAdd, 0.5f), M(StatType.MoveSpeed, ModifierKind.PercentAdd, 0.15f) },
            null);

        Make("brand_of_sacrifice", "Brand of Sacrifice", "Dark rocks. +40% damage. (Its cost comes with player health.)",
            new[] { "dark", "blood" },
            new[] { M(StatType.Damage, ModifierKind.PercentAdd, 0.4f) },
            i => { i.overrideDamageType = true; i.damageType = DamageType.Dark; });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Starter items created in " + Folder);
    }

    static ItemDefinition.StatEntry[] StatEntryArray()
    {
        return new ItemDefinition.StatEntry[0];
    }

    static ItemDefinition.StatEntry M(StatType stat, ModifierKind kind, float value)
    {
        ItemDefinition.StatEntry e = new ItemDefinition.StatEntry();
        e.stat = stat;
        e.kind = kind;
        e.value = value;
        return e;
    }

    static void Make(string id, string displayName, string description, string[] tags,
        ItemDefinition.StatEntry[] mods, Action<ItemDefinition> edit)
    {
        string path = Folder + "/" + id + ".asset";
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
}
#endif