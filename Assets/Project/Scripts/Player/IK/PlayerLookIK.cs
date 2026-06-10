using Mirror;
using UnityEngine;

public class PlayerLookIK : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform lookTarget;
    [SerializeField] private CameraMovement cameraMovement;

    [Header("Look Target Settings")]
    [SerializeField] private float targetDistance = 6f;
    [SerializeField] private float smoothSpeed = 12f;

    private Vector3 currentLookPosition;

    private void Awake()
    {
        if (cameraMovement == null)
            cameraMovement = GetComponentInParent<CameraMovement>();

        if (lookTarget != null)
            currentLookPosition = lookTarget.position;
    }

    private void LateUpdate()
    {
        if (!isOwned) return;
        if (cameraMovement == null) return;
        if (lookTarget == null) return;
        if (cameraMovement.PlayerCameraTransform == null) return;

        Transform cameraTransform = cameraMovement.PlayerCameraTransform;

        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * targetDistance;

        currentLookPosition = Vector3.Lerp(currentLookPosition, targetPosition, smoothSpeed * Time.deltaTime);

        lookTarget.position = currentLookPosition;
    }
}