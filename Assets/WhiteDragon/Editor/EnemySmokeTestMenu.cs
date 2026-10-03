using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhiteDragon
{
    /// <summary>Tools/WhiteDragon/Smoke Test All Enemies (Play mode): runs EnemySmokeTest in a temporary arena scene.</summary>
    public static class EnemySmokeTestMenu
    {
        const string MenuPath = "Tools/WhiteDragon/Smoke Test All Enemies";

        [MenuItem(MenuPath, priority = 102)]
        public static void RunInPlayMode()
        {
            var report = EnemySmokeTest.RunAll(
                () => SceneManager.CreateScene("SmokeTestArena"),
                scene =>
                {
                    foreach (var root in scene.GetRootGameObjects()) Object.Destroy(root);
                    SceneManager.UnloadSceneAsync(scene);
                });
            if (report.Passed) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
        }

        [MenuItem(MenuPath, true)]
        static bool CanRun() => EditorApplication.isPlaying;
    }
}
