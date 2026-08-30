# OBLYSS
![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white) ![Aseprite](https://img.shields.io/badge/Aseprite-%23FFFFFF.svg?style=for-the-badge&logo=Aseprite&logoColor=#7D929E) ![Blender](https://img.shields.io/badge/blender-%23F5792A.svg?style=for-the-badge&logo=blender&logoColor=white)
> **Two souls. One body. No identity of its own.**

---
## 📁 Codigos

Aqui você pode encontrar algumas coisas que eu resolvi compartilhar com relação aos **Codigos** utilizados nesse projeto.
### Topicos:
* Movimentação
* Dash
* Deslizar
* Gancho

---
## 🚶 Movimentação

Obviamente a movimentação seria a parte mais importante de **OBLYSS**, então essa seria a parte que eu falaria mais feliz sobre.

```C#
void Update()
    {
        MouseLook();

        HandleCrouchAndSlide();
        HandleDash();
        HandleJumpInput();
        UpdateFOV();
    }
```
A ideia era deixar o update de fato com a menor quantidade de codigo possivel, então eu acabei criando varias funções para cuidar de cada mecânica de movimentação, e simplesmente fazer o Update chamar todas.
Enquanto a movimentação de FATO ficaria localizada no **FixedUpdate**, juntamente de um pouco de codigo para cuidar de outros efeitos e mecânicas do jogo.
```C#
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
```
Isso de certa forma ajuda mais o codigo a rodar, porque a função **Update** é chamada a cada frame do jogo, então deixar muito codigo especificamente dentro dela é de certa forma propenso a pesar mais o jogo, então uma opção boa é fazer o **Update** chamar outras funções, assim suavizando um pouco o peso que ficaria sobre o **Update**.
E dai então temos a função que controla justamente o movimento.
```C#
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

        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z
        );
    }
```
Nesse caso, é um codigo "basico" que utiliza o sistema de Imput antigo da Unity, fazendo um classico **Vector3** porém nesse caso, zerando o Y do vetor ja que o mesmo vai ser controlado pela gravidade do **Rigidbody**.
A velocidade na qual o jogador se movimenta é alterada caso **isCrouched** seja verdadeiro, ou caso ele esteja apertando **LeftShift**, que seria correndo.
```C#
void HandleCrouchAndSlide()
    {
        bool grounded = IsGrounded();

        // Começar agachamento
        if (Input.GetKeyDown(KeyCode.LeftControl) && grounded)
        {
            SetCrouch(true);

            // Começar a deslizar
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
```
Quando o personagem agacha, a sua colisão e o seu campo de visão abaixam para poder passar por lugares menores, ao **agachar** enquanto estiver **correndo** você vai ativar um **Slide** ou então **Deslizar**, falado mais a frente.

## 💨 Dash
```C#
void HandleDash()
    {
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

            dashDirection = cameraTransform.forward.normalized;

            rb.useGravity = false;
        }
    }
```
O Dash é simples mas ao mesmo tempo não é, a ideia era poder dar um avanço em qualquer direção, ficando momentaneamente imune aos efeitos da gravidade, o dash serve para chegar em locais mais altos ou lugares mais distantes, que você não chegaria com um simples pulo.

```C#
void ExecuteDash()
    {
        if (!isDashing)
            return;

        float dashProgress =
            dashTimer / dashDuration;

        float currentDashSpeed =
            dashSpeed * dashProgress;

        rb.linearVelocity = dashDirection * currentDashSpeed;

        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            isDashing = false;
            rb.useGravity = true;
        }
    }
```
Quando o dash é utilizado pela primeira vez, **canDash** se torna **false** impedindo que outros dashs sejam executados mesmo que o cooldown do mesmo recarregue, **canDash** somente volta a ser **true** quando o jogador pisar no chão pelo menos uma vez, no frame exato que o jogador tocar no chão, automaticamente **canDash** volta a ser **true**.
```C#
// Detecta pouso
        if (grounded && !wasGrounded)
        {
            canDash = true;
            OnLand(lastVerticalVelocity);
        }
```
Outro cenario aonde **canDash** volta a ser **true** são interações especiais usando o gancho ou ao finalizar certos inimigos, aonde uma função do player chamada **ForceDashReset()** é ativada, resetando o cooldown do dash, e definindo **canDash** para **true** automaticamente.

---

**OBLYSS — Ascend. Remember. Become.**