using System.Collections.Generic;
using UnityEngine;

// Isaac-style half-heart HUD rendering Red containers, Soul hearts, and Dark hearts.
public class HeartHUD : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] Vector2 screenOffset = new Vector2(24f, 24f);
    [SerializeField] float heartSize = 32f;
    [SerializeField] float heartSpacing = 4f;
    [SerializeField] int heartsPerRow = 6;

    [Header("Visual Feedback")]
    [SerializeField] bool showLowHealthPulse = true;
    [SerializeField] bool showDamageVignette = true;

    [Header("Target")]
    [SerializeField] PlayerHealth playerHealth;

    // Procedural heart textures (16x16 pixel art)
    Texture2D texContainerEmpty;
    Texture2D texRedFull;
    Texture2D texRedHalf;
    Texture2D texSoulFull;
    Texture2D texSoulHalf;
    Texture2D texDarkFull;
    Texture2D texDarkHalf;
    Texture2D texVignette;

    float pulseTimer;
    float currentPulseScale = 1f;

    static readonly string[] Mask = new string[]
    {
        "................", // 0
        ".......OO.......", // 1
        "......OFFO......", // 2
        ".....OFFFFO.....", // 3
        "....OFFFFFFO....", // 4
        "...OFFFFFFFFO...", // 5
        "..OFFFFFFFFFFO..", // 6
        ".OFFFFFFFFFFFFO.", // 7
        ".OFFFFFFFFFFFFO.", // 8
        "OFFFFFFFFFFFFFFO", // 9
        "OHHFFFFFFFFFFFFO", // 10
        "OHHFFFFFFFFFFFFO", // 11
        ".OFFFOOOOOOFFFO.", // 12
        ".OFFO......OFFO.", // 13
        "..OO........OO..", // 14
        "................"  // 15
    };

    void Awake()
    {
        GenerateTextures();
    }

    void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (playerHealth != null && playerHealth.Health != null)
        {
            playerHealth.Health.Changed += OnHealthChanged;
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null && playerHealth.Health != null)
        {
            playerHealth.Health.Changed -= OnHealthChanged;
        }
    }

    void OnHealthChanged()
    {
        pulseTimer = 0.25f;
    }

    void Update()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth != null && playerHealth.Health != null)
            {
                playerHealth.Health.Changed += OnHealthChanged;
            }
        }

        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.deltaTime;
            currentPulseScale = 1f + 0.25f * Mathf.Clamp01(pulseTimer / 0.25f);
        }
        else
        {
            currentPulseScale = 1f;
        }
    }

    void OnGUI()
    {
        if (playerHealth == null || playerHealth.Health == null)
        {
            return;
        }

        DrawVignette();
        DrawHearts();
        DrawDeathOverlay();
    }

    void DrawVignette()
    {
        if (!showDamageVignette || !playerHealth.IsInvulnerable)
        {
            return;
        }

        float alpha = Mathf.Clamp01(playerHealth.InvulnerableTimer / 1.0f) * 0.35f;
        GUI.color = new Color(0.8f, 0.05f, 0.05f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texVignette);
        GUI.color = Color.white;
    }

    void DrawHearts()
    {
        HealthState health = playerHealth.Health;
        int totalRedContainers = (health.RedContainers + 1) / 2;
        int redHalfCurrent = health.RedCurrent;

        int slotIndex = 0;

        // Low health heartbeat
        float lowHealthScale = 1f;
        if (showLowHealthPulse && health.RedCurrent <= 2 && health.Overlay.Count == 0 && !playerHealth.IsDead)
        {
            lowHealthScale = 1f + Mathf.Sin(Time.time * 8f) * 0.1f;
        }

        float totalScale = currentPulseScale * lowHealthScale;
        float renderSize = heartSize * totalScale;
        float centerOffset = (renderSize - heartSize) * 0.5f;

        // 1. Draw Red Heart Containers
        for (int i = 0; i < totalRedContainers; i++)
        {
            GetSlotPosition(slotIndex, out float x, out float y);
            Rect slotRect = new Rect(x - centerOffset, y - centerOffset, renderSize, renderSize);

            // Container background
            GUI.color = Color.white;
            GUI.DrawTexture(slotRect, texContainerEmpty);

            // Red heart fill
            int containerHalfIndex = i * 2;
            if (redHalfCurrent >= containerHalfIndex + 2)
            {
                GUI.DrawTexture(slotRect, texRedFull);
            }
            else if (redHalfCurrent == containerHalfIndex + 1)
            {
                GUI.DrawTexture(slotRect, texRedHalf);
            }

            slotIndex++;
        }

        // 2. Draw Overlay Hearts (Soul & Dark)
        List<HeartType> overlay = health.Overlay;
        int overlayIndex = 0;
        while (overlayIndex < overlay.Count)
        {
            GetSlotPosition(slotIndex, out float x, out float y);
            Rect slotRect = new Rect(x - centerOffset, y - centerOffset, renderSize, renderSize);

            HeartType type = overlay[overlayIndex];

            // Check if the next overlay heart is of the same type to pair as a full heart
            bool isPair = (overlayIndex + 1 < overlay.Count) && (overlay[overlayIndex + 1] == type);

            GUI.color = Color.white;
            if (type == HeartType.Soul)
            {
                GUI.DrawTexture(slotRect, isPair ? texSoulFull : texSoulHalf);
            }
            else if (type == HeartType.Dark)
            {
                GUI.DrawTexture(slotRect, isPair ? texDarkFull : texDarkHalf);
            }

            overlayIndex += isPair ? 2 : 1;
            slotIndex++;
        }
    }

    void GetSlotPosition(int slotIndex, out float x, out float y)
    {
        int col = slotIndex % heartsPerRow;
        int row = slotIndex / heartsPerRow;
        x = screenOffset.x + col * (heartSize + heartSpacing);
        y = screenOffset.y + row * (heartSize + heartSpacing);
    }

    void DrawDeathOverlay()
    {
        if (!playerHealth.IsDead)
        {
            return;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texVignette);

        GUI.color = new Color(0.9f, 0.15f, 0.15f, 1f);
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter
        };

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        GUI.Label(new Rect(cx - 200, cy - 50, 400, 50), "YOU DIED", titleStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(cx - 200, cy + 10, 400, 30), "Open F1 Debug to Revive or Restart", subStyle);
    }

    void GenerateTextures()
    {
        // Colors
        Color outlineColor = new Color(0.12f, 0.10f, 0.14f, 1f);
        Color containerBg = new Color(0.10f, 0.08f, 0.12f, 0.75f);

        // Red Heart
        Color redFill = new Color(0.76f, 0.10f, 0.10f, 1f);
        Color redHighlight = new Color(1.0f, 0.45f, 0.45f, 1f);

        // Soul Heart
        Color soulOutline = new Color(0.08f, 0.18f, 0.26f, 1f);
        Color soulFill = new Color(0.32f, 0.68f, 0.88f, 1f);
        Color soulHighlight = new Color(0.75f, 0.92f, 1.0f, 1f);

        // Dark Heart
        Color darkOutline = new Color(0.06f, 0.04f, 0.08f, 1f);
        Color darkFill = new Color(0.18f, 0.12f, 0.22f, 1f);
        Color darkHighlight = new Color(0.58f, 0.24f, 0.68f, 1f);

        texContainerEmpty = CreateHeartTexture(outlineColor, containerBg, containerBg, false, true);

        texRedFull = CreateHeartTexture(outlineColor, redFill, redHighlight, false, false);
        texRedHalf = CreateHeartTexture(outlineColor, redFill, redHighlight, true, false);

        texSoulFull = CreateHeartTexture(soulOutline, soulFill, soulHighlight, false, false);
        texSoulHalf = CreateHeartTexture(soulOutline, soulFill, soulHighlight, true, false);

        texDarkFull = CreateHeartTexture(darkOutline, darkFill, darkHighlight, false, false);
        texDarkHalf = CreateHeartTexture(darkOutline, darkFill, darkHighlight, true, false);

        texVignette = new Texture2D(1, 1);
        texVignette.SetPixel(0, 0, Color.white);
        texVignette.Apply();
    }

    Texture2D CreateHeartTexture(Color outline, Color fill, Color highlight, bool halfOnly, bool emptyContainer)
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            string row = Mask[size - 1 - y];
            for (int x = 0; x < size; x++)
            {
                char c = row[x];
                Color pixelColor = Color.clear;

                if (c == 'O')
                {
                    pixelColor = outline;
                }
                else if (c == 'F' || c == 'H')
                {
                    if (emptyContainer)
                    {
                        pixelColor = fill;
                    }
                    else if (halfOnly && x >= 8)
                    {
                        pixelColor = Color.clear;
                    }
                    else
                    {
                        pixelColor = (c == 'H') ? highlight : fill;
                    }
                }

                // If drawing half-heart fill, add an inner vertical seam at col 7
                if (halfOnly && !emptyContainer && x == 7 && (c == 'F' || c == 'H'))
                {
                    pixelColor = Color.Lerp(pixelColor, outline, 0.5f);
                }

                tex.SetPixel(x, y, pixelColor);
            }
        }

        tex.Apply();
        return tex;
    }
}
