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

    [Header("Crouch & Slide Parameters")]
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float slideStartSpeed = 16f;
    [SerializeField] private float slideMinSpeed = 5f;
    [SerializeField] private float slideDecayRate = 6.5f;
    private float originalHeight;
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

        // Salva os valores originais de altura e câmera
        originalHeight = controller.height;
        originalCameraY = cameraTransform.localPosition.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        MouseLook();
        
        HandleCrouchAndSlide();

        if (!isDashing && !isSliding)
        {
           Move(); 
        }
        else if (isSliding)
        {
            ExecuteSlide();
        }

        ApplyGravityAndJump();
        HandleDash();
        UpdateFOV();
    }

    void LateUpdate()
    {
        // Interpolação suave para a altura da câmera ao agachar/deslizar
        float targetCamY = isCrouched ? originalCameraY - (originalHeight - crouchHeight) * 0.5f : originalCameraY;
        Vector3 targetCameraPosition = new Vector3(cameraTransform.localPosition.x, targetCamY, cameraTransform.localPosition.z) + cameraOffset;
        
        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition, 
            targetCameraPosition, 
            15f * Time.deltaTime
        );

        // Landing impact effect
        cameraOffset = Vector3.SmoothDamp(
            cameraOffset,
            Vector3.zero,
            ref cameraVelocity,
            1f / impactReturnSpeed
        );
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

        // Define a velocidade baseada no estado físico do jogador
        if (isCrouched)
        {
            actualSpeed = speed * 0.5f; // Velocidade lenta ao andar agachado
        }
        else if (Input.GetKey(KeyCode.LeftShift))
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

    void HandleCrouchAndSlide()
    {
        bool grounded = IsGrounded();

        // Pressionou Ctrl
        if (Input.GetKeyDown(KeyCode.LeftControl) && grounded)
        {
            isCrouched = true;
            controller.height = crouchHeight;
            controller.center = new Vector3(0, crouchHeight / 2f, 0);

            // Condição para iniciar o Deslize (Slide)
            if (Input.GetKey(KeyCode.LeftShift))
            {
                isSliding = true;
                currentSlideSpeed = slideStartSpeed;

                // Define a direção do slide baseado no input atual ou para frente do jogador
                Vector3 inputDir = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
                slideDirection = inputDir.magnitude > 0.1f ? inputDir.normalized : transform.forward;
            }
        }

        // Soltou Ctrl
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            TryStandUp();
        }
    }

    void ExecuteSlide()
    {
        // Move o personagem na direção travada do slide
        controller.Move(slideDirection * currentSlideSpeed * Time.deltaTime);

        // Desaceleração natural do slide ao longo do tempo
        currentSlideSpeed -= slideDecayRate * Time.deltaTime;

        // Condições de parada automática do slide
        if (currentSlideSpeed <= slideMinSpeed || !Input.GetKey(KeyCode.LeftControl))
        {
            isSliding = false;
            // Se ainda segura Ctrl, continua agachado, senão tenta levantar
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                TryStandUp();
            }
        }
    }

    void TryStandUp()
    {
        // Verifica se há teto impedindo o jogador de levantar (evita travar em locais baixos)
        float radius = controller.radius * 0.9f;
        Vector3 start = transform.position + Vector3.up * crouchHeight;
        float distance = originalHeight - crouchHeight;

        if (Physics.SphereCast(start, radius, Vector3.up, out RaycastHit hit, distance))
        {
            return; // Existe um teto acima, continua agachado
        }

        // Se o caminho estiver livre, levanta normalmente
        isCrouched = false;
        isSliding = false;
        controller.height = originalHeight;
        controller.center = new Vector3(0, originalHeight / 2f, 0);
    }

    void ForceExitCrouchAndSlide()
    {
        // Usado para interromper o slide imediatamente no pulo ou dash
        isSliding = false;
        isCrouched = false;
        controller.height = originalHeight;
        controller.center = new Vector3(0, originalHeight / 2f, 0);
    }

    public void ForceDashReset()
    {
        // Usado para resetar o Dash quando usa o Grapple.
        dashCooldownTimer = 0f;
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
        else if (isSliding)
        {
            targetFOV = runFOV + 5f; // FOV dinâmico levemente maior no slide
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
        // RECARGA APENAS NO CHÃO: Modificação aplicada aqui
        if (dashCooldownTimer > 0f && IsGrounded())
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Iniciar dash
        if (Input.GetKeyDown(KeyCode.Q) && dashCooldownTimer <= 0f && !isDashing)
        {
            if (isSliding || isCrouched)
            {
                ForceExitCrouchAndSlide(); // Cancela o slide ao dar o Dash
            }

            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            dashDirection = cameraTransform.forward.normalized;
        }

        // Executar dash
        if (isDashing)
        {
            float dashProgress = dashTimer / dashDuration;
            float currentDashSpeed = dashSpeed * dashProgress;

            controller.Move(dashDirection * currentDashSpeed * Time.deltaTime);

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

        // Pular
        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            if (isSliding || isCrouched)
            {
                ForceExitCrouchAndSlide(); // Cancela o slide ao Pular
            }
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
