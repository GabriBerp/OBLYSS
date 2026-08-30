using UnityEngine;
using System.Collections;

public class HandScript : MonoBehaviour
{
    [Header("Camera Parameters")]
    public Transform cameraTransform;

    [Header("Player Parameters")]
    public PlayerScript player;
    public Rigidbody playerRigidbody;

    [Header("Grapple Settings")]
    public float maxDistance = 30f;
    public float pullSpeed = 25f;
    public float stopDistance = 2f;
    [Header("Repel Settings")]
    [SerializeField] private float repelForce = 12f;
    [SerializeField] private float repelUpwardBoost = 5f;
    [SerializeField] private float repelMinUpward = 1.5f;
    [SerializeField] private float upwardTargetThreshold = 0.15f;


    private enum TargetType
    {
        GrapEnemy,
        GrapWall
    }

    private TargetType target;

    [Header("Rope Settings")]
    public LineRenderer rope;

    private bool ropeFlying;
    private Vector3 ropeEnd;

    [SerializeField] private float ropeSpeed = 60f;
    [SerializeField] private float ropeMaxDistance = 30f;

    private bool isGrappling;
    private Vector3 grapplePoint;

    [Header("Rope Origin")]
    public Transform ropeOrigin;

    [Header("Mesh Settings")]
    public MeshRenderer mesh;

    private Vector3 shootOrigin;
    private Vector3 shootDirection;

    private bool pendingHit;
    private Vector3 pendingHitPoint;
    private float pendingHitDistance;
    private Transform hitTransform;
    private Vector3 localHitOffset;
    


    void Start()
    {
        mesh.enabled = false;

        rope.positionCount = 2;
        rope.enabled = false;

        rope.numCornerVertices = 5;
        rope.numCapVertices = 5;

        // Caso o Rigidbody não tenha sido colocado manualmente no Inspector
        if (playerRigidbody == null && player != null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
    }


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

        // Mantém o ponto de gancho colado no alvo (se ele se mover)
        // também no Update, para o desenho da corda ficar suave.
        if (isGrappling)
        {
            UpdateGrapplePointFromTarget();
        }

        UpdateRope();
    }


    void FixedUpdate()
    {
        if (isGrappling)
        {
            PullPlayer();
        }
    }


    // =========================================================
    // DISPARO DO GANCHO
    // =========================================================

    void ShootRope()
    {
        mesh.enabled = true;
        ropeFlying = true;
        rope.enabled = true;

        // Trava origem e direção no momento exato do clique.
        // A partir daqui, mover a câmera não muda mais a mira.
        shootOrigin = ropeOrigin.position;
        shootDirection = ropeOrigin.forward;

        ropeEnd = shootOrigin;

        pendingHit = false;
        hitTransform = null;

        // O acerto (ou erro) é decidido AGORA, com base na mira
        // no instante do disparo — não a cada frame do voo.
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

                // Guarda o alvo e o ponto de impacto relativo a ele,
                // para a corda seguir o alvo se ele se mover.
                hitTransform = hit.collider.transform;
                localHitOffset = hitTransform.InverseTransformPoint(hit.point);
            }
        }
    }


    // =========================================================
    // VOO DA CORDA
    // Agora é só a animação visual indo até o ponto já decidido
    // no disparo (shootOrigin / shootDirection fixos).
    // =========================================================

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


    // =========================================================
    // INICIAR GRAPPLE
    // =========================================================

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

        // Já avisa a câmera pra travar na direção do gancho
        // desde o primeiro frame do puxão.
        Vector3 initialDirection = grapplePoint - playerRigidbody.position;
        player.SetGrappleDirection(initialDirection);

        // =====================================================
        // DESATIVA TEMPORARIAMENTE A GRAVIDADE
        // =====================================================

        playerRigidbody.useGravity = false;

        // Remove qualquer movimento anterior
        // para o jogador não continuar andando/pulando
        playerRigidbody.linearVelocity = Vector3.zero;

        // Remove também qualquer rotação acumulada
        playerRigidbody.angularVelocity = Vector3.zero;
    }


    // =========================================================
    // PUXAR JOGADOR
    // =========================================================

    void PullPlayer()
    {
        if (playerRigidbody == null)
            return;

        // Atualiza o ponto de gancho caso o alvo (ex: inimigo) tenha se movido.
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

        // Avisa a câmera do jogador para onde olhar durante o puxão.
        player.SetGrappleDirection(direction);

        // =====================================================
        // O Rigidbody agora controla o movimento.
        //
        // Não usamos mais:
        //
        // controller.Move(...)
        //
        // =====================================================

        playerRigidbody.linearVelocity =
            direction * pullSpeed;
    }


    // =========================================================
    // ACOMPANHAR O ALVO (parede fixa ou inimigo que se move)
    // =========================================================

    void UpdateGrapplePointFromTarget()
    {
        if (hitTransform != null)
        {
            grapplePoint = hitTransform.TransformPoint(localHitOffset);
        }
    }


    // =========================================================
    // FINALIZAR GRAPPLE
    // =========================================================

    void StopGrapple(Vector3 direction)
    {
        isGrappling = false;

        // Para completamente o movimento do grapple
        playerRigidbody.linearVelocity = Vector3.zero;

        // =====================================================
        // DEVOLVE A GRAVIDADE
        // =====================================================

        playerRigidbody.useGravity = true;

        player.isGrappling = false;

        player.ForceDashReset();

        rope.enabled = false;
        mesh.enabled = false;

        hitTransform = null;

        ApplyRepel(direction);
    }


    // =========================================================
    // RECUO APÓS O GRAPPLE
    // =========================================================

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


    // =========================================================
    // CORDA
    // =========================================================

    void UpdateRope()
    {
        if (!rope.enabled)
            return;

        rope.SetPosition(
            0,
            ropeOrigin.position
        );

        if (ropeFlying)
        {
            rope.SetPosition(
                1,
                ropeEnd
            );
        }
        else
        {
            rope.SetPosition(
                1,
                grapplePoint
            );
        }
    }
}