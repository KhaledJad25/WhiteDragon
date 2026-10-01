using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Scene entry point. Starts a fresh run once when the scene starts.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            // Debug tools only in the editor and development builds.
            if ((Application.isEditor || Debug.isDebugBuild) && FindAnyObjectByType<DebugPanel>() == null)
                new GameObject("DebugPanel").AddComponent<DebugPanel>();
        }

        void Start() => RunSession.StartRun(0);
    }
}
