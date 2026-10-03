using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WhiteDragon
{
    /// <summary>
    /// Tools/WhiteDragon/New menu entries for enemies: Enemy (definition + starter brain), Enemy Brain, Enemy Variant,
    /// Enemy Behavior Script (generates a C# behavior from a template), Enemy Behavior Asset (searchable chooser)
    /// and Enemy in Scene. Enemy assets live in Data/Resources/Enemies/&lt;Family&gt;/. Never overwrites.
    /// </summary>
    public static class EnemyContentCreator
    {
        public const string EnemiesRoot = ContentCreator.ResourcesRoot + "/Enemies";
        public const string BehaviorScriptsRoot = "Assets/WhiteDragon/Scripts/Enemies/Behaviors";
        const string Menu = "Tools/WhiteDragon/New/";
        const string FleshMaterial = "Assets/WhiteDragon/Data/Materials/Enemy_Flesh.mat";

        // ---------- Menus ----------

        [MenuItem(Menu + "Enemy", priority = 6)]
        public static void NewEnemy() =>
            NamePromptWindow.Show("New Enemy", "New Enemy", "Family", "misc",
                "Lowercase family, e.g. undead. Assets go to Data/Resources/Enemies/<Family>/.",
                (name, family) => CreateEnemy(name, family));

        [MenuItem(Menu + "Enemy Brain", priority = 7)]
        public static void NewBrain() =>
            NamePromptWindow.Show("New Enemy Brain", "New Brain", "Family", "misc",
                "Family folder the brain goes in (Data/Resources/Enemies/<Family>/).",
                (name, family) => Select(CreateBrain(name, family)));

        [MenuItem(Menu + "Enemy Variant", priority = 8)]
        public static void NewVariant()
        {
            var bases = EnemyDefinitions();
            if (bases.Length == 0)
            {
                Debug.LogWarning("[Content] Create an Enemy first; a variant needs a base enemy. Nothing created.");
                return;
            }
            NamePromptWindow.Show("New Enemy Variant", "", "Base enemy", bases.Select(Label).ToArray(),
                (name, index) => Select(CreateVariant(bases[index], name)));
        }

        [MenuItem(Menu + "Enemy Behavior Script", priority = 9)]
        public static void NewBehaviorScript() =>
            NamePromptWindow.Show("New Enemy Behavior Script", "", "Enemy (optional)", "",
                "Empty = a shared behavior in Behaviors/Common. An enemy name puts it in Behaviors/Specific/<Enemy>.",
                (name, enemy) =>
                {
                    string path = CreateBehaviorScript(name, enemy);
                    if (path == null) return;
                    AssetDatabase.Refresh();
                    Select(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                    Debug.Log($"[Content] Created {path}. After it compiles, use Tools/WhiteDragon/New/Enemy Behavior Asset to make an asset of it.");
                });

        [MenuItem(Menu + "Enemy Behavior Asset", priority = 10)]
        public static void NewBehaviorAsset() => EnemyBehaviorPickerWindow.Open();

        [MenuItem(Menu + "Enemy in Scene", priority = 11)]
        public static void NewEnemyInScene() => EnemyInSceneWindow.Open();

        // ---------- Definitions and brains ----------

        /// <summary>
        /// A definition plus a working starter brain (chase and touch) in the enemy's own folder,
        /// Enemies/&lt;Family&gt;/&lt;Name&gt;/. The brain's behaviors are NEW assets in that folder, never shared ones,
        /// so tuning this enemy never changes another; share by dragging an existing asset into a brain on purpose.
        /// </summary>
        public static EnemyDefinition CreateEnemy(string name, string family)
        {
            if (!IsNameGiven(name)) return null;
            family = NormalizeFamily(family);
            string familyFolder = EnsureFamilyFolder(family);
            string ownFolder = AssetDatabase.GenerateUniqueAssetPath($"{familyFolder}/{ToPascal(name)}");
            AssetDatabase.CreateFolder(familyFolder, Path.GetFileName(ownFolder));
            string folder = ownFolder;
            string id = ContentIds.MakeUnique(BaseId(name, "enemy"), EnemyDefinitions().Select(d => d.id).Where(x => !string.IsNullOrEmpty(x)));
            string file = ContentIds.ToFileName(name);

            var chase = Save(ScriptableObject.CreateInstance<MoveTowardBehavior>(), folder, file + "_Chase");
            var contact = Save(ScriptableObject.CreateInstance<MeleeContactBehavior>(), folder, file + "_Contact");
            var brain = ScriptableObject.CreateInstance<StateMachineBrain>();
            brain.states.Add(new BrainState { name = "Chase", behaviors = new List<EnemyBehavior> { chase, contact } });
            Save(brain, folder, file + "_Brain");

            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.id = id;
            def.displayName = name;
            def.family = family;
            def.brain = brain;
            def.movement = MovementMode.Ground;
            Save(def, folder, file);
            AssetDatabase.SaveAssets();
            EnemyCatalog.Reload();
            Select(def);
            return def;
        }

        /// <summary>An empty state machine brain with one "Start" state.</summary>
        public static StateMachineBrain CreateBrain(string name, string family)
        {
            if (!IsNameGiven(name)) return null;
            var brain = ScriptableObject.CreateInstance<StateMachineBrain>();
            brain.states.Add(new BrainState { name = "Start" });
            Save(brain, EnsureFamilyFolder(NormalizeFamily(family)), ContentIds.ToFileName(name));
            AssetDatabase.SaveAssets();
            return brain;
        }

        public static EnemyVariant CreateVariant(EnemyDefinition baseEnemy, string name)
        {
            if (baseEnemy == null) return null;
            if (string.IsNullOrWhiteSpace(name)) name = Label(baseEnemy) + " Variant";
            var v = ScriptableObject.CreateInstance<EnemyVariant>();
            v.id = ContentIds.MakeUnique(BaseId(name, "variant"), EnemyCatalog.Variants.Select(x => x.id).Where(x => !string.IsNullOrEmpty(x)));
            v.displaySuffix = $"({name})";
            v.baseEnemy = baseEnemy;
            Save(v, FolderOf(baseEnemy), ContentIds.ToFileName(name));
            AssetDatabase.SaveAssets();
            EnemyCatalog.Reload();
            return v;
        }

        // ---------- Behavior scripts ----------

        /// <summary>Class name for a behavior: PascalCase, ending in "Behavior".</summary>
        public static string BehaviorClassName(string name)
        {
            string pascal = ToPascal(name);
            if (pascal.Length == 0) return "";
            if (char.IsDigit(pascal[0])) pascal = "B" + pascal;
            return pascal.EndsWith("Behavior", StringComparison.Ordinal) ? pascal : pascal + "Behavior";
        }

        /// <summary>Common/&lt;Class&gt;.cs, or Specific/&lt;Enemy&gt;/&lt;Class&gt;.cs when an enemy name is given.</summary>
        public static string BehaviorScriptPath(string className, string enemyName)
        {
            string enemy = ToPascal(enemyName);
            return enemy.Length == 0
                ? $"{BehaviorScriptsRoot}/Common/{className}.cs"
                : $"{BehaviorScriptsRoot}/Specific/{enemy}/{className}.cs";
        }

        /// <summary>Writes the behavior script from the template. Returns its path, or null if refused.</summary>
        public static string CreateBehaviorScript(string name, string enemyName)
        {
            string className = BehaviorClassName(name);
            if (className.Length == 0)
            {
                Debug.LogWarning("[Content] No name given; nothing created.");
                return null;
            }
            if (TypeCache.GetTypesDerivedFrom<EnemyBehavior>().Any(t => t.Name == className))
            {
                Debug.LogWarning($"[Content] A behavior class named {className} already exists; nothing created.");
                return null;
            }
            string path = BehaviorScriptPath(className, enemyName);
            if (File.Exists(path))
            {
                Debug.LogWarning($"[Content] {path} already exists; nothing created.");
                return null;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, BehaviorScriptText(className, ToPascal(enemyName)));
            return path;
        }

        /// <summary>The C# source of a new behavior (compiles as is).</summary>
        public static string BehaviorScriptText(string className, string enemy)
        {
            string shortName = className.EndsWith("Behavior", StringComparison.Ordinal) ? className.Substring(0, className.Length - "Behavior".Length) : className;
            if (shortName.Length == 0) shortName = className;
            string nice = ObjectNames.NicifyVariableName(shortName);
            string category = string.IsNullOrEmpty(enemy) ? "Custom" : enemy;
            string menu = string.IsNullOrEmpty(enemy) ? nice : enemy + "/" + nice;
            var s = new StringBuilder();
            s.AppendLine("using UnityEngine;");
            s.AppendLine();
            s.AppendLine("namespace WhiteDragon");
            s.AppendLine("{");
            s.AppendLine("    /// <summary>");
            s.AppendLine("    /// TODO: what this behavior does. Assets of it are shared by every enemy that uses them, so keep fields");
            s.AppendLine("    /// as Inspector parameters only; anything that changes while an enemy runs goes in State.");
            s.AppendLine("    /// If it starts an attack, put a Telegraph state before the state that runs it.");
            s.AppendLine("    /// </summary>");
            s.AppendLine($"    [EnemyBehaviorInfo(\"TODO: one-line description.\", \"{category}\")]");
            s.AppendLine($"    [CreateAssetMenu(menuName = \"WhiteDragon/Enemy Behaviors/{menu}\", fileName = \"{shortName}\")]");
            s.AppendLine($"    public class {className} : EnemyBehavior<{className}.State>");
            s.AppendLine("    {");
            s.AppendLine("        /// <summary>One per enemy, created once and reused: reset it in Enter.</summary>");
            s.AppendLine("        public class State");
            s.AppendLine("        {");
            s.AppendLine("            public float Time;");
            s.AppendLine("        }");
            s.AppendLine();
            s.AppendLine("        [Tooltip(\"Seconds until this behavior reports Finished. 0 = never finishes.\")]");
            s.AppendLine("        [Min(0f)] public float duration = 1f;");
            s.AppendLine();
            s.AppendLine("        // The state running this behavior was entered.");
            s.AppendLine("        protected override void Enter(EnemyContext ctx, State s)");
            s.AppendLine("        {");
            s.AppendLine("            s.Time = 0f;");
            s.AppendLine("        }");
            s.AppendLine();
            s.AppendLine("        // Every frame while the state is active. Return true when done (the BehaviorFinished transition).");
            s.AppendLine("        // Helpers: ctx.MoveToward, ctx.Move, ctx.FaceTarget, ctx.Freeze, ctx.Damage, ctx.FireProjectile, ctx.Random.");
            s.AppendLine("        protected override bool Tick(EnemyContext ctx, State s, float dt)");
            s.AppendLine("        {");
            s.AppendLine("            s.Time += dt;");
            s.AppendLine("            if (ctx.HasTarget) ctx.FaceTarget();");
            s.AppendLine("            return duration > 0f && s.Time >= duration - 1e-4f;");
            s.AppendLine("        }");
            s.AppendLine();
            s.AppendLine("        // The state was left.");
            s.AppendLine("        protected override void Exit(EnemyContext ctx, State s)");
            s.AppendLine("        {");
            s.AppendLine("        }");
            s.AppendLine("    }");
            s.AppendLine("}");
            return s.ToString();
        }

        // ---------- Behavior assets ----------

        /// <summary>Concrete behavior types of the game (not tests), sorted by category then name.</summary>
        public static Type[] BehaviorTypes() =>
            TypeCache.GetTypesDerivedFrom<EnemyBehavior>()
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.Assembly == typeof(EnemyBehavior).Assembly)
                .OrderBy(t => Info(t)?.Category ?? "", StringComparer.Ordinal)
                .ThenBy(t => t.Name, StringComparer.Ordinal)
                .ToArray();

        public static EnemyBehaviorInfoAttribute Info(Type t) =>
            (EnemyBehaviorInfoAttribute)Attribute.GetCustomAttribute(t, typeof(EnemyBehaviorInfoAttribute), false);

        public static EnemyBehavior CreateBehaviorAsset(Type type, string name, string family)
        {
            if (type == null) return null;
            if (string.IsNullOrWhiteSpace(name)) name = type.Name.Replace("Behavior", "");
            var b = (EnemyBehavior)ScriptableObject.CreateInstance(type);
            Save(b, EnsureFamilyFolder(NormalizeFamily(family)), ContentIds.ToFileName(name));
            AssetDatabase.SaveAssets();
            return b;
        }

        // ---------- Enemy in scene ----------

        /// <summary>
        /// Builds a ready enemy GameObject from a definition: its prefab if one in its folder points at it, otherwise
        /// placeholder shapes tinted by the definition. With a room, it is parented there and APPENDED to the room's
        /// enemy list (never inserted: each enemy's random key is room id + list index).
        /// </summary>
        public static Enemy BuildInScene(EnemyDefinition definition, EnemyVariant variant, Vector3 position, RoomController room)
        {
            if (definition == null) return null;
            var prefab = FindPrefab(definition);
            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = Label(definition);
            }
            else
            {
                go = BuildPlaceholder(definition);
            }
            go.transform.position = position;
            var enemy = go.GetComponent<Enemy>();
            enemy.definition = definition;
            enemy.variant = variant;
            Undo.RegisterCreatedObjectUndo(go, "Enemy in Scene");
            if (room != null) AppendToRoom(room, enemy);
            return enemy;
        }

        /// <summary>Parents the enemy under the room (its "Enemies" child if any) and adds it at the END of the list.</summary>
        public static void AppendToRoom(RoomController room, Enemy enemy)
        {
            var parent = room.transform.Find("Enemies") ?? room.transform;
            Undo.SetTransformParent(enemy.transform, parent, "Enemy in Scene");
            Undo.RecordObject(room, "Enemy in Scene");
            room.enemies.Add(enemy);
            EditorUtility.SetDirty(room);
        }

        public static GameObject FindPrefab(EnemyDefinition definition)
        {
            string path = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrEmpty(path)) return null;
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var e = p != null ? p.GetComponent<Enemy>() : null;
                if (e != null && e.definition == definition) return p;
            }
            return null;
        }

        static GameObject BuildPlaceholder(EnemyDefinition definition)
        {
            bool flying = definition.movement == MovementMode.Flying;
            var go = new GameObject(Label(definition));
            var cc = go.AddComponent<CharacterController>();
            cc.radius = flying ? 0.35f : 0.4f;
            cc.height = flying ? 0.7f : 1.9f;
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            go.AddComponent<StatusReceiver>();
            go.AddComponent<Enemy>();
            var flesh = AssetDatabase.LoadAssetAtPath<Material>(FleshMaterial);
            if (flying)
            {
                Part(go.transform, PrimitiveType.Sphere, "Body", new Vector3(0f, 0.35f, 0f), new Vector3(0.5f, 0.45f, 0.55f), flesh);
            }
            else
            {
                Part(go.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 0.8f, 0.6f), flesh);
                Part(go.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.65f, 0f), new Vector3(0.45f, 0.45f, 0.45f), flesh);
            }
            return go;
        }

        static void Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            var p = GameObject.CreatePrimitive(type);
            p.name = name;
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false);
            p.transform.localPosition = position;
            p.transform.localScale = scale;
            if (material != null) p.GetComponent<Renderer>().sharedMaterial = material;
        }

        // ---------- Helpers ----------

        public static EnemyDefinition[] EnemyDefinitions() =>
            ContentCreator.FindAll<EnemyDefinition>().OrderBy(d => Label(d), StringComparer.OrdinalIgnoreCase).ToArray();

        public static string Label(EnemyDefinition d) => string.IsNullOrEmpty(d.displayName) ? d.name : d.displayName;

        /// <summary>Lowercase snake_case family; empty becomes "misc".</summary>
        public static string NormalizeFamily(string family)
        {
            string f = ContentIds.ToSnakeCase(family ?? "");
            return f.Length == 0 ? "misc" : f;
        }

        /// <summary>Folder for a family: undead -> Data/Resources/Enemies/Undead.</summary>
        public static string FamilyFolder(string family) => $"{EnemiesRoot}/{ToPascal(NormalizeFamily(family))}";

        public static string EnsureFamilyFolder(string family)
        {
            string folder = FamilyFolder(family);
            if (!AssetDatabase.IsValidFolder(EnemiesRoot)) AssetDatabase.CreateFolder(ContentCreator.ResourcesRoot, "Enemies");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(EnemiesRoot, Path.GetFileName(folder));
            return folder;
        }

        static string FolderOf(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? EnsureFamilyFolder("misc") : Path.GetDirectoryName(path).Replace('\\', '/');
        }

        /// <summary>"dark elves" / "dark_elves" -> "DarkElves".</summary>
        public static string ToPascal(string text)
        {
            var sb = new StringBuilder();
            bool upper = true;
            foreach (char c in text ?? "")
            {
                if (!char.IsLetterOrDigit(c))
                {
                    upper = true;
                    continue;
                }
                sb.Append(upper ? char.ToUpperInvariant(c) : c);
                upper = false;
            }
            return sb.ToString();
        }

        static string BaseId(string name, string fallback)
        {
            string id = ContentIds.ToSnakeCase(name);
            return id.Length == 0 ? fallback : id;
        }

        static bool IsNameGiven(string name)
        {
            if (!string.IsNullOrWhiteSpace(name)) return true;
            Debug.LogWarning("[Content] No name given; nothing created.");
            return false;
        }

        static T Save<T>(T asset, string folder, string fileName) where T : Object
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}.asset");
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[Content] Created {asset.GetType().Name} at {path}", asset);
            return asset;
        }

        static void Select(Object o)
        {
            if (o == null) return;
            Selection.activeObject = o;
            EditorGUIUtility.PingObject(o);
        }
    }
}
