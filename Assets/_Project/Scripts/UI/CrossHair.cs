using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [SerializeField] float size = 4f;
    [SerializeField] float outline = 1f;
    [SerializeField] Color color = Color.white;

    static Texture2D pixel;

    void OnGUI()
    {
        if (pixel == null)
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        float o = size + outline * 2f;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(cx - o * 0.5f, cy - o * 0.5f, o, o), pixel);

        GUI.color = color;
        GUI.DrawTexture(new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size), pixel);
    }
}