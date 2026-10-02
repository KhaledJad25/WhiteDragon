using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Shaders for materials created in code, referenced from Data/Resources/RenderingDefaults.asset so
    /// player builds include them (no Shader.Find). Pipeline: URP.
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Rendering Defaults", fileName = "RenderingDefaults")]
    public class RenderingDefaults : ScriptableObject
    {
        [Tooltip("URP Lit. Used for placeholder shapes and rocks.")]
        public Shader lit;
        [Tooltip("URP Particles/Unlit. Used for hit bursts.")]
        public Shader particles;

        static RenderingDefaults current;
        static bool loaded;

        public static RenderingDefaults Current
        {
            get
            {
                if (!loaded)
                {
                    current = Resources.Load<RenderingDefaults>("RenderingDefaults");
                    loaded = true;
                }
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            loaded = false;
        }
    }
}
