using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Creates the placeholder pickups and their effects, the starter drop tables and items, and assigns the tables to the
    /// starter enemies and the Sandbox rooms. Idempotent: an asset that already exists is reused and never
    /// overwritten. Logs what it created.
    /// </summary>
    public static class PickupStarterContent
    {
        const string Root = "Assets/WhiteDragon/Data/Resources";

        static List<string> created;

        [MenuItem("Tools/WhiteDragon/Create Pickup Starter Content")]
        public static void Create()
        {
            created = new List<string>();
            EnsureFolder("Pickups");
            EnsureFolder("Pickups/Effects");

            var coins = AssetDatabase.LoadAssetAtPath<CurrencyDefinition>($"{Root}/Currencies/Coins.asset");
            var keys = AssetDatabase.LoadAssetAtPath<CurrencyDefinition>($"{Root}/Currencies/Keys.asset");

            // Effects
            var healHalf = Asset<HealEffect>("Pickups/Effects", "HealHalf", e => e.halves = 1);
            var healFull = Asset<HealEffect>("Pickups/Effects", "HealFull", e => e.halves = 2);
            var soulHalf = Asset<AddOverlayHeartEffect>("Pickups/Effects", "SoulHalf", e => { e.kind = HeartKind.Soul; e.halves = 1; });
            var darkHalf = Asset<AddOverlayHeartEffect>("Pickups/Effects", "DarkHalf", e => { e.kind = HeartKind.Dark; e.halves = 1; });
            var coin1 = Asset<CurrencyEffect>("Pickups/Effects", "Coin1", e => { e.currency = coins; e.amount = 1; });
            var coin5 = Asset<CurrencyEffect>("Pickups/Effects", "Coin5", e => { e.currency = coins; e.amount = 5; });
            var key1 = Asset<CurrencyEffect>("Pickups/Effects", "Key1", e => { e.currency = keys; e.amount = 1; });

            // Pickups
            var halfHeart = Pickup("half_heart", "Half Heart", T("heart"), healHalf);
            var fullHeart = Pickup("full_heart", "Full Heart", T("heart"), healFull);
            var soul = Pickup("soul_half", "Half Soul Heart", T("heart", "soul"), soulHalf);
            var dark = Pickup("dark_half", "Half Dark Heart", T("heart", "dark"), darkHalf);
            var coin = Pickup("coin", "Coin", T("coin"), coin1);
            var bigCoin = Pickup("big_coin", "Big Coin", T("coin"), coin5);
            var key = Pickup("key", "Key", T("key"), key1);

            // Drop tables
            EnsureFolder("DropTables");
            var enemyCommon = Table("enemy_common", "EnemyCommon", nothing: 4f,
                E(coin, 6f), E(bigCoin, 0.5f), E(key, 1f), E(soul, 1f), E(dark, 0.5f),
                E(halfHeart, 3f, hurt: true), E(fullHeart, 1f, hurt: true));
            var roomClear = Table("room_clear_basic", "RoomClearBasic", nothing: 1f,
                E(coin, 4f, 1, 3), E(bigCoin, 1f), E(key, 2f), E(soul, 1f),
                E(halfHeart, 2f, hurt: true), E(fullHeart, 2f, hurt: true));

            // Items
            Asset<ItemDefinition>("Items", "Lucky Penny", i =>
            {
                i.id = "lucky_penny"; i.displayName = "Lucky Penny"; i.description = "Coins turn up twice as often.";
                i.rarity = ItemRarity.Common; i.tags = new string[0]; i.poolIds = T("normal");
                i.dropModifiers = new[] { new DropModifier("coin", 2f) };
            });
            Asset<ItemDefinition>("Items", "Scavenger", i =>
            {
                i.id = "scavenger"; i.displayName = "Scavenger"; i.description = "The dead give up more.";
                i.rarity = ItemRarity.Common; i.tags = new string[0]; i.poolIds = T("normal");
                i.statModifiers = new[] { new StatModifier(StatType.DropRate, ModifierKind.PercentAdd, 0.25f) };
            });

            // Assignments (only where nothing is assigned yet, so hand edits are kept)
            AssignEnemy("ghoul", enemyCommon, 0.2f);
            AssignEnemy("brute", enemyCommon, 0.35f);
            AssignEnemy("skeleton", enemyCommon, 0.25f);
            AssignEnemy("bat", enemyCommon, 0.15f);
            AssignEnemy("exploder", enemyCommon, 0.2f);
            AssignSandbox(roomClear, "room_01", "room_02");

            AssetDatabase.SaveAssets();
            Debug.Log(created.Count == 0
                ? "[PickupStarterContent] Nothing new; all pickup starter assets already exist."
                : $"[PickupStarterContent] Created {created.Count} assets:\n" + string.Join("\n", created));
        }

        static void EnsureFolder(string folder)
        {
            string path = $"{Root}/{folder}";
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
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

        static PickupDefinition Pickup(string id, string displayName, string[] tags, params PickupEffect[] effects) =>
            Asset<PickupDefinition>("Pickups", displayName.Replace(" ", ""), p =>
            {
                p.id = id;
                p.displayName = displayName;
                p.tags = tags;
                p.effects = new List<PickupEffect>(effects);
            });

        static DropTableDefinition Table(string id, string name, float nothing, params DropEntry[] entries) =>
            Asset<DropTableDefinition>("DropTables", name, t =>
            {
                t.id = id;
                t.nothingWeight = nothing;
                t.rolls = 1;
                t.entries = new List<DropEntry>(entries);
            });

        static DropEntry E(PickupDefinition pickup, float weight, int min = 1, int max = 1, bool hurt = false) => new DropEntry
        {
            pickup = pickup, weight = weight, minCount = min, maxCount = max,
            condition = hurt ? DropCondition.PlayerHurt : DropCondition.Always,
        };

        static void AssignEnemy(string id, DropTableDefinition table, float chance)
        {
            var enemy = EnemyCatalog.Find(id);
            if (enemy == null || enemy.dropTable != null) return;
            enemy.dropTable = table;
            enemy.dropChance = chance;
            EditorUtility.SetDirty(enemy);
            created.Add($"{AssetDatabase.GetAssetPath(enemy)}: drops {table.id} at {chance:0.##}");
        }

        /// <summary>In Sandbox: reward table on the named rooms and PlayerDropModifiers on the Player (only if missing).</summary>
        static void AssignSandbox(DropTableDefinition table, params string[] roomIds)
        {
            const string scenePath = "Assets/WhiteDragon/Scenes/Sandbox.unity";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            bool changed = false;
            foreach (var room in UnityEngine.Object.FindObjectsByType<RoomController>(FindObjectsInactive.Include))
            {
                if (Array.IndexOf(roomIds, room.Id) < 0 || room.rewardTable != null) continue;
                Undo.RecordObject(room, "Assign reward table");
                room.rewardTable = table;
                changed = true;
                created.Add($"{scenePath}: {room.Id} reward table {table.id}");
            }
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player != null && player.GetComponent<PlayerDropModifiers>() == null)
            {
                Undo.AddComponent<PlayerDropModifiers>(player.gameObject);
                changed = true;
                created.Add($"{scenePath}: Player gets PlayerDropModifiers");
            }
            if (!changed) return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static string[] T(params string[] tags) => tags;
    }
}
