using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Spawns an enemy at runtime from a definition (its prefab under Resources/Enemies, else placeholder shapes)
    /// with an optional variant. Debug tools give spawned enemies keys from their own namespace ("debug:N"), so they
    /// never shift the random generators of room enemies, and they never join a RoomController.
    /// </summary>
    public static class EnemySpawner
    {
        static int debugCounter;

        /// <summary>A fresh key in the debug namespace.</summary>
        public static string NextDebugKey() => "debug:" + (++debugCounter);

        public static Enemy Spawn(EnemyDefinition definition, EnemyVariant variant, Vector3 position, string key)
        {
            if (definition == null) return null;
            // Built under an inactive holder so Awake (which reads the variant) runs only once everything is set.
            var holder = new GameObject("SpawnHolder");
            holder.SetActive(false);
            var prefab = EnemyCatalog.PrefabFor(definition);
            var go = prefab != null ? Object.Instantiate(prefab, holder.transform) : Placeholder(definition, holder.transform);
            go.name = definition.displayName + (variant != null && !string.IsNullOrEmpty(variant.displaySuffix) ? " " + variant.displaySuffix : "");
            var enemy = go.GetComponent<Enemy>();
            enemy.definition = definition;
            enemy.variant = variant;
            enemy.SetSpawnKey(key);
            go.transform.SetParent(null, false);
            go.transform.position = position;
            DestroySafe(holder);
            enemy.Initialize();
            return enemy;
        }

        static GameObject Placeholder(EnemyDefinition definition, Transform parent)
        {
            bool flying = definition.movement == MovementMode.Flying;
            var go = new GameObject("Enemy");
            go.transform.SetParent(parent, false);
            var cc = go.AddComponent<CharacterController>();
            cc.radius = flying ? 0.35f : 0.4f;
            cc.height = flying ? 0.7f : 1.9f;
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            go.AddComponent<StatusReceiver>();
            go.AddComponent<Enemy>();
            var body = GameObject.CreatePrimitive(flying ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            body.name = "Body";
            DestroySafe(body.GetComponent<Collider>());
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, cc.height * 0.5f, 0f);
            body.transform.localScale = flying ? new Vector3(0.5f, 0.45f, 0.55f) : new Vector3(0.7f, 0.95f, 0.6f);
            body.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.Lit(Color.white);
            return go;
        }

        static void DestroySafe(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => debugCounter = 0;
    }
}
