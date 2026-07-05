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

    [Header("Network Settings")]
    [SerializeField] private float sendRate = 0.05f;
    [SerializeField] private float pitchSendThreshold = 1f;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;

    [SyncVar]
    private float syncedLookPitch;

    private Vector3 currentLookPosition;
    private float lastSentPitch;
    private float sendTimer;

    private void Awake()
    {
        if (cameraMovement == null)
            cameraMovement = GetComponentInParent<CameraMovement>();

        if (lookTarget != null)
            currentLookPosition = lookTarget.position;
    }

    private void LateUpdate()
    {
        if (lookTarget == null)
            return;

        if (isOwned)
        {
            UpdateLocalLookTarget();
            SendLookPitchToServer();
            return;
        }

        UpdateRemoteLookTarget();
    }

    private void UpdateLocalLookTarget()
    {
        if (cameraMovement == null)
            return;

        if (cameraMovement.PlayerCameraTransform == null)
            return;

        Transform cameraTransform = cameraMovement.PlayerCameraTransform;
        Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * targetDistance;
        currentLookPosition = Vector3.Lerp(currentLookPosition, targetPosition, smoothSpeed * Time.deltaTime);

        lookTarget.position = currentLookPosition;
    }

    private void SendLookPitchToServer()
    {
        if (cameraMovement == null)
            return;

        if (cameraMovement.PlayerCameraTransform == null)
            return;

        sendTimer += Time.deltaTime;

        if (sendTimer < sendRate)
            return;

        sendTimer = 0f;

        float pitch = cameraMovement.PlayerCameraTransform.localEulerAngles.x;

        if (pitch > 180f)
            pitch -= 360f;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (Mathf.Abs(pitch - lastSentPitch) < pitchSendThreshold)
            return;

        lastSentPitch = pitch;

        CmdSetLookPitch(pitch);
    }

    [Command]
    private void CmdSetLookPitch(float newPitch)
    {
        syncedLookPitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
    }

    private void UpdateRemoteLookTarget()
    {
        Vector3 lookDirection = Quaternion.Euler(syncedLookPitch, transform.eulerAngles.y, 0f) * Vector3.forward;

        Vector3 targetPosition = transform.position + lookDirection * targetDistance;

        currentLookPosition = Vector3.Lerp(currentLookPosition, targetPosition, smoothSpeed * Time.deltaTime);
        lookTarget.position = currentLookPosition;
    }
}