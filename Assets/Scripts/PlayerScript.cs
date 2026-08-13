using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Control Parameters")]
    private CharacterController controller;

    [Header("Movement Parameters")]
    private float gravity = -9.81f;
    public bool disableGravity;
    [SerializeField] private float mouseSensitivity = 150f;
    [SerializeField] private float speed = 6f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 2.5f;
    private float lastVerticalVelocity;
    private bool wasGrounded;
    private float xRotation = 0f;
    private float verticalVelocity;
    [SerializeField] float impactMultiplier = 0.02f;
    [SerializeField] float impactReturnSpeed = 10f;

    [Header("Dash Parameters")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashDuration = 0.75f;
    [SerializeField] private float dashCooldown = 1.5f;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    [Header("Grappling Parameters")]
    [HideInInspector] public bool isGrappling;

    [Header("Camera Parameters")]
    public Transform cameraTransform;
    private Vector3 cameraOffset;
    private Vector3 cameraVelocity;

    [Header("FOV Effects")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float normalFOV = 75f;
    [SerializeField] private float runFOV = 85f;
    [SerializeField] private float dashFOV = 110f;
    [SerializeField] private float grappleFOV = 100f;
    [SerializeField] private float fovSmoothSpeed = 8f;

    [Header("Gameplay Config")]
    private float purpleLevel = 0.0f;
    private float yellowLevel = 0.0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        MouseLook();
        if (!isDashing)
        {
           Move(); 
        }
        ApplyGravityAndJump();
        HandleDash();
        UpdateFOV();
    }

    void LateUpdate()
    {
        // LateUpdate for the camera landing effect.
        cameraOffset = Vector3.SmoothDamp(
            cameraOffset,
            Vector3.zero,
            ref cameraVelocity,
            1f / impactReturnSpeed
        );

        cameraTransform.localPosition = cameraOffset;
    }

    void MouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void Move()
    {
        float actualSpeed;
        float x = Input.GetAxis("Horizontal"); // A/D
        float z = Input.GetAxis("Vertical");   // W/S

        if (Input.GetKey(KeyCode.LeftShift))
        {
            actualSpeed = runSpeed;
        }
        else
        {
            actualSpeed = speed;
        }

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * actualSpeed * Time.deltaTime);
    }

    void UpdateFOV()
{
    float targetFOV = normalFOV;

    if (isGrappling)
    {
        targetFOV = grappleFOV;
    }
    else if (isDashing)
    {
        targetFOV = dashFOV;
    }
    else if (Input.GetKey(KeyCode.LeftShift))
    {
        targetFOV = runFOV;
    }

    playerCamera.fieldOfView = Mathf.Lerp(
        playerCamera.fieldOfView,
        targetFOV,
        fovSmoothSpeed * Time.deltaTime
    );
}
    void HandleDash()
    {
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // Iniciar dash
        if (Input.GetKeyDown(KeyCode.Q) && dashCooldownTimer <= 0f && !isDashing)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            dashDirection = cameraTransform.forward.normalized;
        }

        // Executar dash
        if (isDashing)
        {
            float dashProgress = dashTimer / dashDuration;
            float speed = dashSpeed * dashProgress;

            controller.Move(dashDirection * speed * Time.deltaTime);

            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }
    }

    void ApplyGravityAndJump()
    {
        bool grounded = IsGrounded();

        // Detecta impacto real no chão
        if (grounded && !wasGrounded)
        {
            OnLand(lastVerticalVelocity);
            verticalVelocity = 0f;
        }

        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Gravity only works when isn't fully on ground
        if (!grounded)
        {
            if (!isDashing && !disableGravity)
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
            else
            {
                verticalVelocity = 0f;
            }
        }

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        lastVerticalVelocity = verticalVelocity;
        wasGrounded = grounded;
    }

    bool IsGrounded()
    {
        float radius = controller.radius * 0.9f;
        float distance = (controller.height / 2f) + 0.3f;

        /* 
        #################################################
        Explanation:
        Ground Detection doesn't directly detect the ground beneath the player, but rather creates a small sphere beneath them to detect any surface below. This allows for accurate collision detection on ramps.
        ###################################################
        */
        return Physics.SphereCast(
            transform.position,
            radius,
            Vector3.down,
            out RaycastHit hit,
            distance
        );
    }

    void OnLand(float impactVelocity)
    {
        float impact = Mathf.Abs(impactVelocity);

        if (impact < 5f) return; // ignora quedas pequenas

        float offset = Mathf.Clamp(impact * impactMultiplier, 0.05f, 0.3f);
        cameraOffset.y -= offset;
    }
}
