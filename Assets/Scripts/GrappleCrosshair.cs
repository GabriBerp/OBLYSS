using UnityEngine;
using UnityEngine.UI;

public class GrappleCrosshair : MonoBehaviour
{
    public Transform cameraTransform;
    public Image crosshair;
    public float maxDistance = 30f;

    void Update()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.CompareTag("GrapTarget"))
            {
                crosshair.color = Color.green;
                return;
            }
        }

        crosshair.color = Color.white;
    }
}