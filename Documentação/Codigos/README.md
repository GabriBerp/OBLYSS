# OBLYSS
![Unity](https://img.shields.io/badge/unity-%23000000.svg?style=for-the-badge&logo=unity&logoColor=white) ![Aseprite](https://img.shields.io/badge/Aseprite-%23FFFFFF.svg?style=for-the-badge&logo=Aseprite&logoColor=#7D929E) ![Blender](https://img.shields.io/badge/blender-%23F5792A.svg?style=for-the-badge&logo=blender&logoColor=white)
> **Two souls. One body. No identity of its own.**

---
## 📁 Codigos

Aqui você pode encontrar algumas coisas que eu resolvi compartilhar com relação aos **Codigos** utilizados nesse projeto.
### Topicos:
* **Mecânicas do Jogador**:
    * [Movimentação](https://github.com/GabriBerp/OBLYSS/tree/main/Documentação/Codigos#-movimentação)
    * [Dash](https://github.com/GabriBerp/OBLYSS/tree/main/Documentação/Codigos#-dash)
    * [Deslizar](https://github.com/GabriBerp/OBLYSS/tree/main/Documentação/Codigos#-deslizar)
    * [Gancho](https://github.com/GabriBerp/OBLYSS/tree/main/Documentação/Codigos#-gancho)

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

## 🛝 Deslizar
```C#
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
```
**Deslizar** é uma das mecânicas do jogo mais interessantes falando pela parte de programação, novamente, para **Desliza** o jogador precisa **Correr** e durante a corrida, **Agachar**, ao fazer isso, o jogador começa a deslizar na direção que estava indo, **Deslizar** assim como o **Dash** move o jogador em uma unica direção, o diferencial de Deslizar é que ele tende a ser mais rapido e mais longo que o dash, e pode ser usado a qualquer momento enquanto o jogador estiver no chão.

O unico lado negativo é justamente o fato de somente poder ser usado enquanto o jogador estiver em contato com o chão, se o jogador começar a cair de uma plataforma mais alta, o **Deslizar** é automaticamente cancelado.
```C#
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
```
**Deslizar** dura bem mais do que o **Dash**, mas se ficar deslizando de mais, o jogador começa a perder velocidade gradativamente, e quando a velocidade ficar minima, ele cancela o deslizar, voltando a ficar agachado caso ainda esteja segurando Ctrl, ou então caso não esteja, o jogador vai tentar levantar.
```C#
void ForceExitCrouchAndSlide()
    {
        isSliding = false;
        isCrouched = false;

        capsule.height = originalHeight;
        capsule.center = originalCenter;
    }
```
Certas ações podem forçar o jogador a parar de **Deslizar**, sendo elas **Pular** e **Dash**, ao realizar uma dessas ações enquanto estiver deslizando, ira cancelar o **Deslizar** e o **Agachar**, fazendo o jogador levantar automaticamente (se possivel) ao utilizar uma dessas ações.

## 🪝 Gancho

A mecânica mais diferente que eu ja fiz em qualquer projeto na minha vida, por isso imagino que vou modificar ela bastante com o passar do tempo.
```C#
void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            ShootRope();
        }

        if (ropeFlying)
        {
            UpdateRopeFlight();
        }

        if (isGrappling)
        {
            UpdateGrapplePointFromTarget();
        }

        UpdateRope();
    }
```
Nesse caso, todos os scripts mais voltados para o funcionamento do gancho ficaram em outro **GameObject** sem ser o Player.
O **Update** desse script não precisou ter varias funções pra evitar ter muitas linhas de codigo, ja que o script era totalmente focado somente na mecânica do gancho, diferente do script do **Player** que focava em todas as mecânicas citadas acima.
```C#
void ShootRope()
    {
        mesh.enabled = true;
        ropeFlying = true;
        rope.enabled = true;

        shootOrigin = ropeOrigin.position;
        shootDirection = ropeOrigin.forward;

        ropeEnd = shootOrigin;

        pendingHit = false;
        hitTransform = null;

        Ray ray = new Ray(shootOrigin, shootDirection);

        if (Physics.Raycast(ray, out RaycastHit hit, ropeMaxDistance))
        {
            if (
                hit.collider.CompareTag("GrapTarget") ||
                hit.collider.CompareTag("GrapEnemy")
            )
            {
                pendingHit = true;
                pendingHitPoint = hit.point;
                pendingHitDistance = hit.distance;

                target = hit.collider.CompareTag("GrapEnemy") ? TargetType.GrapEnemy : TargetType.GrapWall;

                hitTransform = hit.collider.transform;
                localHitOffset = hitTransform.InverseTransformPoint(hit.point);
            }
        }
    }
```
O codigo da corda em si é bem complicado de entender por utilizar um **LineRenderer** pra (quem diria) renderizar uma linha, que seria a corda do nosso gancho nesse caso, ele dispara essa linha usando como base a direção em que o player esta olhando no momento que apertou o **Botão Direito** do mouse, a linha vai crescendo até um certo alcance, e caso acerte algo no caminho que tenha a tag **GrapTarget** (Uma supercie que o gancho pode se prender) ou **GrapEnemy** (O tipo de inimigo especifico em que você precisaria se puxar até ele para destruir ele), você é puxado até o objeto com a tag.
```C#
void UpdateRopeFlight()
    {
        ropeEnd += shootDirection * ropeSpeed * Time.deltaTime;

        float traveled = Vector3.Distance(shootOrigin, ropeEnd);

        if (pendingHit)
        {
            if (traveled >= pendingHitDistance)
            {
                ropeEnd = pendingHitPoint;
                grapplePoint = pendingHitPoint;

                ropeFlying = false;

                StartGrapple();
            }

            return;
        }

        if (traveled > ropeMaxDistance)
        {
            ropeFlying = false;
            rope.enabled = false;
            mesh.enabled = false;
        }
    }
```
Essa é justamente a função que realiza a viagem da corda do gancho até ela atingir o ponto que ja tinha sido decidido se existia ou não na função anterior, essa função é puramente mais estetica.

```C#
void StartGrapple()
    {
        if (playerRigidbody == null)
        {
            Debug.LogError(
                "HandScript: Rigidbody do jogador não foi encontrado."
            );

            return;
        }

        isGrappling = true;

        player.isGrappling = true;

        Vector3 initialDirection = grapplePoint - playerRigidbody.position;
        player.SetGrappleDirection(initialDirection);

        playerRigidbody.useGravity = false;
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }
```
Essa função por outro lado, é a responsavel por de fato preparar o jogador para ser puxado, mas quem faz esse trabalho é a proxima função e o **FixedUpdate**.

```C#
void FixedUpdate()
    {
        if (isGrappling)
        {
            PullPlayer();
        }
    }

void PullPlayer()
    {
        if (playerRigidbody == null)
            return;

        UpdateGrapplePointFromTarget();

        Vector3 direction =
            grapplePoint -
            playerRigidbody.position;

        float distance =
            direction.magnitude;

        if (distance <= stopDistance)
        {
            StopGrapple(direction.normalized);
            return;
        }

        direction.Normalize();

        player.SetGrappleDirection(direction);

        playerRigidbody.linearVelocity =
            direction * pullSpeed;
    }
```
Aqui o jogador é puxado tendo seu movimento padrão completamente substituido pelo movimento da função, fazendo ele ser puxado e olhar na direção de onde ele esta indo com o gancho, a ideia de forçar o jogador a olhar para onde ele esta indo, é para auxiliar quando for usar o gancho para atacar um inimigo.

Quando o puxão do gancho termina, uma força de repulsão é aplicada ao jogador, o afastando do objeto que ele acabou de se aproximar, isso serve tanto para dar espaço para o jogador utilizar um **Dash** ou se posicionar melhor para disparar o gancho em outro alvo disponivel ao redor, o deixando mais livre.
```C#
private void ApplyRepel(Vector3 travelDirection)
    {
        if (playerRigidbody == null) return;

        Vector3 flatBack = new Vector3(-travelDirection.x, 0f, -travelDirection.z);
        if (flatBack.sqrMagnitude < 0.0001f)
            flatBack = -playerRigidbody.transform.forward;

        flatBack.Normalize();

        float verticalAmount = travelDirection.y > upwardTargetThreshold
            ? repelUpwardBoost
            : repelMinUpward;

        Vector3 finalImpulse = flatBack * repelForce + Vector3.up * verticalAmount;

        Vector3 currentVel = playerRigidbody.linearVelocity;
        playerRigidbody.linearVelocity = new Vector3(0f, currentVel.y, 0f);

        playerRigidbody.AddForce(finalImpulse, ForceMode.VelocityChange);

        player.LockMovementControl();
    }
```

---

**OBLYSS — Ascend. Remember. Become.**
