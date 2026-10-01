using UnityEngine;

namespace WhiteDragon
{
    /// <summary>First-person movement, mouse look, jump with coyote time, and Esc cursor toggle.</summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        public Transform head;
        public float acceleration = 40f;
        public float deceleration = 50f;
        [Tooltip("Degrees per pixel of mouse movement.")]
        public float mouseSensitivity = 0.1f;
        public float pitchClamp = 85f;
        public float gravity = -25f;
        public float coyoteTime = 0.1f;

        CharacterController controller;
        PlayerStats stats;
        float yaw, pitch;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        float lastGroundedTime = float.NegativeInfinity;

        public Vector3 HorizontalVelocity => horizontalVelocity;
        public bool IsGrounded => controller != null && controller.isGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            stats = GetComponent<PlayerStats>();
            if (head == null) head = transform.Find("Head");
            yaw = transform.eulerAngles.y;
        }

        void Start() => CursorState.SetLocked(true);

        void Update()
        {
            if (GameInput.ToggleCursor.WasPressedThisFrame())
                CursorState.SetLocked(!CursorState.Locked);

            bool hasControl = CursorState.Locked;
            float dt = Time.deltaTime;

            if (hasControl)
            {
                Vector2 look = GameInput.Look.ReadValue<Vector2>();
                yaw += look.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -pitchClamp, pitchClamp);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (head != null) head.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector2 input = hasControl ? GameInput.Move.ReadValue<Vector2>() : Vector2.zero;
            Vector3 wish = Vector3.ClampMagnitude(transform.right * input.x + transform.forward * input.y, 1f)
                           * stats.Stats.Get(StatType.MoveSpeed);
            float rate = wish.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wish, rate * dt);

            if (controller.isGrounded)
            {
                lastGroundedTime = Time.time;
                if (verticalVelocity < 0f) verticalVelocity = -2f;
            }

            if (hasControl && GameInput.Jump.WasPressedThisFrame() && Time.time - lastGroundedTime <= coyoteTime)
            {
                float height = Mathf.Max(0f, stats.Stats.Get(StatType.JumpHeight));
                verticalVelocity = Mathf.Sqrt(2f * -gravity * height);
                lastGroundedTime = float.NegativeInfinity;
            }

            verticalVelocity += gravity * dt;
            var flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
        }
    }
}
