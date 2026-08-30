using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    [Header("Control Parameters")]
    private Rigidbody rb;
    private CapsuleCollider capsule;

    [Header("Movement Parameters")]
    [SerializeField] private float gravityMultiplier = 1f;
    [SerializeField] private float mouseSensitivity = 150f;
    [SerializeField] private float speed = 6f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 2.5f;

    private float lastVerticalVelocity;
    private bool wasGrounded;
    private float xRotation = 0f;
    private float verticalVelocity;

    [SerializeField] private float impactMultiplier = 0.02f;
    [SerializeField] private float impactReturnSpeed = 10f;

    [Header("Crouch & Slide Parameters")]
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float slideStartSpeed = 16f;
    [SerializeField] private float slideMinSpeed = 5f;
    [SerializeField] private float slideDecayRate = 6.5f;

    private float originalHeight;
    private Vector3 originalCenter;
    private float originalCameraY;

    private bool isCrouched;
    private bool isSliding;
    private float currentSlideSpeed;
    private Vector3 slideDirection;

    [Header("Dash Parameters")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashDuration = 0.75f;
    [SerializeField] private float dashCooldown = 1.5f;

    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector3 dashDirection;
    private bool canDash = true;
    public bool IsDashing => isDashing;
    public float DashProgress01 => isDashing ? 1f - (dashTimer / dashDuration) : 0f;

    [Header("Grappling Parameters")]
    [HideInInspector] public bool isGrappling;
    [Header("Repel / Knockback")]
    [Tooltip("Tempo em que o Move() não sobrescreve a velocidade horizontal, para o impulso do repel não ser apagado no próximo FixedUpdate.")]
    [SerializeField] private float repelControlLockDuration = 0.2f;
    private float repelLockTimer = 0f;
    [Header("Camera Parameters")]
    public Transform cameraTransform;
    private Vector3 cameraOffset;
    private Vector3 cameraVelocity;

    [Header("Grapple Camera Lock")]
    [Tooltip("Velocidade com que a câmera gira até encarar a direção do puxão do grapple.")]
    [SerializeField] private float grappleLookSpeed = 12f;
    private Vector3 grappleLookDirection;

    [Header("Crouch Camera")]
    [SerializeField] private float crouchCameraOffset = 0.5f;

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

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundCheckRadiusMultiplier = 0.9f;
    [SerializeField] private float maxGroundAngle = 60f;
    [SerializeField] private LayerMask groundMask = ~0; // Configure no Inspector para excluir a layer do próprio Player
    [SerializeField] private float groundCheckSkin = 0.05f; // Folga extra pra evitar o caso de "toque exato" que o SphereCast não detecta

    [Header("Jump Feel (Buffer & Coyote Time)")]
    [SerializeField] private float jumpBufferTime = 0.15f; // quanto tempo um "pedido" de pulo fica guardado
    [SerializeField] private float coyoteTime = 0.15f;     // quanto tempo depois de sair do chão ainda dá pra pular

    private Vector3 movementInput;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (rb == null)
        {
            Debug.LogError("PlayerScript: Rigidbody não encontrado no jogador.");
            return;
        }

        if (capsule == null)
        {
            Debug.LogError("PlayerScript: CapsuleCollider não encontrado no jogador.");
            return;
        }

        // Configuração do Rigidbody
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Salva os valores originais
        originalHeight = capsule.height;
        originalCenter = capsule.center;
        originalCameraY = cameraTransform.localPosition.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Update()
    {
        MouseLook();

        HandleCrouchAndSlide();
        HandleDash();
        HandleJumpInput();
        UpdateFOV();
    }


    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (repelLockTimer > 0f)
            repelLockTimer -= Time.fixedDeltaTime;

        if (!isDashing && !isSliding && repelLockTimer <= 0f)
        {
            Move();
        }
        else if (isSliding)
        {
            ExecuteSlide();
        }

        ExecuteDash();

        UpdateGroundState();
    }

    void LateUpdate()
    {
        // Câmera
        float targetCamY = isCrouched
            ? originalCameraY - crouchCameraOffset
            : originalCameraY;

        Vector3 targetCameraPosition =
            new Vector3(
                cameraTransform.localPosition.x,
                targetCamY,
                cameraTransform.localPosition.z
            ) + cameraOffset;

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            targetCameraPosition,
            15f * Time.deltaTime
        );

        // Retorno suave do impacto da câmera
        cameraOffset = Vector3.SmoothDamp(
            cameraOffset,
            Vector3.zero,
            ref cameraVelocity,
            1f / impactReturnSpeed
        );
    }


    // =========================================================
    // CAMERA
    // =========================================================

    void MouseLook()
    {
        // Durante o grapple, a câmera não responde mais ao mouse:
        // ela é travada na direção do puxão (ver RotateTowardsGrappleDirection).
        if (isGrappling)
        {
            RotateTowardsGrappleDirection();
            return;
        }

        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation =
            Quaternion.Euler(xRotation, 0f, 0f);

        rb.MoveRotation(
            rb.rotation *
            Quaternion.Euler(0f, mouseX, 0f)
        );
    }


    // =========================================================
    // GRAPPLE CAMERA LOCK
    // =========================================================

    /// <summary>
    /// Chamado pelo HandScript enquanto o grapple está ativo, informando
    /// a direção atual do puxão (jogador -> ponto de gancho).
    /// </summary>
    public void SetGrappleDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.0001f)
        {
            grappleLookDirection = direction.normalized;
        }
    }

    void RotateTowardsGrappleDirection()
    {
        if (grappleLookDirection.sqrMagnitude < 0.0001f)
            return;

        // Rotação horizontal (yaw) do corpo do jogador
        Vector3 flatDir = new Vector3(grappleLookDirection.x, 0f, grappleLookDirection.z);

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(flatDir.normalized, Vector3.up);

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetYaw,
                    grappleLookSpeed * Time.deltaTime
                )
            );
        }

        // Rotação vertical (pitch) da câmera
        float targetPitch =
            -Mathf.Asin(Mathf.Clamp(grappleLookDirection.y, -1f, 1f)) *
            Mathf.Rad2Deg;

        targetPitch = Mathf.Clamp(targetPitch, -90f, 90f);

        xRotation = Mathf.Lerp(
            xRotation,
            targetPitch,
            grappleLookSpeed * Time.deltaTime
        );

        cameraTransform.localRotation =
            Quaternion.Euler(xRotation, 0f, 0f);
    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float actualSpeed;

        if (isCrouched)
        {
            actualSpeed = speed * 0.5f;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            actualSpeed = runSpeed;
        }
        else
        {
            actualSpeed = speed;
        }

        Vector3 move =
            transform.right * x +
            transform.forward * z;

        move.y = 0f;

        if (move.magnitude > 1f)
            move.Normalize();

        Vector3 targetVelocity = move * actualSpeed;

        // Mantém a velocidade vertical controlada pelo Rigidbody
        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z
        );
    }

    /// <summary>
    /// Chamado pelo HandScript logo após aplicar um impulso externo (repel),
    /// para o Move() não sobrescrever a velocidade no próximo tick.
    /// </summary>
    public void LockMovementControl(float duration = -1f)
    {
        float d = duration > 0f ? duration : repelControlLockDuration;
        repelLockTimer = Mathf.Max(repelLockTimer, d);
    }


    // =========================================================
    // CROUCH / SLIDE
    // =========================================================

    void HandleCrouchAndSlide()
    {
        bool grounded = IsGrounded();

        // Começar agachamento
        if (Input.GetKeyDown(KeyCode.LeftControl) && grounded)
        {
            SetCrouch(true);

            // Começar slide
            if (Input.GetKey(KeyCode.LeftShift))
            {
                StartSlide();
            }
        }

        // Soltou Ctrl
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            TryStandUp();
        }
    }


    void SetCrouch(bool crouch)
    {
        if (crouch)
        {
            if (isCrouched)
                return;

            isCrouched = true;

            capsule.height = crouchHeight;

            capsule.center = new Vector3(
                originalCenter.x,
                originalCenter.y - (originalHeight - crouchHeight) * 0.5f,
                originalCenter.z
            );
        }
        else
        {
            isCrouched = false;

            capsule.height = originalHeight;
            capsule.center = originalCenter;
        }
    }


    void StartSlide()
    {
        isSliding = true;

        currentSlideSpeed = slideStartSpeed;

        Vector3 inputDir =
            transform.right * Input.GetAxis("Horizontal") +
            transform.forward * Input.GetAxis("Vertical");

        inputDir.y = 0f;

        slideDirection =
            inputDir.magnitude > 0.1f
                ? inputDir.normalized
                : transform.forward;
    }


    void ExecuteSlide()
    {
        Vector3 velocity = rb.linearVelocity;

        Vector3 slideVelocity =
            slideDirection * currentSlideSpeed;

        rb.linearVelocity = new Vector3(
            slideVelocity.x,
            velocity.y,
            slideVelocity.z
        );

        // Desaceleração
        currentSlideSpeed -=
            slideDecayRate * Time.fixedDeltaTime;

        // Parar slide
        if (
            currentSlideSpeed <= slideMinSpeed ||
            !Input.GetKey(KeyCode.LeftControl)
        )
        {
            isSliding = false;

            if (!Input.GetKey(KeyCode.LeftControl))
            {
                TryStandUp();
            }
        }
    }


    void TryStandUp()
    {
        if (!CanStandUp())
            return;

        isCrouched = false;
        isSliding = false;

        capsule.height = originalHeight;
        capsule.center = originalCenter;
    }


    bool CanStandUp()
    {
        float radius = capsule.radius * 0.9f;

        Vector3 crouchTop =
            transform.position +
            capsule.center +
            Vector3.up *
            (capsule.height * 0.5f - radius);

        float extraHeight =
            originalHeight - crouchHeight;

        if (extraHeight <= 0f)
            return true;

        return !Physics.SphereCast(
            crouchTop,
            radius,
            Vector3.up,
            out RaycastHit hit,
            extraHeight,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }


    void ForceExitCrouchAndSlide()
    {
        isSliding = false;
        isCrouched = false;

        capsule.height = originalHeight;
        capsule.center = originalCenter;
    }


    // =========================================================
    // JUMP
    // =========================================================

    void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Guarda o pedido de pulo por uma janela curta, em vez de para sempre.
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }


    void ExecuteJump()
    {
        bool wantsToJump = jumpBufferCounter > 0f;
        bool canJump = coyoteTimeCounter > 0f;

        if (!wantsToJump || !canJump)
            return;

        // Consome os dois contadores para não pular de novo por engano
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        if (isSliding || isCrouched)
        {
            ForceExitCrouchAndSlide();
        }

        float jumpVelocity =
            Mathf.Sqrt(
                jumpForce *
                -2f *
                Physics.gravity.y *
                gravityMultiplier
            );

        Vector3 velocity = rb.linearVelocity;

        velocity.y = jumpVelocity;

        rb.linearVelocity = velocity;
    }


    // =========================================================
    // DASH
    // =========================================================

    void HandleDash()
    {
        // Recarrega sempre, no chão ou no ar (evita travar caso a detecção de chão oscile)
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Iniciar dash
        if (
            Input.GetKeyDown(KeyCode.Q) &&
            dashCooldownTimer <= 0f &&
            !isDashing && canDash
        )
        {
            if (isSliding || isCrouched)
            {
                ForceExitCrouchAndSlide();
            }

            isDashing = true;
            canDash = false;

            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            // Usa a direção completa da câmera (sem achatar o eixo Y),
            // assim olhar pra cima/baixo realmente muda o dash.
            dashDirection = cameraTransform.forward.normalized;

            // Desliga a gravidade durante o dash para a trajetória seguir
            // de verdade a direção da câmera, sem ser puxada pra baixo no meio do caminho.
            rb.useGravity = false;
        }
    }


    void ExecuteDash()
    {
        if (!isDashing)
            return;

        float dashProgress =
            dashTimer / dashDuration;

        float currentDashSpeed =
            dashSpeed * dashProgress;

        // Substitui a velocidade inteira (x, y e z) pela direção do dash,
        // permitindo dash na diagonal, pra cima ou pra baixo.
        rb.linearVelocity = dashDirection * currentDashSpeed;

        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            isDashing = false;
            rb.useGravity = true; // reativa a gravidade normal ao terminar o dash
        }
    }


    public void ForceDashReset()
    {
        canDash = true;
        dashCooldownTimer = 0f;
    }


    // =========================================================
    // GROUND CHECK
    // =========================================================

    bool IsGrounded()
    {
        Vector3 bottom =
            transform.position +
            capsule.center -
            Vector3.up *
            (capsule.height * 0.5f);

        float radius =
            capsule.radius *
            groundCheckRadiusMultiplier;

        Vector3 castStart =
            bottom +
            Vector3.up * (radius + groundCheckSkin);

        float castDistance =
            groundCheckSkin +
            groundCheckDistance;

        if (
            Physics.SphereCast(
                castStart,
                radius,
                Vector3.down,
                out RaycastHit hit,
                castDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            float angle =
                Vector3.Angle(
                    hit.normal,
                    Vector3.up
                );

            bool isGroundedResult = angle <= maxGroundAngle;

            return isGroundedResult;
        }
        return false;
    }


    void UpdateGroundState()
    {
        bool grounded = IsGrounded();

        if (grounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }

        // ExecuteJump agora decide sozinho (via buffer/coyote) se deve pular ou não,
        // então pode ser chamado sempre, sem depender só do "grounded" deste frame.
        ExecuteJump();

        // Detecta pouso
        if (grounded && !wasGrounded)
        {
            canDash = true;
            OnLand(lastVerticalVelocity);
        }

        lastVerticalVelocity =
            rb.linearVelocity.y;

        wasGrounded = grounded;
    }


    // =========================================================
    // CAMERA IMPACT
    // =========================================================

    void OnLand(float impactVelocity)
    {
        float impact =
            Mathf.Abs(impactVelocity);

        if (impact < 5f)
            return;

        float offset =
            Mathf.Clamp(
                impact * impactMultiplier,
                0.05f,
                0.3f
            );

        cameraOffset.y -= offset;
    }


    // =========================================================
    // FOV
    // =========================================================

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
        else if (isSliding)
        {
            targetFOV = runFOV + 5f;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            targetFOV = runFOV;
        }

        playerCamera.fieldOfView =
            Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFOV,
                fovSmoothSpeed *
                Time.deltaTime
            );
    }
}