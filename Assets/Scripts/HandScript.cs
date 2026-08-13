using UnityEngine;
using System.Collections;

public class HandScript : MonoBehaviour
{
    [Header("Camera Parameters")]
    public Transform cameraTransform;

    [Header("Player Parameters")]
    public PlayerScript player;
    public CharacterController controller;

    [Header("Grapple Settings")]
    public float maxDistance = 30f;
    public float pullSpeed = 25f;
    public float stopDistance = 2f;
    public float repelForce = 8f;

    [Header("Rope Settings")]
    public LineRenderer rope;
    bool ropeFlying;
    Vector3 ropeEnd;
    float ropeSpeed = 60f;
    float ropeMaxDistance = 30f;
    private bool isGrappling;
    private Vector3 grapplePoint;

    [Header("Rope Origin")]
    public Transform ropeOrigin;

    void Start()
    {
        rope.positionCount = 2;
        rope.enabled = false;
        rope.numCornerVertices = 5;
        rope.numCapVertices = 5;
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

        if (isGrappling)
        {
            PullPlayer();
        }

        UpdateRope();
    }

    void TryGrapple()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.CompareTag("GrapTarget"))
            {
                grapplePoint = hit.point;
                isGrappling = true;

                player.disableGravity = true;
                player.isGrappling = true;

                rope.positionCount = 2;
            }
        }
    }

    void ShootRope()
    {
        ropeFlying = true;
        rope.enabled = true;

        ropeEnd = ropeOrigin.position;
    }

    void PullPlayer()
    {
        Vector3 direction = (grapplePoint - controller.transform.position);
        float distance = direction.magnitude;

        direction.Normalize();

        controller.Move(direction * pullSpeed * Time.deltaTime);

        if (distance < stopDistance)
        {
            StopGrapple(direction);
        }
    }

    void UpdateRopeFlight()
    {
        ropeEnd += ropeOrigin.forward * ropeSpeed * Time.deltaTime;

        if (Vector3.Distance(ropeOrigin.position, ropeEnd) > ropeMaxDistance)
        {
            ropeFlying = false;
            rope.enabled = false;
            return;
        }

        Ray ray = new Ray(ropeOrigin.position, ropeOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, ropeMaxDistance))
        {
            if (hit.collider.CompareTag("GrapTarget"))
            {
                grapplePoint = hit.point;
                ropeEnd = grapplePoint;

                ropeFlying = false;
                StartGrapple();
            }
        }
    }

    void UpdateRope()
    {
        if (!rope.enabled) return;

        rope.SetPosition(0, ropeOrigin.position);

        if (ropeFlying)
            rope.SetPosition(1, ropeEnd);
        else
            rope.SetPosition(1, grapplePoint);
    }

    void StartGrapple()
    {
        isGrappling = true;

        player.disableGravity = true;
        player.isGrappling = true;
    }
    void StopGrapple(Vector3 direction)
    {
        isGrappling = false;

        player.disableGravity = false;
        player.isGrappling = false;
        player.ForceDashReset();

        rope.enabled = false;

        StartCoroutine(Repel(direction));
    }

    IEnumerator Repel(Vector3 direction)
    {
        float timer = 0.2f;

        while (timer > 0)
        {
            controller.Move(-direction * repelForce * Time.deltaTime);
            timer -= Time.deltaTime;
            yield return null;
        }
    }
}