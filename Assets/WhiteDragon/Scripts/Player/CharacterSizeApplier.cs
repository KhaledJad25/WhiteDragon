using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Applies the CharacterSize stat to the collider and eye height. Never scales the transform.</summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerStats))]
    public class CharacterSizeApplier : MonoBehaviour
    {
        public Transform head;
        public float baseHeight = 1.8f;
        public float baseRadius = 0.35f;
        [Tooltip("Eye height as a fraction of collider height.")]
        public float eyeRatio = 0.9f;
        public float minSize = 0.5f;
        public float maxSize = 2f;

        CharacterController controller;
        PlayerStats stats;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            stats = GetComponent<PlayerStats>();
            if (head == null) head = transform.Find("Head");
        }

        void OnEnable()
        {
            stats.Stats.Changed += OnStatChanged;
            Apply();
        }

        void OnDisable() => stats.Stats.Changed -= OnStatChanged;

        void OnStatChanged(StatType stat)
        {
            if (stat == StatType.CharacterSize) Apply();
        }

        public void Apply()
        {
            float size = Mathf.Clamp(stats.Stats.Get(StatType.CharacterSize), minSize, maxSize);
            float height = baseHeight * size;
            controller.height = height;
            controller.radius = baseRadius * size;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            if (head != null) head.localPosition = new Vector3(0f, height * eyeRatio, 0f);
        }
    }
}
