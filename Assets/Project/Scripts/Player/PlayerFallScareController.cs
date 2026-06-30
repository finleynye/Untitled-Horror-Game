using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerFallScareController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform headTarget;

    [Header("Head Follow")]
    [SerializeField] private float cameraFollowSpeed = 12f;
    [SerializeField] private float cameraRotationSpeed = 10f;
    [SerializeField] private float cameraResetTime = 0.25f;

    [Header("Fall Animation")]
    [SerializeField] private string fallTriggerName = "FallBack";

    // fall is roughly 3 seconds
    // get up is roughly 2 seconds at 1.2x speed, so about 1.67 seconds
    // total is about 4.67 seconds
    [SerializeField] private float totalScareDuration = 4.7f;

    [Header("Root Motion")]
    [SerializeField] private bool useRootMotionForFall = true;
    [SerializeField] private bool applyRootYawRotation = false;
    [SerializeField] private float scareGravity = -18f;

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

    [Header("Ground Snap")]
    [SerializeField] private float groundRayStartHeight = 0.75f;
    [SerializeField] private float groundRayDistance = 4f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isPlayingScare;
    private bool shouldFollowHead;
    private bool scareRootMotionActive;

    private bool originalApplyRootMotion;
    private float scareVerticalVelocity;

    [Header("Model Reset")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private bool resetModelRootAfterScare = true;

    private Vector3 originalModelLocalPosition;
    private Quaternion originalModelLocalRotation;

    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private float originalFOV;

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

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (modelRoot == null && animator != null)
            modelRoot = animator.transform;

        if (modelRoot != null)
        {
            originalModelLocalPosition = modelRoot.localPosition;
            originalModelLocalRotation = modelRoot.localRotation;
        }

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

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

        cameraHolder.position = Vector3.Lerp(
            cameraHolder.position,
            headTarget.position,
            cameraFollowSpeed * Time.deltaTime
        );

        cameraHolder.rotation = Quaternion.Slerp(
            cameraHolder.rotation,
            headTarget.rotation,
            cameraRotationSpeed * Time.deltaTime
        );
    }

    private void OnAnimatorMove()
    {
        if (!isOwned)
            return;

        if (!scareRootMotionActive)
            return;

        if (animator == null)
            return;

        if (characterController == null)
            return;

        Vector3 rootDelta = animator.deltaPosition;

        // use X/Z root motion only
        // do not let the animation's Y motion pull the controller through the floor
        Vector3 horizontalDelta = new Vector3(rootDelta.x, 0f, rootDelta.z);

        if (characterController.isGrounded && scareVerticalVelocity < 0f)
            scareVerticalVelocity = -2f;

        scareVerticalVelocity += scareGravity * Time.deltaTime;

        Vector3 verticalDelta = Vector3.up * (scareVerticalVelocity * Time.deltaTime);

        characterController.Move(horizontalDelta + verticalDelta);

        if (applyRootYawRotation)
        {
            Vector3 rootEuler = animator.deltaRotation.eulerAngles;
            transform.Rotate(0f, rootEuler.y, 0f);
        }
    }
    private void ResetModelRoot()
    {
        if (!resetModelRootAfterScare)
            return;

        if (modelRoot == null)
            return;

        modelRoot.localPosition = originalModelLocalPosition;
        modelRoot.localRotation = originalModelLocalRotation;
    }
    public void PlayTreeFallScare(Vector3 treePosition)
    {
        if (!isOwned)
            return;

        if (isPlayingScare)
            return;

        scareRoutine = StartCoroutine(TreeFallRoutine(treePosition));
    }

    private IEnumerator TreeFallRoutine(Vector3 treePosition)
    {
        isPlayingScare = true;

        if (showDebugLogs)
            Debug.Log("PLAYER TREE FALL SCARE STARTED");

        if (playerMovement != null)
            playerMovement.isFrozen = true;

        if (audioSource != null && scareSound != null)
            audioSource.PlayOneShot(scareSound);

        if (animator != null)
        {
            originalApplyRootMotion = animator.applyRootMotion;

            animator.ResetTrigger(fallTriggerName);
            animator.SetTrigger(fallTriggerName);

            if (showDebugLogs)
                Debug.Log("triggering fall animation: " + fallTriggerName);
        }
        else
        {
            Debug.Log("no animator assigned for fall scare");
        }

        visualRoutine = StartCoroutine(ScareVisualRoutine());

        shouldFollowHead = true;

        if (useRootMotionForFall)
        {
            BeginRootMotionScare();

            yield return new WaitForSeconds(totalScareDuration);

            EndRootMotionScare();
        }
        else
        {
            yield return MoveCollisionBackwardsRoutine(totalScareDuration);
        }

        yield return ResetCameraRoutine();

        StopFallScare();

        if (showDebugLogs)
            Debug.Log("PLAYER TREE FALL SCARE FINISHED");
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

        if (animator != null)
            animator.applyRootMotion = originalApplyRootMotion;

        SnapControllerToGround();
    }

    private void StopFallScare()
    {
        scareRootMotionActive = false;
        shouldFollowHead = false;

        if (animator != null)
            animator.applyRootMotion = originalApplyRootMotion;

        SnapControllerToGround();

        ResetModelRoot();

        if (playerMovement != null)
            playerMovement.isFrozen = false;

        ApplyFOV(originalFOV);
        ApplyPostProcessing(0f);

        isPlayingScare = false;

        scareRoutine = null;
        visualRoutine = null;
    }

    private void FaceAwayFromTree(Vector3 treePosition)
    {
        Vector3 awayDirection = transform.position - treePosition;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.LookRotation(awayDirection.normalized);
    }

    private IEnumerator MoveCollisionBackwardsRoutine(float duration)
    {
        if (characterController == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float timer = 0f;
        float previousCurveValue = 0f;

        Vector3 backwardsDirection = -transform.forward;

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

            yield return null;
        }

        SnapControllerToGround();
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

    private void SnapControllerToGround()
    {
        if (characterController == null)
            return;

        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * groundRayStartHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundRayDistance))
        {
            characterController.enabled = false;

            Vector3 position = transform.position;
            position.y = hit.point.y;

            transform.position = position;

            characterController.enabled = true;
        }
    }
}