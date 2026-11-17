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

    public bool allowJump = true;
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

    readonly Collider[] ceilingHits = new Collider[12];
    readonly RaycastHit[] groundHits = new RaycastHit[8];
    CharacterController controller;
    float pitch;
    float verticalVelocity;
    float coyoteTimer;
    Vector3 horizontalVelocity;

    void Awake() => controller = GetComponent<CharacterController>();

    void Start()
    {
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

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var prop = hit.collider.GetComponentInParent<FallingProp>();
        if (prop) prop.HitController(hit);
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

        // A controller can move under a new obstruction while the capsule is still expanding.
        StandBlocked = !wantCrouch && controller.height < standHeight - .005f && !CanStandUp();
        IsCrouching = wantCrouch || StandBlocked;

        float target = IsCrouching ? crouchHeight : standHeight;
        float height = Mathf.Lerp(controller.height, target, Time.deltaTime * crouchLerpSpeed);
        if (Mathf.Abs(height - target) < .005f) height = target;
        if (height != controller.height) ApplyHeight(height);
    }

    bool CanStandUp()
    {
        float radius = controller.radius;
        float queryRadius = radius - Mathf.Min(controller.skinWidth * .5f, radius * .05f);
        Vector3 top = transform.position + Vector3.up * (controller.height - radius);
        Vector3 standingTop = transform.position + Vector3.up * (standHeight - queryRadius);
        int hits = Physics.OverlapCapsuleNonAlloc(top, standingTop, queryRadius, ceilingHits,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        if (hits == ceilingHits.Length) return false;

        for (int i = 0; i < hits; i++)
        {
            if (!ceilingHits[i].transform.IsChildOf(transform)) return false;
        }

        return true;
    }

    void ApplyHeight(float height)
    {
        controller.height = height;
        controller.center = new Vector3(0f, height * .5f, 0f);

        if (cameraPivot)
            cameraPivot.localPosition = new Vector3(0f, height - eyeOffset, 0f);

        if (bodyVisual)
        {
            bodyVisual.localScale = new Vector3(controller.radius * 2f, height * .5f, controller.radius * 2f);
            bodyVisual.localPosition = new Vector3(0f, height * .5f, 0f);
        }
    }

    void HandleMovement()
    {
        float delta = Time.deltaTime;
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        float speed = (IsCrouching ? crouchSpeed : walkSpeed) * speedMultiplier;
        Vector3 desiredVelocity = transform.TransformDirection(new Vector3(input.x, 0f, input.y)) * speed;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, desiredVelocity, acceleration * delta);

        bool wasGrounded = IsGrounded;
        coyoteTimer = wasGrounded ? coyoteTime : coyoteTimer - delta;
        if (wasGrounded && verticalVelocity < 0f) verticalVelocity = -2f;

        bool jump = allowJump && Input.GetKeyDown(KeyCode.Space) && coyoteTimer > 0f && !IsCrouching;
        if (jump)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimer = 0f;
        }

        verticalVelocity += gravity * delta;
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        var collisions = controller.Move(velocity * delta);
        if (!enabled) return;

        if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        IsGrounded = (collisions & CollisionFlags.Below) != 0;
        if (!IsGrounded && wasGrounded && !jump) SnapDownStep();
    }

    void SnapDownStep()
    {
        const float probeLift = .04f;
        float closest = Mathf.Min(groundCheckDistance, controller.stepOffset) + probeLift;
        int hits = Physics.RaycastNonAlloc(transform.position + Vector3.up * probeLift,
            Vector3.down, groundHits, closest, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        float walkable = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
        bool found = false;
        for (int i = 0; i < hits; i++)
        {
            var hit = groundHits[i];
            if (hit.transform.IsChildOf(transform) || hit.normal.y < walkable || hit.distance > closest) continue;
            closest = hit.distance;
            found = true;
        }

        if (!found) return;
        float step = Mathf.Max(.01f, closest - probeLift + controller.skinWidth);
        var collisions = controller.Move(Vector3.down * step);
        IsGrounded = (collisions & CollisionFlags.Below) != 0;
    }
}
