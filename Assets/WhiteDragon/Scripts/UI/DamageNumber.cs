using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Floating world-space damage number that rises and fades. Pooled and capped (GameFeel.MaxDamageNumbers):
    /// when full, the oldest is reused. Hits on the same target within MergeWindow add to one number.
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        const float Lifetime = 0.8f;
        const float RiseSpeed = 1.5f;
        public const float MergeWindow = 0.15f;
        const int MaxCachedTexts = 4096;

        static readonly List<DamageNumber> active = new List<DamageNumber>();
        static readonly Stack<DamageNumber> pool = new Stack<DamageNumber>();
        static readonly Dictionary<int, string> textCache = new Dictionary<int, string>();

        /// <summary>Lets tests spawn numbers outside play mode.</summary>
        public static bool RunInEditMode;

        TextMesh text;
        Color color;
        Vector3 drift;
        float age;
        float amount;
        object key;

        /// <summary>Damage numbers on screen (debug/stress readout).</summary>
        public static int LiveCount => active.Count;
        public float Amount => amount;
        public bool IsShowing => gameObject.activeSelf;

        /// <summary>Shows amount at position. key (usually the target) lets quick repeat hits merge into one number.</summary>
        public static DamageNumber Spawn(Vector3 position, float amount, Color color, object key = null)
        {
            if (!Application.isPlaying && !RunInEditMode) return null;

            if (key != null)
                foreach (var a in active)
                    if (a.key == key && a.age < MergeWindow)
                    {
                        a.amount += amount;
                        a.text.text = Format(a.amount);
                        return a;
                    }

            DamageNumber n;
            if (active.Count >= GameFeel.MaxDamageNumbers)
            {
                n = Oldest();
                active.Remove(n);
            }
            else
            {
                n = TakeFromPool() ?? Create();
            }

            n.transform.position = position + Vector3.up * 0.3f;
            n.amount = amount;
            n.key = key;
            n.age = 0f;
            n.color = color;
            n.drift = new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f);
            n.text.text = Format(amount);
            TextMeshColor.Set(n.text, color);
            n.gameObject.SetActive(true);
            active.Add(n);
            return n;
        }

        static DamageNumber Create()
        {
            var go = new GameObject("DamageNumber");
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            tm.fontSize = 48;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontStyle = FontStyle.Bold;
            var n = go.AddComponent<DamageNumber>();
            n.text = tm;
            return n;
        }

        static DamageNumber TakeFromPool()
        {
            while (pool.Count > 0)
            {
                var n = pool.Pop();
                if (n != null) return n;
            }
            return null;
        }

        static DamageNumber Oldest()
        {
            DamageNumber oldest = active[0];
            foreach (var a in active)
                if (a.age > oldest.age) oldest = a;
            return oldest;
        }

        static string Format(float value)
        {
            int k = Mathf.RoundToInt(value * 10f);
            if (textCache.TryGetValue(k, out var s)) return s;
            s = Mathf.Approximately(value, Mathf.Round(value)) ? value.ToString("0") : value.ToString("0.#");
            if (textCache.Count < MaxCachedTexts) textCache[k] = s;
            return s;
        }

        void Update() => Tick(Time.unscaledDeltaTime);

        public void Tick(float dt)
        {
            age += dt;
            transform.position += (Vector3.up * RiseSpeed + drift) * dt;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
            color.a = Mathf.Clamp01(1f - age / Lifetime);
            TextMeshColor.Set(text, color);
            if (age >= Lifetime) Release();
        }

        void Release()
        {
            active.Remove(this);
            key = null;
            gameObject.SetActive(false);
            pool.Push(this);
        }

        void OnDestroy() => active.Remove(this);

        /// <summary>Destroys pooled and active numbers (tests and scene cleanup).</summary>
        public static void ClearAll()
        {
            foreach (var n in active.ToArray()) if (n != null) DestroySafe(n.gameObject);
            while (pool.Count > 0)
            {
                var n = pool.Pop();
                if (n != null) DestroySafe(n.gameObject);
            }
            active.Clear();
        }

        static void DestroySafe(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            pool.Clear();
            textCache.Clear();
            RunInEditMode = false;
        }
    }
}
