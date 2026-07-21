using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerLookIK : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform lookTarget;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private MultiAimConstraint headAimConstraint;

    [Header("Look Target Settings")]
    [SerializeField] private float targetDistance = 6f;
    [SerializeField] private float smoothSpeed = 12f;

    [Header("Jumpscare")]
    [SerializeField] private bool inJumpscare;
    [SerializeField] private int normalLookSourceIndex;
    [SerializeField] private int jumpscareLookSourceIndex = 1;

    [Header("Network Settings")]
    [SerializeField] private float sendRate = 0.05f;
    [SerializeField] private float pitchSendThreshold = 1f;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;

    [SyncVar]
    private float syncedLookPitch;

    private Vector3 currentLookPosition;
    private Transform rootTransform;
    private float lastSentPitch;
    private float sendTimer;

    public bool InJumpscare => inJumpscare;

    private void Awake()
    {
        if (cameraMovement == null)
            cameraMovement = GetComponentInParent<CameraMovement>();

        rootTransform = transform.root;

        if (lookTarget != null)
        {
            lookTarget.SetParent(rootTransform, true);
            lookTarget.gameObject.SetActive(true);
            currentLookPosition = lookTarget.position;
        }

        SetJumpscareLook(false);
    }

    private void LateUpdate()
    {
        if (lookTarget == null)
            return;

        // normal camera look stops controlling the head during jumpscare
        if (inJumpscare)
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

        Transform cameraTransform =
            cameraMovement.PlayerCameraTransform;

        Vector3 targetPosition =
            cameraTransform.position +
            cameraTransform.forward * targetDistance;

        currentLookPosition = Vector3.Lerp(
            currentLookPosition,
            targetPosition,
            smoothSpeed * Time.deltaTime);

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

        float pitch = Mathf.Clamp(
            cameraMovement.verticalRotation,
            minPitch,
            maxPitch);

        if (Mathf.Abs(pitch - lastSentPitch) <
            pitchSendThreshold)
        {
            return;
        }

        lastSentPitch = pitch;
        CmdSetLookPitch(pitch);
    }

    [Command]
    private void CmdSetLookPitch(float newPitch)
    {
        syncedLookPitch = Mathf.Clamp(
            newPitch,
            minPitch,
            maxPitch);
    }

    private void UpdateRemoteLookTarget()
    {
        Transform yawTransform =
            rootTransform != null
                ? rootTransform
                : transform;

        Vector3 lookDirection =
            Quaternion.Euler(
                syncedLookPitch,
                yawTransform.eulerAngles.y,
                0f) *
            Vector3.forward;

        Vector3 targetPosition =
            yawTransform.position +
            lookDirection * targetDistance;

        currentLookPosition = Vector3.Lerp(
            currentLookPosition,
            targetPosition,
            smoothSpeed * Time.deltaTime);

        lookTarget.position = currentLookPosition;
    }

    public void SetJumpscareLook(bool active)
    {
        inJumpscare = active;

        if (headAimConstraint == null)
            return;

        WeightedTransformArray sources =
            headAimConstraint.data.sourceObjects;

        if (normalLookSourceIndex >= 0 &&
            normalLookSourceIndex < sources.Count)
        {
            WeightedTransform normalSource =
                sources[normalLookSourceIndex];

            normalSource.weight = active ? 0f : 1f;
            sources[normalLookSourceIndex] = normalSource;
        }

        if (jumpscareLookSourceIndex >= 0 &&
            jumpscareLookSourceIndex < sources.Count)
        {
            WeightedTransform jumpscareSource =
                sources[jumpscareLookSourceIndex];

            jumpscareSource.weight = active ? 1f : 0f;
            sources[jumpscareLookSourceIndex] =
                jumpscareSource;
        }

        MultiAimConstraintData data =
            headAimConstraint.data;

        data.sourceObjects = sources;
        headAimConstraint.data = data;
    }
}