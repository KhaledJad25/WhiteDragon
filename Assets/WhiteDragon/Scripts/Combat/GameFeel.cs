using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Hit-stop, trauma camera shake, particle bursts and code-generated placeholder sounds.
    /// Creates itself on first use. All randomness here is cosmetic.
    /// </summary>
    public class GameFeel : MonoBehaviour
    {
        public static float ShakeScale = 1f;
        public static float HitStopScale = 1f;
        public static float SoundVolume = 0.6f;

        const float SlowTimeScale = 0.05f;
        const float TraumaDecay = 1.5f;
        const float MaxShakeAngle = 3f;

        static GameFeel instance;

        AudioSource audioSource;
        AudioClip throwClip, hitClip, killClip, wallClip, hurtClip;
        float trauma;
        float hitStopUntil;
        bool hitStopActive;
        Transform shakenCamera;

        static GameFeel Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("GameFeel");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<GameFeel>();
                }
                return instance;
            }
        }

        // ---- Events used by gameplay code ----

        public static void OnThrow(Vector3 position)
        {
            if (!Application.isPlaying) return;
            Instance.Play(Instance.throwClip, 0.35f);
        }

        public static void OnHit(Vector3 point, Color color)
        {
            if (!Application.isPlaying) return;
            HitStop(0.04f);
            Shake(0.15f);
            Burst(point, color, 8, 3f);
            Instance.Play(Instance.hitClip, 0.8f);
        }

        public static void OnKill(Vector3 point, Color color)
        {
            if (!Application.isPlaying) return;
            HitStop(0.09f);
            Shake(0.4f);
            Burst(point, color, 30, 6f);
            Instance.Play(Instance.killClip, 0.9f);
        }

        public static void OnWallImpact(Vector3 point, Color color)
        {
            if (!Application.isPlaying) return;
            Burst(point, color, 5, 2f);
            Instance.Play(Instance.wallClip, 0.4f);
        }

        public static void OnPlayerHurt(Vector3 point)
        {
            if (!Application.isPlaying) return;
            HitStop(0.08f);
            Shake(0.6f);
            Burst(point, new Color(0.6f, 0.02f, 0.02f), 16, 4f);
            Instance.Play(Instance.hurtClip, 1f);
        }

        // ---- Building blocks ----

        public static void HitStop(float seconds)
        {
            if (!Application.isPlaying) return;
            if (HitStopScale <= 0f || seconds <= 0f) return;
            var i = Instance;
            i.hitStopUntil = Mathf.Max(i.hitStopUntil, Time.unscaledTime + seconds * HitStopScale);
            i.hitStopActive = true;
            Time.timeScale = SlowTimeScale;
        }

        public static void Shake(float amount)
        {
            if (!Application.isPlaying) return;
            var i = Instance;
            i.trauma = Mathf.Clamp01(i.trauma + amount * ShakeScale);
        }

        public static void Burst(Vector3 position, Color color, int count, float speed)
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("Burst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = color;
            main.gravityModifier = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = PlaceholderMaterials.Particle();

            ps.Emit(count);
            Destroy(go, 1.5f);
        }

        void Play(AudioClip clip, float volume)
        {
            if (SoundVolume <= 0f || clip == null) return;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip, volume * SoundVolume);
        }

        // ---- Lifecycle ----

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            throwClip = MakeClip("Throw", 0.08f, t => Noise() * Mathf.Pow(1f - t, 2f) * 0.5f);
            hitClip = MakeClip("Hit", 0.12f, t => (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(180f, 60f, t) * t * 0.12f) + Noise() * 0.3f) * Mathf.Pow(1f - t, 3f));
            killClip = MakeClip("Kill", 0.35f, t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 40f, t) * t * 0.35f)) * 0.4f * Mathf.Pow(1f - t, 2f));
            wallClip = MakeClip("Wall", 0.05f, t => Noise() * Mathf.Pow(1f - t, 4f) * 0.6f);
            hurtClip = MakeClip("Hurt", 0.25f, t => (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 45f, t) * t * 0.25f) * 0.8f + Noise() * 0.4f) * Mathf.Pow(1f - t, 2f));
        }

        void Update()
        {
            if (hitStopActive && Time.unscaledTime >= hitStopUntil)
            {
                hitStopActive = false;
                Time.timeScale = 1f;
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (shakenCamera != null && shakenCamera != cam.transform) shakenCamera.localRotation = Quaternion.identity;
            shakenCamera = cam.transform;

            if (trauma <= 0f) return;
            trauma = Mathf.Max(0f, trauma - TraumaDecay * Time.unscaledDeltaTime);
            float s = trauma * trauma * MaxShakeAngle;
            float t = Time.unscaledTime * 25f;
            shakenCamera.localRotation = Quaternion.Euler(
                (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * s,
                (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * s,
                (Mathf.PerlinNoise(t, t) - 0.5f) * 2f * s);
        }

        void OnDestroy()
        {
            if (hitStopActive) Time.timeScale = 1f;
        }

        static float Noise() => Random.value * 2f - 1f;

        static AudioClip MakeClip(string name, float seconds, System.Func<float, float> wave)
        {
            const int rate = 44100;
            int samples = Mathf.CeilToInt(seconds * rate);
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = Mathf.Clamp(wave((float)i / samples), -1f, 1f);
            var clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            ShakeScale = 1f;
            HitStopScale = 1f;
            SoundVolume = 0.6f;
            Time.timeScale = 1f;
        }
    }
}
