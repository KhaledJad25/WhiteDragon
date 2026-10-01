using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhiteDragon
{
    /// <summary>Reloads the active scene. In the editor this works even if the scene is not in Build Settings.</summary>
    public static class SceneReloader
    {
        public static void ReloadActive()
        {
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.name);
#endif
        }
    }
}
