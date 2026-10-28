using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonCrouchController : MonoBehaviour
{
    public Transform cameraPivot;
    public Transform bodyVisual;

    public float walkSpeed = 4f;
    public float crouchSpeed = 1.8f;
    public float acceleration = 12f;
    public float gravity = -20f;
    public float speedMultiplier = 1f;
    public float swayStrength = 0f;

    public float jumpHeight = 0.8f;
    public float coyoteTime = 0.1f;
    public float groundCheckDistance = 0.25f;

    public float mouseSensitivity = 2f;
    public float maxPitch = 85f;

    public float standHeight = 1.8f;
    public float crouchHeight = 0.9f;
    public float crouchLerpSpeed = 10f;
    public float eyeOffset = 0.15f;

    public bool IsCrouching { get; private set; }
    public bool StandBlocked { get; private set; }
    public bool IsGrounded { get; private set; }

    CharacterController controller;
    float pitch;
    float verticalVelocity;
    float coyoteTimer;
    Vector3 horizontalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        ApplyHeight(standHeight);
        LockCursor(true);
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            LockCursor(true);

        if (Cursor.lockState == CursorLockMode.Locked)
            HandleLook();

        HandleCrouch();
        HandleMovement();
    }

    void HandleLook()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mx, 0f);
        pitch = Mathf.Clamp(pitch - my, -maxPitch, maxPitch);
        if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void HandleCrouch()
    {
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl) ||
                          Input.GetKey(KeyCode.RightControl) ||
                          Input.GetKey(KeyCode.C);

        StandBlocked = false;
        if (wantCrouch)
            IsCrouching = true;
        else if (IsCrouching)
        {
            if (CanStandUp()) IsCrouching = false;
            else StandBlocked = true;
        }

        float target = IsCrouching ? crouchHeight : standHeight;
        float h = Mathf.Lerp(controller.height, target, Time.deltaTime * crouchLerpSpeed);
        if (Mathf.Abs(h - target) < 0.005f) h = target;
        ApplyHeight(h);
    }

    bool CanStandUp()
    {
        float r = controller.radius;
        Vector3 origin = transform.position + Vector3.up * (controller.height - r);
        float distance = standHeight - controller.height + 0.02f;
        return !Physics.SphereCast(origin, r * 0.9f, Vector3.up, out _, distance,
                                   ~0, QueryTriggerInteraction.Ignore);
    }

    void ApplyHeight(float h)
    {
        controller.height = h;
        controller.center = new Vector3(0f, h * 0.5f, 0f);

        if (cameraPivot)
            cameraPivot.localPosition = new Vector3(0f, h - eyeOffset, 0f);

        if (bodyVisual)
        {
            bodyVisual.localScale = new Vector3(controller.radius * 2f, h * 0.5f, controller.radius * 2f);
            bodyVisual.localPosition = new Vector3(0f, h * 0.5f, 0f);
        }
    }

    bool CheckGround()
    {
        if (verticalVelocity > 0.1f) return false;
        if (controller.isGrounded) return true;

        Vector3 origin = transform.position + Vector3.up * (controller.radius + 0.05f);
        return Physics.SphereCast(origin, controller.radius * 0.9f, Vector3.down, out _,
                                  groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    void HandleMovement()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        float speed = (IsCrouching ? crouchSpeed : walkSpeed) * speedMultiplier;
        Vector3 targetVel = transform.TransformDirection(new Vector3(input.x, 0f, input.y)) * speed;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVel, acceleration * Time.deltaTime);

        IsGrounded = CheckGround();
        if (IsGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        if (IsGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

        if (Input.GetKeyDown(KeyCode.Space) && coyoteTimer > 0f && !IsCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimer = 0f;
            IsGrounded = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 sway = Vector3.zero;
        if (swayStrength > 0f)
        {
            float t = Time.time * 3f;
            sway = new Vector3(Mathf.PerlinNoise(t, 0.5f) - 0.5f, 0f, Mathf.PerlinNoise(0.5f, t) - 0.5f) * 2f * swayStrength;
        }

        Vector3 velocity = horizontalVelocity + sway + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }
}
