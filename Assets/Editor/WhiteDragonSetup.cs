#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Run once via the menu: Tools > WhiteDragon > Setup Project Structure
// Safe to re-run: it never overwrites existing files.
public static class WhiteDragonSetup
{
    const string Root = "Assets/_Project";

    static readonly string[] Folders =
    {
        "Scripts/Core", "Scripts/Player", "Scripts/Combat", "Scripts/Items", "Scripts/Enemies",
        "Scripts/Generation", "Scripts/UI", "Scripts/Debugging",
        "Data", "Prefabs", "Scenes", "Art", "Audio", "Tests/EditMode"
    };

    [MenuItem("Tools/WhiteDragon/Setup Project Structure")]
    public static void Run()
    {
        foreach (var f in Folders) Directory.CreateDirectory($"{Root}/{f}");

        WriteAsmdef("Scripts/Core", "WD.Core");
        WriteAsmdef("Scripts/Combat", "WD.Combat", "WD.Core");
        WriteAsmdef("Scripts/Player", "WD.Player", "WD.Core", "Unity.InputSystem");
        WriteAsmdef("Scripts/Items", "WD.Items", "WD.Core", "WD.Combat");
        WriteAsmdef("Scripts/Enemies", "WD.Enemies", "WD.Core", "WD.Combat");
        WriteAsmdef("Scripts/Generation", "WD.Generation", "WD.Core");
        WriteAsmdef("Scripts/UI", "WD.UI", "WD.Core", "WD.Combat", "WD.Player", "WD.Items", "WD.Enemies", "WD.Generation");
        WriteAsmdef("Scripts/Debugging", "WD.Debugging", "WD.Core", "WD.Combat", "WD.Player", "WD.Items", "WD.Enemies", "WD.Generation");
        WriteTestAsmdef();

        AssetDatabase.Refresh();
        CreateScenes();
        Debug.Log("WhiteDragon setup complete. Now add the Core, Combat, and Test scripts.");
    }

    static void WriteAsmdef(string folder, string name, params string[] refs)
    {
        string path = $"{Root}/{folder}/{name}.asmdef";
        if (File.Exists(path)) return;
        string refJson = refs.Length == 0 ? "" : "\"" + string.Join("\", \"", refs) + "\"";
        File.WriteAllText(path,
$@"{{
    ""name"": ""{name}"",
    ""rootNamespace"": """",
    ""references"": [{refJson}],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}
");
    }

    static void WriteTestAsmdef()
    {
        string path = $"{Root}/Tests/EditMode/WD.Tests.EditMode.asmdef";
        if (File.Exists(path)) return;
        File.WriteAllText(path,
@"{
    ""name"": ""WD.Tests.EditMode"",
    ""rootNamespace"": """",
    ""references"": [""UnityEngine.TestRunner"", ""UnityEditor.TestRunner"", ""WD.Core"", ""WD.Combat""],
    ""includePlatforms"": [""Editor""],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": true,
    ""precompiledReferences"": [""nunit.framework.dll""],
    ""autoReferenced"": false,
    ""defineConstraints"": [""UNITY_INCLUDE_TESTS""],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}
");
    }

    static void CreateScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string boot = $"{Root}/Scenes/Bootstrap.unity";
        string room = $"{Root}/Scenes/TestRoom.unity";
        if (!File.Exists(boot))
            EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), boot);
        if (!File.Exists(room))
            EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single), room);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(boot, true),
            new EditorBuildSettingsScene(room, true)
        };
        EditorSceneManager.OpenScene(room);
    }
}
#endif