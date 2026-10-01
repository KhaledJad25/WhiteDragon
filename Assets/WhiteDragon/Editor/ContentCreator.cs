using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Tools/WhiteDragon/New menu. Asks for a name, creates the asset in the right Resources folder
    /// with a unique snake_case id and sensible defaults, then selects and pings it.
    /// Never overwrites: file names are made unique by the AssetDatabase.
    /// </summary>
    public static class ContentCreator
    {
        public const string ResourcesRoot = "Assets/WhiteDragon/Data/Resources";
        const string Menu = "Tools/WhiteDragon/New/";

        [MenuItem(Menu + "Item", priority = 1)]
        public static void NewItem() =>
            NamePromptWindow.Show("New Item", "New Item", name => CreateItem(name));

        [MenuItem(Menu + "Synergy", priority = 2)]
        public static void NewSynergy() =>
            NamePromptWindow.Show("New Synergy", "New Synergy", name => CreateSynergy(name));

        [MenuItem(Menu + "Shot Effect Asset", priority = 3)]
        public static void NewShotEffect()
        {
            var types = ShotEffectTypes();
            if (types.Length == 0)
            {
                Debug.LogWarning("[Content] No ShotEffect subclasses found; nothing created.");
                return;
            }
            var labels = types.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
            NamePromptWindow.Show("New Shot Effect Asset", "New Effect", "Effect type", labels,
                (name, index) => CreateShotEffect(types[index], name));
        }

        [MenuItem(Menu + "Status Effect", priority = 4)]
        public static void NewStatus() =>
            NamePromptWindow.Show("New Status Effect", "New Status", name => CreateStatus(name));

        [MenuItem(Menu + "Apply-Status Effect", priority = 5)]
        public static void NewApplyStatus()
        {
            var statuses = FindAll<StatusEffectDefinition>().OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
            if (statuses.Length == 0)
            {
                Debug.LogWarning("[Content] Create a Status Effect first; an Apply-Status effect needs one. Nothing created.");
                return;
            }
            var labels = statuses.Select(s => string.IsNullOrEmpty(s.displayName) ? s.name : s.displayName).ToArray();
            NamePromptWindow.Show("New Apply-Status Effect", "", "Status", labels,
                (name, index) => CreateApplyStatus(statuses[index], name));
        }

        [MenuItem(Menu + "Enemy", priority = 6)]
        public static void NewEnemy() =>
            NamePromptWindow.Show("New Enemy", "New Enemy", name => CreateEnemy(name));

        [MenuItem(Menu + "Character", priority = 20)]
        public static void NewCharacter() =>
            EditorUtility.DisplayDialog("Characters are coming",
                "Character assets arrive in a later step of the authoring tools. Until then the player uses the default stats and hearts.",
                "OK");

        // ---------- Creation (also used by the Content Browser) ----------

        public static ItemDefinition CreateItem(string name) =>
            CreateWithId<ItemDefinition>("Items", name, i => i.id, (i, id) =>
            {
                i.id = id;
                i.displayName = name;
                i.rarity = ItemRarity.Common;
                i.poolIds = new[] { "normal" };
                i.weightMultiplier = 1f;
            });

        public static SynergyDefinition CreateSynergy(string name) =>
            CreateWithId<SynergyDefinition>("Synergies", name, s => s.id, (s, id) =>
            {
                s.id = id;
                s.displayName = name;
            });

        public static StatusEffectDefinition CreateStatus(string name) =>
            CreateWithId<StatusEffectDefinition>("Statuses", name, s => s.id, (s, id) =>
            {
                s.id = id;
                s.displayName = name;
            });

        public static EnemyDefinition CreateEnemy(string name) =>
            CreateWithId<EnemyDefinition>("Enemies", name, e => e.id, (e, id) =>
            {
                e.id = id;
                e.displayName = name;
            });

        public static ShotEffect CreateShotEffect(Type type, string name)
        {
            if (!IsNameGiven(name)) return null;
            var effect = (ShotEffect)ScriptableObject.CreateInstance(type);
            return Save(effect, "Effects", name);
        }

        public static ApplyStatusEffect CreateApplyStatus(StatusEffectDefinition status, string name)
        {
            string label = string.IsNullOrEmpty(status.displayName) ? status.name : status.displayName;
            if (string.IsNullOrWhiteSpace(name)) name = "Apply " + label;
            var effect = ScriptableObject.CreateInstance<ApplyStatusEffect>();
            effect.status = status;
            effect.description = "On hit, applies " + label + ".";
            return Save(effect, "Effects", name);
        }

        public static Type[] ShotEffectTypes() =>
            TypeCache.GetTypesDerivedFrom<ShotEffect>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.Assembly == typeof(ShotEffect).Assembly)
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToArray();

        public static IEnumerable<T> FindAll<T>() where T : ScriptableObject =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null);

        // ---------- Helpers ----------

        static T CreateWithId<T>(string folder, string name, Func<T, string> getId, Action<T, string> init) where T : ScriptableObject
        {
            if (!IsNameGiven(name)) return null;
            string baseId = ContentIds.ToSnakeCase(name);
            if (baseId.Length == 0) baseId = ContentIds.ToSnakeCase(typeof(T).Name);
            string id = ContentIds.MakeUnique(baseId, FindAll<T>().Select(getId).Where(x => !string.IsNullOrEmpty(x)));
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset, id);
            return Save(asset, folder, name);
        }

        static bool IsNameGiven(string name)
        {
            if (!string.IsNullOrWhiteSpace(name)) return true;
            Debug.LogWarning("[Content] No name given; nothing created.");
            return false;
        }

        static T Save<T>(T asset, string folder, string name) where T : ScriptableObject
        {
            string dir = EnsureFolder(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{ContentIds.ToFileName(name)}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[Content] Created {asset.GetType().Name} at {path}", asset);
            return asset;
        }

        static string EnsureFolder(string folder)
        {
            string dir = $"{ResourcesRoot}/{folder}";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder(ResourcesRoot, folder);
                Debug.Log($"[Content] Created folder {dir}");
            }
            return dir;
        }
    }
}
