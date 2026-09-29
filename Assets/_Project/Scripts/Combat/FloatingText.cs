using UnityEngine;

public class FloatingText : MonoBehaviour
{
    const float Life = 0.8f;
    float t;

    public static void Spawn(Vector3 position, string text)
    {
        GameObject go = new GameObject("DamageNumber");
        go.transform.position = position;

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.text = text;
        tm.fontSize = 48;
        tm.characterSize = 0.05f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        mr.material = tm.font.material;

        go.AddComponent<FloatingText>();
    }

    void Update()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * 1.2f * Time.deltaTime;

        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        if (t >= Life)
        {
            Destroy(gameObject);
        }
    }
}