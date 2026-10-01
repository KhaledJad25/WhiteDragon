using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Floating world-space damage number that rises and fades.</summary>
    public class DamageNumber : MonoBehaviour
    {
        const float Lifetime = 0.8f;
        const float RiseSpeed = 1.5f;

        TextMesh text;
        Color color;
        Vector3 drift;
        float age;

        public static void Spawn(Vector3 position, float amount, Color color)
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("DamageNumber");
            go.transform.position = position + Vector3.up * 0.3f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            tm.text = Mathf.Approximately(amount, Mathf.Round(amount)) ? amount.ToString("0") : amount.ToString("0.#");
            tm.fontSize = 48;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;

            var dn = go.AddComponent<DamageNumber>();
            dn.text = tm;
            dn.color = color;
            dn.drift = new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f);
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            transform.position += (Vector3.up * RiseSpeed + drift) * Time.unscaledDeltaTime;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
            color.a = Mathf.Clamp01(1f - age / Lifetime);
            text.color = color;
            if (age >= Lifetime) Destroy(gameObject);
        }
    }
}
