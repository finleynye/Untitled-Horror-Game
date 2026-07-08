using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerFallScareController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    [Header("Local Mesh Visibility")]
    [SerializeField] private bool forceLocalMeshVisibleDuringScare = true;
    [SerializeField] private LocalPlayerMeshVisibility localPlayerMeshVisibility;

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform headTarget;
    [SerializeField] private CameraMovement cameraMovement;

    [Header("Head Follow")]
    [SerializeField] private float cameraFollowSpeed = 12f;
    [SerializeField] private float cameraRotationSpeed = 10f;
    [SerializeField] private float cameraResetTime = 0.25f;
    [SerializeField] private float cameraPositionSmoothTime = 0.08f;
    [SerializeField] private float cameraRotationSmoothSpeed = 5f;
    [SerializeField] private bool followHeadRotationDuringScare = false;
    [SerializeField] private Vector3 scareCameraLocalOffset;

    [Header("Fall Animation")]
    [SerializeField] private string fallTriggerName = "FallBack";
    [SerializeField] private float totalScareDuration = 3f;

    [Header("Root Motion")]
    [SerializeField] private bool useRootMotionForFall = true;
    [SerializeField] private bool moveControllerWithRootMotion = false;
    [SerializeField] private bool applyRootYawRotation = false;
    [SerializeField] private float scareGravity = -18f;

    [Header("FOV Effect")]
    [SerializeField] private float scareFOV = 95f;
    [SerializeField] private float fovInTime = 0.2f;
    [SerializeField] private float fovOutTime = 0.8f;

    [Header("Post Processing")]
    [SerializeField] private Volume scareVolume;
    [SerializeField] private Color scareFilterColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private float scareSaturation = -35f;
    [SerializeField] private float scareContrast = 30f;
    [SerializeField] private float scareVignetteIntensity = 0.55f;
    [SerializeField] private float scareChromaticAberration = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip scareSound;

    [Header("Model Reset")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private bool resetModelRootAfterScare = true;

    private Transform playerRoot;

    private bool isPlayingScare;
    private bool shouldFollowHead;
    private bool scareRootMotionActive;
    private bool originalApplyRootMotion;
    private float scareVerticalVelocity;

    private TransformSnapshot playerRootSnapshot;
    private TransformSnapshot animatorSnapshot;
    private TransformSnapshot modelRootSnapshot;
    private TransformSnapshot cameraHolderSnapshot;

    private bool hasGetUpEventPosition;
    private Vector3 getUpEventPosition;
    private Quaternion getUpEventRotation;

    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private float originalFOV;
    private Vector3 cameraFollowVelocity;
    private Quaternion scareStartCameraWorldRotation;
    private bool originalCameraMovementEnabled;
    private bool hasCameraMovementSnapshot;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    private Color originalColorFilter;
    private float originalSaturation;
    private float originalContrast;
    private float originalVignetteIntensity;
    private float originalChromaticAberration;

    private struct TransformSnapshot
    {
        public Transform target;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 worldPosition;
        public Quaternion worldRotation;

        public bool IsValid => target != null;

        public TransformSnapshot(Transform transform)
        {
            target = transform;

            if (transform == null)
            {
                localPosition = Vector3.zero;
                localRotation = Quaternion.identity;
                worldPosition = Vector3.zero;
                worldRotation = Quaternion.identity;
                return;
            }

            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            worldPosition = transform.position;
            worldRotation = transform.rotation;
        }

        public void RestoreLocal()
        {
            if (target == null)
                return;

            target.localPosition = localPosition;
            target.localRotation = localRotation;
        }
    }

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (characterController == null) characterController = GetComponentInParent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (cameraMovement == null) cameraMovement = GetComponent<CameraMovement>();

        playerRoot = characterController != null ? characterController.transform : transform;

        if (modelRoot == null && animator != null)
            modelRoot = animator.transform;

        if (cameraMovement == null && playerRoot != null)
            cameraMovement = playerRoot.GetComponent<CameraMovement>();

        if (animator != null)
            originalApplyRootMotion = animator.applyRootMotion;

        if (cameraHolder != null)
        {
            originalCameraLocalPosition = cameraHolder.localPosition;
            originalCameraLocalRotation = cameraHolder.localRotation;
        }

        if (playerCamera != null)
            originalFOV = playerCamera.fieldOfView;

        CachePostProcessingValues();
    }

    private void LateUpdate()
    {
        if (!isOwned || !shouldFollowHead)
            return;

        FollowHeadWithCamera();
    }

    //public scare entry

    public void PlayTreeFallScare()
    {
        if (!isOwned || isPlayingScare)
            return;

        CmdPlayTreeFallScareForObservers();
        StartCoroutine(TreeFallRoutine());
    }

    public void PlayTreeFallScare(Vector3 _)
    {
        PlayTreeFallScare();
    }

    //networking

    [Command]
    private void CmdPlayTreeFallScareForObservers()
    {
        RpcPlayTreeFallScareForObservers();
    }

    [ClientRpc(includeOwner = false)]
    private void RpcPlayTreeFallScareForObservers()
    {
        if (isPlayingScare)
            return;

        StartCoroutine(RemoteVisualFallRoutine());
    }

    //local scare flow

    private IEnumerator TreeFallRoutine()
    {
        if (!CanPlayLocalScare())
            yield break;

        isPlayingScare = true;
        hasGetUpEventPosition = false;

        CaptureScareSnapshots();
        CaptureScareCameraStart();

        ForceLocalMeshVisibleForScare();
        DisableCameraMovementForScare();
        SetPlayerFrozen(true);
        PlayScareSound();
        PlayFallAnimation();

        StartCoroutine(ScareVisualRoutine());
        shouldFollowHead = true;

        if (useRootMotionForFall)
            yield return RunRootMotionScareUntilGetUp();
        else
            yield return MoveCollisionBackwardsRoutine(totalScareDuration);

        yield return ResetCameraRoutine();
        StopFallScare();
    }

    private bool CanPlayLocalScare()
    {
        if (animator == null) return false;
        if (playerMovement == null) return false;
        
        return true;
    }

    private void StopFallScare()
    {
        scareRootMotionActive = false;
        shouldFollowHead = false;
        cameraFollowVelocity = Vector3.zero;

        if (animator != null)
            animator.applyRootMotion = originalApplyRootMotion;

        ForceRestoreAfterScare();
        SetPlayerFrozen(false);

        ApplyFOV(originalFOV);
        ApplyPostProcessing(0f);
        RestoreLocalMeshVisibilityAfterScare();
        RestoreCameraMovementAfterScare();

        isPlayingScare = false;
    }

    private void SetPlayerFrozen(bool frozen)
    {
        if (playerMovement != null)
            playerMovement.isFrozen = frozen;
    }

    private void PlayScareSound()
    {
        if (audioSource != null && scareSound != null)
            audioSource.PlayOneShot(scareSound);
    }

    private void PlayFallAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(fallTriggerName);
        animator.SetTrigger(fallTriggerName);
    }

    //remote scare visuals

    private IEnumerator RemoteVisualFallRoutine()
    {
        isPlayingScare = true;

        CaptureScareSnapshots();

        bool previousApplyRootMotion = animator != null && animator.applyRootMotion;

        if (playerMovement != null)
            playerMovement.SetScareAnimationOverride(true);

        if (animator != null)
            animator.applyRootMotion = false;

        PlayScareSound();
        PlayFallAnimation();

        yield return new WaitForSeconds(totalScareDuration);

        if (animator != null)
            animator.applyRootMotion = previousApplyRootMotion;

        if (playerMovement != null)
            playerMovement.SetScareAnimationOverride(false);

        RestoreRemoteVisualOffsets();
        isPlayingScare = false;
    }

    private void RestoreRemoteVisualOffsets()
    {
        if (resetModelRootAfterScare)
            RestoreLocalIfDistinct(modelRootSnapshot, playerRoot, transform);

        RestoreLocalIfDistinct(animatorSnapshot, playerRoot, transform, modelRoot);
    }

    //root motion

    private void OnAnimatorMove()
    {
        if (!isOwned || !scareRootMotionActive || animator == null || characterController == null)
            return;

        Vector3 movement = Vector3.zero;

        if (moveControllerWithRootMotion)
        {
            Vector3 rootDelta = animator.deltaPosition;
            movement += new Vector3(rootDelta.x, 0f, rootDelta.z);
        }

        if (characterController.isGrounded && scareVerticalVelocity < 0f)
            scareVerticalVelocity = -2f;

        scareVerticalVelocity += scareGravity * Time.deltaTime;
        movement += Vector3.up * (scareVerticalVelocity * Time.deltaTime);

        characterController.Move(movement);
        Physics.SyncTransforms();

        if (applyRootYawRotation && moveControllerWithRootMotion && playerRoot != null)
        {
            Vector3 rootEuler = animator.deltaRotation.eulerAngles;
            playerRoot.Rotate(0f, rootEuler.y, 0f);
        }
    }

    private IEnumerator RunRootMotionScareUntilGetUp()
    {
        BeginRootMotionScare();

        float timer = 0f;

        while (!hasGetUpEventPosition && timer < totalScareDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        EndRootMotionScare();
    }

    private void BeginRootMotionScare()
    {
        if (animator == null)
            return;

        scareVerticalVelocity = -2f;
        originalApplyRootMotion = animator.applyRootMotion;
        animator.applyRootMotion = true;
        scareRootMotionActive = true;
    }

    private void EndRootMotionScare()
    {
        scareRootMotionActive = false;

        if (animator != null)
            animator.applyRootMotion = originalApplyRootMotion;
    }

    private IEnumerator MoveCollisionBackwardsRoutine(float duration)
    {
        if (characterController == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        scareVerticalVelocity = -2f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            if (characterController.isGrounded && scareVerticalVelocity < 0f)
                scareVerticalVelocity = -2f;

            scareVerticalVelocity += scareGravity * Time.deltaTime;
            Physics.SyncTransforms();

            yield return null;
        }
    }

    //camera

    private void FollowHeadWithCamera()
    {
        if (cameraHolder == null || headTarget == null)
            return;

        Vector3 targetPosition = headTarget.position + headTarget.TransformDirection(scareCameraLocalOffset);
        float positionSmoothTime = cameraPositionSmoothTime > 0f ? cameraPositionSmoothTime : 1f / Mathf.Max(cameraFollowSpeed, 0.01f);

        cameraHolder.position = Vector3.SmoothDamp(cameraHolder.position, targetPosition, ref cameraFollowVelocity, positionSmoothTime);

        Quaternion targetRotation = followHeadRotationDuringScare ? headTarget.rotation : scareStartCameraWorldRotation;
        float rotationSpeed = cameraRotationSmoothSpeed > 0f ? cameraRotationSmoothSpeed : cameraRotationSpeed;
        float rotationT = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
        cameraHolder.rotation = Quaternion.Slerp(cameraHolder.rotation, targetRotation, rotationT);
    }

    private void CaptureScareCameraStart()
    {
        cameraFollowVelocity = Vector3.zero;

        if (cameraHolder != null)
            scareStartCameraWorldRotation = cameraHolder.rotation;
    }

    private void DisableCameraMovementForScare()
    {
        hasCameraMovementSnapshot = false;

        if (cameraMovement == null)
            return;

        originalCameraMovementEnabled = cameraMovement.enabled;
        hasCameraMovementSnapshot = true;
        cameraMovement.enabled = false;
    }

    private void RestoreCameraMovementAfterScare()
    {
        if (!hasCameraMovementSnapshot || cameraMovement == null)
            return;

        cameraMovement.enabled = originalCameraMovementEnabled;
        hasCameraMovementSnapshot = false;
    }

    private IEnumerator ResetCameraRoutine()
    {
        if (cameraHolder == null)
            yield break;

        float timer = 0f;
        Vector3 startPosition = cameraHolder.localPosition;
        Quaternion startRotation = cameraHolder.localRotation;

        while (timer < cameraResetTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / cameraResetTime);

            cameraHolder.localPosition = Vector3.Lerp(startPosition, originalCameraLocalPosition, t);
            cameraHolder.localRotation = Quaternion.Slerp(startRotation, originalCameraLocalRotation, t);

            yield return null;
        }

        cameraHolder.localPosition = originalCameraLocalPosition;
        cameraHolder.localRotation = originalCameraLocalRotation;
    }

    //visual/post processing

    private void ForceLocalMeshVisibleForScare()
    {
        if (!forceLocalMeshVisibleDuringScare || localPlayerMeshVisibility == null)
            return;

        localPlayerMeshVisibility.SetForcedLocalVisible(true);
    }

    private void RestoreLocalMeshVisibilityAfterScare()
    {
        if (!forceLocalMeshVisibleDuringScare || localPlayerMeshVisibility == null)
            return;

        localPlayerMeshVisibility.SetForcedLocalVisible(false);
    }

    private IEnumerator ScareVisualRoutine()
    {
        float timer = 0f;

        while (timer < fovInTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fovInTime);

            ApplyFOV(Mathf.Lerp(originalFOV, scareFOV, t));
            ApplyPostProcessing(t);

            yield return null;
        }

        float remainingTime = Mathf.Max(0f, totalScareDuration - fovInTime - fovOutTime);
        yield return new WaitForSeconds(remainingTime);

        timer = 0f;

        while (timer < fovOutTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fovOutTime);

            ApplyFOV(Mathf.Lerp(scareFOV, originalFOV, t));
            ApplyPostProcessing(1f - t);

            yield return null;
        }

        ApplyFOV(originalFOV);
        ApplyPostProcessing(0f);
    }

    private void ApplyFOV(float targetFOV)
    {
        if (playerCamera == null)
            return;

        playerCamera.fieldOfView = targetFOV;
    }

    private void CachePostProcessingValues()
    {
        if (scareVolume == null || scareVolume.profile == null)
            return;

        scareVolume.profile.TryGet(out colorAdjustments);
        scareVolume.profile.TryGet(out vignette);
        scareVolume.profile.TryGet(out chromaticAberration);

        if (colorAdjustments != null)
        {
            originalColorFilter = colorAdjustments.colorFilter.value;
            originalSaturation = colorAdjustments.saturation.value;
            originalContrast = colorAdjustments.contrast.value;
        }

        if (vignette != null)
            originalVignetteIntensity = vignette.intensity.value;

        if (chromaticAberration != null)
            originalChromaticAberration = chromaticAberration.intensity.value;
    }

    private void ApplyPostProcessing(float amount)
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(originalColorFilter, scareFilterColor, amount);
            colorAdjustments.saturation.value = Mathf.Lerp(originalSaturation, scareSaturation, amount);
            colorAdjustments.contrast.value = Mathf.Lerp(originalContrast, scareContrast, amount);
        }

        if (vignette != null)
            vignette.intensity.value = Mathf.Lerp(originalVignetteIntensity, scareVignetteIntensity, amount);

        if (chromaticAberration != null)
            chromaticAberration.intensity.value = Mathf.Lerp(originalChromaticAberration, scareChromaticAberration, amount);
    }

    //restore/reset helpers

    private void CaptureScareSnapshots()
    {
        playerRootSnapshot = new TransformSnapshot(playerRoot);
        animatorSnapshot = new TransformSnapshot(animator != null ? animator.transform : null);
        modelRootSnapshot = new TransformSnapshot(modelRoot);
        cameraHolderSnapshot = new TransformSnapshot(cameraHolder);
    }

    private void ForceRestoreAfterScare()
    {
        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        if (playerRoot != null)
        {
            if (hasGetUpEventPosition)
            {
                playerRoot.position = getUpEventPosition;
                playerRoot.rotation = getUpEventRotation;
            }
            else if (playerRootSnapshot.IsValid)
            {
                playerRoot.rotation = playerRootSnapshot.worldRotation;
            }
        }

        if (resetModelRootAfterScare)
            RestoreLocalIfDistinct(modelRootSnapshot, playerRoot, transform);

        RestoreLocalIfDistinct(animatorSnapshot, playerRoot, transform, modelRoot);

        if (cameraHolderSnapshot.IsValid)
            cameraHolderSnapshot.RestoreLocal();

        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;
    }

    private void RestoreLocalIfDistinct(TransformSnapshot snapshot, params Transform[] alreadyRestored)
    {
        if (!snapshot.IsValid)
            return;

        for (int i = 0; i < alreadyRestored.Length; i++)
        {
            if (snapshot.target == alreadyRestored[i])
                return;
        }

        snapshot.RestoreLocal();
    }

    //animation events

    public void CaptureGetUpRootMotionPosition()
    {
        if (!isOwned || !isPlayingScare || hasGetUpEventPosition || playerRoot == null)
            return;

        hasGetUpEventPosition = true;
        getUpEventPosition = playerRoot.position;
        getUpEventRotation = playerRoot.rotation;

      //  Debug.Log($"Captured get up position: {getUpEventPosition}", this);
    }
}

