using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerFallScareController : NetworkBehaviour
{
    //this a big GODDAMN SCRIPT. but i cannot do it in any less lines. help me if you can xox
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
    [SerializeField] private float totalScareDuration = 2f;

    [Header("Root Motion")]
    [SerializeField] private bool useRootMotionForFall = true;
    [SerializeField] private bool moveControllerWithRootMotion = false;
    [SerializeField] private bool applyRootYawRotation = false;
    [SerializeField] private float scareGravity = -18f;
    private Transform playerRoot;

    [Header("Fallback Movement")]
    [SerializeField] private float backwardsDistance = 1.2f;
    [SerializeField] private AnimationCurve backwardsCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

    private bool isPlayingScare;
    private bool shouldFollowHead;
    private bool scareRootMotionActive;

    private bool originalApplyRootMotion;
    private float scareVerticalVelocity;

    [Header("Model Reset")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private bool resetModelRootAfterScare = true;

    //MOTHERFUCKING SNAPSHOTS
    private TransformSnapshot playerRootSnapshot;
    private TransformSnapshot scriptTransformSnapshot;
    private TransformSnapshot animatorSnapshot;
    private TransformSnapshot modelRootSnapshot;
    private TransformSnapshot cameraHolderSnapshot;

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

    private Coroutine scareRoutine;
    private Coroutine visualRoutine;
    private Coroutine remoteScareRoutine;


    //ts capture transform state for restoration after scare (original and post root motion fuckery)
    private struct TransformSnapshot
    {
        public Transform target;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 worldPosition;
        public Quaternion worldRotation;

        public bool IsValid => target != null;

        //awesome little hack here, i was using a before/after transform reference before.
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

        public void RestoreWorld()
        {
            if (target == null)
                return;

            target.position = worldPosition;
            target.rotation = worldRotation;
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

        if (characterController != null)
            playerRoot = characterController.transform;
        else
            playerRoot = transform;

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
        if (!isOwned)
            return;

        if (!shouldFollowHead)
            return;

        if (cameraHolder == null || headTarget == null)
            return;

        Vector3 targetPosition = headTarget.position + headTarget.TransformDirection(scareCameraLocalOffset);
        float positionSmoothTime = cameraPositionSmoothTime > 0f ? cameraPositionSmoothTime : 1f / Mathf.Max(cameraFollowSpeed, 0.01f);

        //smooth noisy jittery head bone motion
        cameraHolder.position = Vector3.SmoothDamp(cameraHolder.position, targetPosition, ref cameraFollowVelocity, positionSmoothTime);

        Quaternion targetRotation = followHeadRotationDuringScare ? headTarget.rotation : scareStartCameraWorldRotation;
        float rotationSpeed = cameraRotationSmoothSpeed > 0f ? cameraRotationSmoothSpeed : cameraRotationSpeed;
        float rotationT = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
        cameraHolder.rotation = Quaternion.Slerp(cameraHolder.rotation, targetRotation, rotationT);
    }

    private void OnAnimatorMove()
    {
        if (!isOwned) return;
        if (!scareRootMotionActive) return;
        if (animator == null) return;
        if (characterController == null) return;

        Vector3 movement = Vector3.zero;

        //visual root motion
        if (moveControllerWithRootMotion)
        {
            Vector3 rootDelta = animator.deltaPosition;
            movement += new Vector3(rootDelta.x, 0f, rootDelta.z);
        }

        if (characterController.isGrounded && scareVerticalVelocity < 0f)
            scareVerticalVelocity = -2f;

        scareVerticalVelocity += scareGravity * Time.deltaTime;
        movement += Vector3.up * (scareVerticalVelocity * Time.deltaTime);

        characterController.Move(movement); //actually move the character controller now yipee
        Physics.SyncTransforms();

        if (applyRootYawRotation && moveControllerWithRootMotion)
        {
            Vector3 rootEuler = animator.deltaRotation.eulerAngles;
            playerRoot.Rotate(0f, rootEuler.y, 0f);
        }
    }

    public void PlayTreeFallScare(Vector3 treePosition)
    {
        if (!isOwned) return;
        if (isPlayingScare) return;

        scareRoutine = StartCoroutine(TreeFallRoutine(treePosition));
    }

    [TargetRpc]
    public void TargetPlayTreeFallScare(NetworkConnectionToClient conn, Vector3 treePosition)
    {
        PlayTreeFallScare(treePosition);
    }

    [ClientRpc(includeOwner = false)]
    public void RpcPlayTreeFallScareForObservers(Vector3 treePosition)
    {
        if (isPlayingScare)
            return;

        if (remoteScareRoutine != null)
            StopCoroutine(remoteScareRoutine);

        remoteScareRoutine = StartCoroutine(RemoteTreeFallRoutine(treePosition));
    }

    private IEnumerator TreeFallRoutine(Vector3 treePosition)
    {
        isPlayingScare = true;

        //pre animation restore anchors
        CaptureScareSnapshots();
        CaptureScareCameraStart();

        //local body visible for fall animation
        ForceLocalMeshVisibleForScare();
        DisableCameraMovementForScare();

        playerMovement.isFrozen = true;
        playerMovement.SetScareAnimationOverride(true);
        audioSource.PlayOneShot(scareSound);

        //apply the rootmotion and play fall animation
        originalApplyRootMotion = animator.applyRootMotion;
        animator.ResetTrigger(fallTriggerName);
        animator.SetTrigger(fallTriggerName);

        visualRoutine = StartCoroutine(ScareVisualRoutine());
        shouldFollowHead = true;

        if (useRootMotionForFall)
        {
            BeginRootMotionScare();
            yield return new WaitForSeconds(totalScareDuration);
            EndRootMotionScare();
        }
        else
            yield return MoveCollisionBackwardsRoutine(totalScareDuration);

        //reset the origin because FUCKING ROOT ANIMATION MESSES WITH TRANSFORMS AND I HATE IT BECAUSE I HAVE HAD TO DO SO MUCH MORE WORK BECAUSE OF IT
        ForceRestoreAfterScare();
        yield return ResetCameraRoutine();
        StopFallScare();
    }

    private void BeginRootMotionScare()
    {
        if (animator == null)
            return;

        scareVerticalVelocity = -2f;
        animator.applyRootMotion = true;
        scareRootMotionActive = true;
    }

    private void EndRootMotionScare()
    {
        scareRootMotionActive = false;
        animator.applyRootMotion = originalApplyRootMotion;
        //clear animation applied offsets
        animator.Rebind();
        animator.Update(0f);
    }

    private void StopFallScare()
    {
        scareRootMotionActive = false;

        shouldFollowHead = false;
        cameraFollowVelocity = Vector3.zero;
        animator.applyRootMotion = originalApplyRootMotion;

        ForceRestoreAfterScare();
        playerMovement.SetScareAnimationOverride(false);
        playerMovement.isFrozen = false;

        ApplyFOV(originalFOV);
        ApplyPostProcessing(0f);
        RestoreLocalMeshVisibilityAfterScare();
        RestoreCameraMovementAfterScare();

        isPlayingScare = false;
        scareRoutine = null;
        visualRoutine = null;
    }

    private IEnumerator RemoteTreeFallRoutine(Vector3 treePosition)
    {
        isPlayingScare = true;

        CaptureScareSnapshots();

        if (playerMovement != null)
            playerMovement.SetScareAnimationOverride(true);

        if (animator != null)
        {
            originalApplyRootMotion = animator.applyRootMotion;
            animator.applyRootMotion = false;
            animator.ResetTrigger(fallTriggerName);
            animator.SetTrigger(fallTriggerName);
        }

        yield return new WaitForSeconds(totalScareDuration);

        if (animator != null)
        {
            animator.applyRootMotion = originalApplyRootMotion;
            animator.Rebind();
            animator.Update(0f);
        }

        ForceRestoreAfterScare();

        if (playerMovement != null)
            playerMovement.SetScareAnimationOverride(false);

        isPlayingScare = false;
        remoteScareRoutine = null;
    }

    private void DisableCameraMovementForScare()
    {
        hasCameraMovementSnapshot = false;

        if (cameraMovement == null)
            return;

        //jumpscare camera owns view
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
    private void CaptureScareSnapshots()
    {
        //before root motion fucks with the transform hierarchy we capture EXACLTY WHERE THE MF is
        playerRootSnapshot = new TransformSnapshot(playerRoot);
        scriptTransformSnapshot = new TransformSnapshot(transform);
        animatorSnapshot = new TransformSnapshot(animator != null ? animator.transform : null);
        modelRootSnapshot = new TransformSnapshot(modelRoot);
        cameraHolderSnapshot = new TransformSnapshot(cameraHolder);
    }

    private void CaptureScareCameraStart()
    {
        cameraFollowVelocity = Vector3.zero;
        scareStartCameraWorldRotation = cameraHolder.rotation;
    }
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

    private void ForceRestoreAfterScare()
    {
        bool controllerWasEnabled = characterController != null && characterController.enabled;

        //avoid controller fighting transform restore
        if (characterController != null)
            characterController.enabled = false;

        //root first children after otherwise shit breaks
        if (playerRootSnapshot.IsValid)
            playerRootSnapshot.RestoreWorld();

        RestoreLocalIfDistinct(scriptTransformSnapshot, playerRoot);

        if (resetModelRootAfterScare)
            RestoreLocalIfDistinct(modelRootSnapshot, playerRoot, transform);

        RestoreLocalIfDistinct(animatorSnapshot, playerRoot, transform, modelRoot);

        if (cameraHolderSnapshot.IsValid)
            cameraHolderSnapshot.RestoreLocal();

        //sync the transform for local player
        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;
    }

    private void RestoreLocalIfDistinct(TransformSnapshot snapshot, params Transform[] alreadyRestored)
    {
        //same transform may be model and animator
        if (!snapshot.IsValid)
            return;

        for (int i = 0; i < alreadyRestored.Length; i++)
        {
            if (snapshot.target == alreadyRestored[i])
                return;
        }

        snapshot.RestoreLocal();
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
        float previousCurveValue = 0f;
        Vector3 backwardsDirection = -playerRoot.forward;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float normalisedTime = Mathf.Clamp01(timer / duration);
            float curveValue = backwardsCurve.Evaluate(normalisedTime);
            float curveDelta = curveValue - previousCurveValue;
            previousCurveValue = curveValue;

            Vector3 horizontalMovement = backwardsDirection * (backwardsDistance * curveDelta);

            if (characterController.isGrounded && scareVerticalVelocity < 0f)
                scareVerticalVelocity = -2f;

            scareVerticalVelocity += scareGravity * Time.deltaTime;
            Vector3 verticalMovement = Vector3.up * (scareVerticalVelocity * Time.deltaTime);

            characterController.Move(horizontalMovement + verticalMovement);
            Physics.SyncTransforms();

            yield return null;
        }
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
}
