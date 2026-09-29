using UnityEngine;

public class GameFeel : MonoBehaviour
{
    public static float ShakeScale = 1f;
    public static float HitStopScale = 1f;
    public static float SoundVolume = 0.6f;

    static GameFeel instance;
    static Material particleMaterial;
    static AudioClip hitClip;
    static AudioClip killClip;
    static AudioClip throwClip;

    AudioSource source;
    float trauma;
    float stopEnd;
    bool stopped;
    bool shaking;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        particleMaterial = null;
        hitClip = null;
        killClip = null;
        throwClip = null;
    }

    static GameFeel Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameFeel");
                instance = go.AddComponent<GameFeel>();
            }
            return instance;
        }
    }

    public static void Throw()
    {
        Instance.AddTrauma(0.04f);
        Instance.PlaySound(GetThrowClip(), 0.5f);
    }

    public static void Hit(Vector3 point)
    {
        Color dust = new Color(0.85f, 0.8f, 0.7f);
        Instance.DoFeedback(point, 0.05f, 0.25f, 10, 0.08f, dust, GetHitClip(), 0.9f);
    }

    public static void Kill(Vector3 point)
    {
        Color blood = new Color(0.75f, 0.12f, 0.1f);
        Instance.DoFeedback(point, 0.10f, 0.5f, 24, 0.12f, blood, GetKillClip(), 1f);
    }

    public static void Impact(Vector3 point)
    {
        Color dust = new Color(0.6f, 0.58f, 0.52f);
        Instance.DoFeedback(point, 0f, 0.04f, 6, 0.06f, dust, GetHitClip(), 0.35f);
    }

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    void OnDisable()
    {
        if (stopped)
        {
            Time.timeScale = 1f;
            stopped = false;
        }
    }

    void DoFeedback(Vector3 point, float stop, float shake, int count, float size, Color color, AudioClip clip, float volume)
    {
        float stopTime = stop * HitStopScale;
        if (stopTime > 0f)
        {
            stopEnd = Mathf.Max(stopEnd, Time.unscaledTime + stopTime);
        }

        AddTrauma(shake);
        SpawnBurst(point, count, size, color);
        PlaySound(clip, volume);
    }

    void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    void Update()
    {
        if (Time.unscaledTime < stopEnd)
        {
            Time.timeScale = 0.05f;
            stopped = true;
        }
        else if (stopped)
        {
            Time.timeScale = 1f;
            stopped = false;
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 2f);
        float s = trauma * trauma * ShakeScale;

        if (s > 0.0001f)
        {
            Vector3 offset = Random.insideUnitSphere * s * 0.15f;
            float roll = (Random.value * 2f - 1f) * s * 3f;
            cam.transform.localPosition = offset;
            cam.transform.localRotation = Quaternion.Euler(0f, 0f, roll);
            shaking = true;
        }
        else if (shaking)
        {
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            shaking = false;
        }
    }

    void SpawnBurst(Vector3 point, int count, float size, Color color)
    {
        GameObject go = new GameObject("HitBurst");
        go.transform.position = point;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.45f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)count)
        });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = GetParticleMaterial();

        ps.Play();
        Destroy(go, 1.5f);
    }

    void PlaySound(AudioClip clip, float volume)
    {
        source.pitch = Random.Range(0.92f, 1.08f);
        source.PlayOneShot(clip, volume * SoundVolume);
    }

    static Material GetParticleMaterial()
    {
        if (particleMaterial == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null)
            {
                sh = Shader.Find("Sprites/Default");
            }
            particleMaterial = new Material(sh);
        }
        return particleMaterial;
    }

    static AudioClip GetHitClip()
    {
        if (hitClip == null)
        {
            hitClip = MakeClip("hit", 0.15f, 140f, 0.5f, 30f, 11);
        }
        return hitClip;
    }

    static AudioClip GetKillClip()
    {
        if (killClip == null)
        {
            killClip = MakeClip("kill", 0.3f, 90f, 0.6f, 14f, 23);
        }
        return killClip;
    }

    static AudioClip GetThrowClip()
    {
        if (throwClip == null)
        {
            throwClip = MakeClip("throw", 0.12f, 300f, 0.9f, 35f, 37);
        }
        return throwClip;
    }

    static AudioClip MakeClip(string clipName, float length, float freq, float noiseMix, float decay, int seed)
    {
        int rate = 44100;
        int samples = (int)(rate * length);
        float[] data = new float[samples];
        System.Random rng = new System.Random(seed);

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-decay * t);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            data[i] = (tone * (1f - noiseMix) + noise * noiseMix) * env * 0.8f;
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}