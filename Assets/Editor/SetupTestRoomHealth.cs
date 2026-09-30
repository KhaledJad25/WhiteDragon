#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupTestRoomHealth
{
    [MenuItem("Tools/WhiteDragon/Wire Health and HUD in TestRoom")]
    public static void Run()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "TestRoom")
        {
            return;
        }

        bool modified = false;

        // 1. Wire PlayerHealth to Player
        PlayerController playerController = Object.FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            GameObject playerGo = playerController.gameObject;
            PlayerHealth playerHealth = playerGo.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = playerGo.AddComponent<PlayerHealth>();
                modified = true;
                Debug.Log("[WhiteDragon] Added PlayerHealth to Player GameObject.");
            }
        }

        // 2. Wire HeartHUD to DebugTools
        HeartHUD heartHud = Object.FindAnyObjectByType<HeartHUD>();
        if (heartHud == null)
        {
            GameObject debugTools = GameObject.Find("DebugTools");
            if (debugTools != null)
            {
                heartHud = debugTools.AddComponent<HeartHUD>();
                modified = true;
                Debug.Log("[WhiteDragon] Added HeartHUD to DebugTools GameObject.");
            }
            else
            {
                GameObject hudGo = new GameObject("HeartHUD");
                heartHud = hudGo.AddComponent<HeartHUD>();
                modified = true;
                Debug.Log("[WhiteDragon] Created HeartHUD GameObject.");
            }
        }

        // 3. Create a test DamageHazard pad in TestRoom
        DamageHazard hazard = Object.FindAnyObjectByType<DamageHazard>();
        if (hazard == null)
        {
            GameObject hazardGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hazardGo.name = "SpikeHazard";
            hazardGo.transform.position = new Vector3(3f, 0.15f, 4f);
            hazardGo.transform.localScale = new Vector3(2.5f, 0.3f, 2.5f);

            Collider col = hazardGo.GetComponent<Collider>();
            col.isTrigger = true;

            hazard = hazardGo.AddComponent<DamageHazard>();

            Renderer rend = hazardGo.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.7f, 0.15f, 0.15f, 0.8f);
                rend.sharedMaterial = mat;
            }

            modified = true;
            Debug.Log("[WhiteDragon] Created test DamageHazard at (3, 0.15, 4).");
        }

        if (modified)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[WhiteDragon] Successfully wired and saved TestRoom scene with Health & HUD systems.");
        }
    }
}
#endif

