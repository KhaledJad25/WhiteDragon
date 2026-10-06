using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Creates the placeholder pickups and their effects. Idempotent: an asset that already exists is reused and never
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
            Pickup("half_heart", "Half Heart", T("heart"), healHalf);
            Pickup("full_heart", "Full Heart", T("heart"), healFull);
            Pickup("soul_half", "Half Soul Heart", T("heart", "soul"), soulHalf);
            Pickup("dark_half", "Half Dark Heart", T("heart", "dark"), darkHalf);
            Pickup("coin", "Coin", T("coin"), coin1);
            Pickup("big_coin", "Big Coin", T("coin"), coin5);
            Pickup("key", "Key", T("key"), key1);

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

        static string[] T(params string[] tags) => tags;
    }
}
