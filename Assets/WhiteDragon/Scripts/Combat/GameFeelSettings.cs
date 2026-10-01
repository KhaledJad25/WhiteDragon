using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Optional game-feel overrides at Data/Resources/GameFeelSettings.asset. Every empty slot keeps
    /// today's code-generated placeholder; a missing asset keeps everything as it is.
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Game Feel Settings", fileName = "GameFeelSettings")]
    public class GameFeelSettings : ScriptableObject
    {
        [Header("Sounds (optional, empty = generated placeholder)")]
        [Tooltip("When a rock is thrown.")]
        public AudioClip throwSound;
        [Tooltip("When a rock damages something.")]
        public AudioClip hitSound;
        [Tooltip("When an enemy or dummy dies.")]
        public AudioClip killSound;
        [Tooltip("When a rock hits a wall or the floor.")]
        public AudioClip impactSound;
        [Tooltip("When the player takes damage.")]
        public AudioClip hurtSound;

        [Header("Particles (optional, empty = generated burst)")]
        [Tooltip("Spawned where a rock damages something.")]
        public GameObject hitParticles;
        [Tooltip("Spawned where an enemy or dummy dies.")]
        public GameObject killParticles;
        [Tooltip("Spawned where a rock hits a wall or the floor.")]
        public GameObject impactParticles;
        [Tooltip("Seconds before a spawned particle prefab is removed.")]
        [Min(0.1f)]
        public float particleLifetime = 2f;

        [Header("Strength")]
        [Tooltip("Camera shake multiplier. 0 = off, 1 = normal.")]
        [Min(0f)]
        public float shakeScale = 1f;
        [Tooltip("Hit-stop (brief slow-motion) multiplier. 0 = off, 1 = normal.")]
        [Min(0f)]
        public float hitStopScale = 1f;
        [Tooltip("Volume of feedback sounds. 0 = silent.")]
        [Range(0f, 1f)]
        public float soundVolume = 0.6f;

        static GameFeelSettings current;
        static bool loaded;

        /// <summary>The asset in Resources (loaded once). Tests may assign one; null reloads.</summary>
        public static GameFeelSettings Current
        {
            get
            {
                if (!loaded)
                {
                    current = Resources.Load<GameFeelSettings>("GameFeelSettings");
                    loaded = true;
                }
                return current;
            }
            set
            {
                current = value;
                loaded = value != null;
            }
        }

        public AudioClip Clip(GameSound sound)
        {
            switch (sound)
            {
                case GameSound.Throw: return throwSound;
                case GameSound.Hit: return hitSound;
                case GameSound.Kill: return killSound;
                case GameSound.Impact: return impactSound;
                case GameSound.Hurt: return hurtSound;
                default: return null;
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
