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

        [Header("Limits (performance)")]
        [Tooltip("Low / Medium / High scale the caps below by 0.25 / 0.5 / 1. Hook for a future settings menu.")]
        public EffectsQuality quality = EffectsQuality.High;
        [Tooltip("Most damage numbers on screen at once (at High). Extra hits on the same target within a short window add to its number.")]
        [Min(1)]
        public int maxDamageNumbers = GameFeel.DefaultMaxDamageNumbers;
        [Tooltip("Most burst particles alive at once (at High).")]
        [Min(1)]
        public int maxParticles = GameFeel.DefaultMaxParticles;
        [Tooltip("Most live rocks at once (at High). When reached, the oldest rock is recycled.")]
        [Min(1)]
        public int maxProjectiles = GameFeel.DefaultMaxProjectiles;
        [Tooltip("The same sound cannot play again within this many seconds (stops dozens of hits stacking).")]
        [Min(0f)]
        public float soundCooldown = GameFeel.DefaultSoundCooldown;
        [Tooltip("Longest a single hit-stop can last, even when hits keep extending it (seconds).")]
        [Min(0f)]
        public float hitStopMaxDuration = 0.15f;
        [Tooltip("After a hit-stop ends, no new one can start for this long (seconds), so constant hits never lock the game in slow motion.")]
        [Min(0f)]
        public float hitStopCooldown = 0.1f;

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
