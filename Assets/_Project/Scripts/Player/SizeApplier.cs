using UnityEngine;

// Turns the CharacterSize stat into collider dimensions and eye height.
// We do NOT scale the transform, which keeps CharacterController and camera behavior predictable.
[RequireComponent(typeof(CharacterController), typeof(PlayerStats))]
public class SizeApplier : MonoBehaviour
{
    [SerializeField] float baseHeight = 1.8f;
    [SerializeField] float baseRadius = 0.35f;
    [SerializeField] float eyeRatio = 0.9f;
    [SerializeField] float minSize = 0.5f;
    [SerializeField] float maxSize = 2.0f;

    CharacterController cc;
    PlayerStats playerStats;

    public float Size { get; private set; } = 1f;
    public float EyeHeight { get; private set; } = 1.62f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerStats = GetComponent<PlayerStats>();
    }

    void OnEnable() { playerStats.Stats.Changed += OnStatChanged; }
    void OnDisable() { playerStats.Stats.Changed -= OnStatChanged; }
    void Start() { Apply(); }

    void OnStatChanged(StatType t) { if (t == StatType.CharacterSize) Apply(); }

    public void Apply()
    {
        Size = Mathf.Clamp(playerStats.Stats.Get(StatType.CharacterSize), minSize, maxSize);

        float h = baseHeight * Size;
        cc.height = h;
        cc.radius = baseRadius * Size;
        cc.center = new Vector3(0f, h * 0.5f, 0f);
        EyeHeight = h * eyeRatio;
    }
}