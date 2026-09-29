using UnityEngine;
using UnityEngine.InputSystem;

// First-person controller. Keyboard and mouse. Speed and jump come from the StatBlock.
[RequireComponent(typeof(CharacterController), typeof(PlayerStats), typeof(SizeApplier))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraRoot;
    [SerializeField] Camera cam;

    [Header("Look")]
    [SerializeField] float sensitivity = 0.1f;
    [SerializeField] float pitchLimit = 85f;

    [Header("Movement feel")]
    [SerializeField] float acceleration = 40f;
    [SerializeField] float deceleration = 50f;
    [SerializeField] float gravity = -25f;
    [SerializeField] float coyoteTime = 0.1f;

    [Header("Comfort options")]
    public bool headBob = true;
    [Range(60, 110)] public float fov = 90f;
    [SerializeField] float bobAmount = 0.04f;
    [SerializeField] float bobFrequency = 1.6f;

    CharacterController cc;
    PlayerStats playerStats;
    SizeApplier size;

    InputAction move, look, jump;
    Vector3 horizontalVel;
    float verticalVel, pitch, bobTimer, lastGroundedTime;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerStats = GetComponent<PlayerStats>();
        size = GetComponent<SizeApplier>();

        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

        look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
    }

    void OnEnable()
    {
        move.Enable(); look.Enable(); jump.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        move.Disable(); look.Disable(); jump.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (Cursor.lockState == CursorLockMode.Locked) HandleLook();
        HandleMove();
        UpdateCamera();
    }

    void HandleLook()
    {
        Vector2 d = look.ReadValue<Vector2>() * sensitivity;
        transform.Rotate(0f, d.x, 0f);
        pitch = Mathf.Clamp(pitch - d.y, -pitchLimit, pitchLimit);
        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMove()
    {
        var s = playerStats.Stats;
        float speed = s.Get(StatType.MoveSpeed);
        float jumpHeight = s.Get(StatType.JumpHeight);

        Vector2 input = move.ReadValue<Vector2>();
        Vector3 wish = transform.right * input.x + transform.forward * input.y;
        if (wish.sqrMagnitude > 1f) wish.Normalize();
        Vector3 target = wish * speed;

        float rate = input.sqrMagnitude > 0.01f ? acceleration : deceleration;
        horizontalVel = Vector3.MoveTowards(horizontalVel, target, rate * Time.deltaTime);

        if (cc.isGrounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVel < 0f) verticalVel = -2f;
        }

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (jump.WasPressedThisFrame() && canJump)
        {
            verticalVel = Mathf.Sqrt(2f * -gravity * jumpHeight);
            lastGroundedTime = -999f;
        }

        verticalVel += gravity * Time.deltaTime;

        Vector3 velocity = horizontalVel + Vector3.up * verticalVel;
        cc.Move(velocity * Time.deltaTime);

        if (cc.isGrounded) bobTimer += horizontalVel.magnitude * Time.deltaTime * bobFrequency * Mathf.PI * 2f;
    }

    void UpdateCamera()
    {
        float bob = 0f;
        if (headBob && cc.isGrounded)
        {
            float t = Mathf.Clamp01(horizontalVel.magnitude / Mathf.Max(0.01f, playerStats.Stats.Get(StatType.MoveSpeed)));
            bob = Mathf.Sin(bobTimer) * bobAmount * t * size.Size;
        }
        cameraRoot.localPosition = new Vector3(0f, size.EyeHeight + bob, 0f);
        cam.fieldOfView = fov;
    }
}