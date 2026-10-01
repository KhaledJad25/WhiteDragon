using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Creates placeholder starter content. Idempotent: an asset that already exists is reused and
    /// never overwritten. Logs what it created.
    /// </summary>
    public static class StarterContentCreator
    {
        const string Root = "Assets/WhiteDragon/Data/Resources";

        static List<string> created;

        [MenuItem("Tools/WhiteDragon/Create Starter Content")]
        public static void Create()
        {
            created = new List<string>();

            // Statuses
            var burn = Asset<StatusEffectDefinition>("Statuses", "Burn", s =>
            {
                s.id = "burn"; s.displayName = "Burn";
                s.duration = 3f; s.tickInterval = 0.5f; s.damagePerSecond = 2f; s.speedMultiplier = 1f;
                s.tint = new Color(1f, 0.4f, 0.05f); s.stacking = StatusStacking.StackIntensity; s.maxStacks = 3;
            });
            var poison = Asset<StatusEffectDefinition>("Statuses", "Poison", s =>
            {
                s.id = "poison"; s.displayName = "Poison";
                s.duration = 6f; s.tickInterval = 1f; s.damagePerSecond = 1f; s.speedMultiplier = 1f;
                s.tint = new Color(0.35f, 0.8f, 0.15f); s.stacking = StatusStacking.RefreshDuration; s.maxStacks = 3;
            });
            var slow = Asset<StatusEffectDefinition>("Statuses", "Slow", s =>
            {
                s.id = "slow"; s.displayName = "Slow";
                s.duration = 2.5f; s.tickInterval = 1f; s.damagePerSecond = 0f; s.speedMultiplier = 0.5f;
                s.tint = new Color(0.5f, 0.75f, 1f); s.stacking = StatusStacking.RefreshDuration; s.maxStacks = 1;
            });

            // Effects
            var homing = Asset<HomingEffect>("Effects", "Homing", e => e.description = "Rocks curve toward nearby foes.");
            var applyBurn = ApplyStatus("ApplyBurn", burn);
            var applyPoison = ApplyStatus("ApplyPoison", poison);
            var applySlow = ApplyStatus("ApplySlow", slow);

            // Items
            Item("flint", "Flint", "Sharper rocks. Sparks fly.", ItemRarity.Common, T("rock", "fire"), P("normal", "treasure"),
                new[] { M(StatType.Damage, ModifierKind.Flat, 1f) });
            Item("sling_strap", "Sling Strap", "Throw faster.", ItemRarity.Common, T("rock", "speed"), P("normal"),
                new[] { M(StatType.FireRate, ModifierKind.PercentAdd, 0.25f) });
            Item("rotten_boots", "Rotten Boots", "They still run.", ItemRarity.Common, T("speed"), P("normal"),
                new[] { M(StatType.MoveSpeed, ModifierKind.Flat, 1f) });
            Item("frog_legs", "Frog Legs", "Jump higher.", ItemRarity.Common, T("speed"), P("normal"),
                new[] { M(StatType.JumpHeight, ModifierKind.Flat, 0.6f) });
            Item("lucky_tooth", "Lucky Tooth", "Someone else's luck.", ItemRarity.Uncommon, T("luck"), P("normal", "treasure"),
                new[] { M(StatType.Luck, ModifierKind.Flat, 1f) });
            Item("giants_gut", "Giant's Gut", "Bigger. Slower. Meaner.", ItemRarity.Uncommon, T("heavy"), P("treasure"),
                new[] { M(StatType.CharacterSize, ModifierKind.Flat, 0.3f), M(StatType.Damage, ModifierKind.Multiply, 1.25f), M(StatType.MoveSpeed, ModifierKind.Flat, -0.5f) });
            Item("pebble_pouch", "Pebble Pouch", "Three rocks, weaker each.", ItemRarity.Uncommon, T("rock", "multi"), P("treasure"),
                new[] { M(StatType.Damage, ModifierKind.Multiply, 0.8f) }, edits: E(count: 2, spread: 10f));
            Item("boulder", "Boulder", "Huge, slow rocks.", ItemRarity.Rare, T("rock", "heavy"), P("treasure", "boss"),
                new[] { M(StatType.Damage, ModifierKind.Flat, 2f), M(StatType.ProjectileSpeed, ModifierKind.Flat, -4f) }, edits: E(size: 1.8f));
            Item("bone_needle", "Bone Needle", "Rocks pierce flesh.", ItemRarity.Uncommon, T("blood"), P("normal", "treasure"),
                new[] { M(StatType.ProjectileSpeed, ModifierKind.Flat, 6f) }, edits: E(pierce: 2));
            Item("ember_coal", "Ember Coal", "Rocks set foes ablaze.", ItemRarity.Uncommon, T("fire"), P("treasure"),
                null, edits: E(damageType: DamageType.Fire), effects: new ShotEffect[] { applyBurn });
            Item("bile_sac", "Bile Sac", "Poisonous, longer throws.", ItemRarity.Uncommon, T("poison"), P("normal"),
                new[] { M(StatType.Range, ModifierKind.Flat, 5f) }, effects: new ShotEffect[] { applyPoison });
            Item("grave_moss", "Grave Moss", "Rocks slow the living.", ItemRarity.Common, T("dark"), P("normal"),
                null, edits: E(damageType: DamageType.Dark), effects: new ShotEffect[] { applySlow });
            Item("hunters_eye", "Hunter's Eye", "Rocks seek prey.", ItemRarity.Rare, T("holy"), P("treasure", "boss"),
                null, effects: new ShotEffect[] { homing });
            Item("black_heart", "Black Heart", "Heavy blows, slow arm.", ItemRarity.Legendary, T("blood", "dark"), P("boss"),
                new[] { M(StatType.Damage, ModifierKind.Multiply, 1.5f), M(StatType.FireRate, ModifierKind.Flat, -0.3f) },
                edits: E(damageType: DamageType.Blood), unlock: "beat_first_boss");

            // Synergies
            Synergy("pyromaniac", "Pyromaniac", "Fire feeds fire.", "fire", 2,
                new[] { M(StatType.Damage, ModifierKind.Flat, 1f) }, effects: new ShotEffect[] { applyBurn });
            Synergy("speed_demon", "Speed Demon", "Faster everything.", "speed", 3,
                new[] { M(StatType.FireRate, ModifierKind.Flat, 0.5f), M(StatType.MoveSpeed, ModifierKind.Flat, 0.5f) });
            Synergy("rockslide", "Rockslide", "One more rock.", "rock", 3, null, edits: E(count: 1));
            Synergy("heavyweight", "Heavyweight", "Even bigger rocks.", "heavy", 2,
                new[] { M(StatType.Damage, ModifierKind.Multiply, 1.2f) }, edits: E(size: 1.3f));
            Synergy("bloodletter", "Bloodletter", "Rocks drink blood.", "blood", 2, null, edits: E(pierce: 1, damageType: DamageType.Blood));

            // Enemies
            Asset<EnemyDefinition>("Enemies", "Ghoul", e =>
            {
                e.id = "ghoul"; e.displayName = "Ghoul"; e.maxHealth = 20f; e.moveSpeed = 2.5f; e.contactDamage = 1;
                e.tint = new Color(0.42f, 0.48f, 0.36f);
            });
            Asset<EnemyDefinition>("Enemies", "Brute", e =>
            {
                e.id = "brute"; e.displayName = "Brute"; e.maxHealth = 45f; e.moveSpeed = 1.6f; e.contactDamage = 2;
                e.tint = new Color(0.45f, 0.12f, 0.1f);
            });

            AssetDatabase.SaveAssets();
            Debug.Log(created.Count == 0
                ? "[StarterContent] Nothing new; all starter assets already exist."
                : $"[StarterContent] Created {created.Count} assets:\n" + string.Join("\n", created));
        }

        static T Asset<T>(string folder, string name, Action<T> init) where T : ScriptableObject
        {
            string path = $"{Root}/{folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            created.Add(path);
            return asset;
        }

        static ApplyStatusEffect ApplyStatus(string name, StatusEffectDefinition status) =>
            Asset<ApplyStatusEffect>("Effects", name, e =>
            {
                e.status = status;
                e.description = "On hit, applies " + status.displayName + ".";
            });

        static void Item(string id, string displayName, string description, ItemRarity rarity, string[] tags, string[] pools,
            StatModifier[] mods, RecipeEdits? edits = null, ShotEffect[] effects = null, string unlock = "")
        {
            Asset<ItemDefinition>("Items", displayName.Replace("'", ""), i =>
            {
                i.id = id; i.displayName = displayName; i.description = description; i.rarity = rarity;
                i.tags = tags; i.poolIds = pools; i.statModifiers = mods ?? new StatModifier[0];
                i.recipeEdits = edits ?? RecipeEdits.Default;
                if (effects != null) i.effects.AddRange(effects);
                i.weightMultiplier = 1f; i.requiredUnlockId = unlock;
            });
        }

        static void Synergy(string id, string displayName, string description, string tag, int required,
            StatModifier[] mods, RecipeEdits? edits = null, ShotEffect[] effects = null)
        {
            Asset<SynergyDefinition>("Synergies", displayName, s =>
            {
                s.id = id; s.displayName = displayName; s.description = description; s.tag = tag; s.requiredCount = required;
                s.statModifiers = mods ?? new StatModifier[0];
                s.recipeEdits = edits ?? RecipeEdits.Default;
                if (effects != null) s.effects.AddRange(effects);
            });
        }

        static StatModifier M(StatType stat, ModifierKind kind, float value) => new StatModifier(stat, kind, value);
        static string[] T(params string[] tags) => tags;
        static string[] P(params string[] pools) => pools;

        static RecipeEdits E(int count = 0, float spread = 0f, int pierce = 0, float size = 1f, DamageType? damageType = null)
        {
            var e = RecipeEdits.Default;
            e.projectileCountAdd = count;
            e.spreadAddDegrees = spread;
            e.pierceAdd = pierce;
            e.sizeMultiplier = size;
            if (damageType.HasValue)
            {
                e.overrideDamageType = true;
                e.damageType = damageType.Value;
            }
            return e;
        }
    }
}
