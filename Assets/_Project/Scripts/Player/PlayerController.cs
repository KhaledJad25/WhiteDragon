using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] float bobAmount = 0.02f;
    [SerializeField] float bobFrequency = 0.4f;
    [SerializeField] float bobSmoothing = 15f;

    CharacterController cc;
    PlayerStats playerStats;
    SizeApplier size;

    InputAction move;
    InputAction look;
    InputAction jump;

    Vector3 horizontalVel;
    float verticalVel;
    float pitch;
    float bobTimer;
    float bobOffset;
    float lastGroundedTime;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerStats = GetComponent<PlayerStats>();
        size = GetComponent<SizeApplier>();

        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
    }

    void OnEnable()
    {
        move.Enable();
        look.Enable();
        jump.Enable();
        LockCursor(true);
    }

    void OnDisable()
    {
        move.Disable();
        look.Disable();
        jump.Disable();
        LockCursor(false);
    }

    void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
        Cursor.visible = !locked;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LockCursor(Cursor.lockState != CursorLockMode.Locked);
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HandleLook();
        }

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
        StatBlock s = playerStats.Stats;
        float speed = s.Get(StatType.MoveSpeed);
        float jumpHeight = s.Get(StatType.JumpHeight);

        Vector2 input = move.ReadValue<Vector2>();
        Vector3 wish = transform.right * input.x + transform.forward * input.y;
        if (wish.sqrMagnitude > 1f)
        {
            wish.Normalize();
        }
        Vector3 target = wish * speed;

        float rate = deceleration;
        if (input.sqrMagnitude > 0.01f)
        {
            rate = acceleration;
        }
        horizontalVel = Vector3.MoveTowards(horizontalVel, target, rate * Time.deltaTime);

        if (cc.isGrounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVel < 0f)
            {
                verticalVel = -2f;
            }
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

        if (cc.isGrounded)
        {
            bobTimer += horizontalVel.magnitude * Time.deltaTime * bobFrequency * Mathf.PI * 2f;
        }
    }

    void UpdateCamera()
    {
        float targetBob = 0f;
        if (headBob && cc.isGrounded)
        {
            float maxSpeed = Mathf.Max(0.01f, playerStats.Stats.Get(StatType.MoveSpeed));
            float t = Mathf.Clamp01(horizontalVel.magnitude / maxSpeed);
            targetBob = Mathf.Sin(bobTimer) * bobAmount * t * size.Size;
        }

        float blend = 1f - Mathf.Exp(-bobSmoothing * Time.deltaTime);
        bobOffset = Mathf.Lerp(bobOffset, targetBob, blend);

        cameraRoot.localPosition = new Vector3(0f, size.EyeHeight + bobOffset, 0f);
        cam.fieldOfView = fov;
    }
}