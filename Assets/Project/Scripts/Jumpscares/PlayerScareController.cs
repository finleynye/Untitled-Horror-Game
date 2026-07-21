using System.Collections;
using UnityEngine;

public class PlayerScareController : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;
    [SerializeField] private LocalPlayerMeshVisibility localMeshVisibility;

    [Header("Module Fallback References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform headTarget;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform modelRoot;

    [Header("Reusable Scare Modules")]
    [SerializeField] private ScareEffectsModule scareEffects;
    [SerializeField] private ScareAudioModule scareAudio;
    [SerializeField] private ScareCameraModule scareCamera;
    [SerializeField] private ScareTransformModule scareTransform;
    [SerializeField] private ScareAnimationModule scareAnimation;

    [Header("Local Player State")]
    [SerializeField] private bool forceLocalMeshVisibleDuringScare = true;

    private Transform playerRoot;
    private bool isScareActive;

    public bool IsScareActive => isScareActive;
    public Animator Animator => animator;
    public CharacterController CharacterController => characterController;
    public Transform PlayerRoot => playerRoot;

    private void Awake()
    {
        ResolveReferences();
        InitialiseModules();
    }

    private void LateUpdate()
    {
        scareCamera?.TickFollow();
    }

    private void ResolveReferences()
    {
        playerMovement = GetComponent<PlayerMovement>();
        characterController = GetComponentInParent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerCamera = GetComponentInChildren<Camera>();
        cameraMovement = GetComponent<CameraMovement>();

        playerRoot = characterController != null ? characterController.transform : transform;

        if (cameraMovement == null && playerRoot != null)
            cameraMovement = playerRoot.GetComponent<CameraMovement>();

        if (modelRoot == null && animator != null)
            modelRoot = animator.transform;

        if (localMeshVisibility == null)
            localMeshVisibility = GetComponentInChildren<LocalPlayerMeshVisibility>();
        
    }

    private void InitialiseModules()
    {
        scareEffects?.Initialise(playerCamera);
        scareAudio?.Initialise(audioSource);
        scareCamera?.Initialise(cameraHolder, headTarget, cameraMovement);
        scareTransform?.Initialise(characterController, playerRoot, animator != null ? animator.transform : null, modelRoot);
        scareAnimation?.Initialise(animator);
    }

    public bool TryBeginScare()
    {
        if (isScareActive)
            return false;

        isScareActive = true;
        return true;
    }

    public void EndScare() => isScareActive = false;
    public bool BeginLocalScare(string animationTrigger, float duration, AudioClip audioClip = null, bool followCamera = true)
    {
        if (!TryBeginScare())
            return false;

        CaptureState();
        LockLocalPlayer();

        if (followCamera)
            BeginCameraFollow();

        if (audioClip != null)
            PlayAudio(audioClip);
        else
            PlayDefaultAudio();

        TriggerAnimation(animationTrigger);
        PlayEffects(duration);

        return true;
    }

    public void FinishLocalScare(bool useOverridePosition = false, Vector3 overridePosition = default, Quaternion overrideRotation = default)
    {
        if (!isScareActive)
            return;

        ResetCameraImmediately();

        RestoreState(useOverridePosition, overridePosition, overrideRotation);

        UnlockLocalPlayer();
        ResetEffects();
        EndScare();
    }

    public void CaptureState() => scareTransform?.Capture();

    public void RestoreState(bool useOverridePosition = false, Vector3 overridePosition = default, Quaternion overrideRotation = default)
    {
        if (overrideRotation == default) overrideRotation = Quaternion.identity;

        scareTransform?.RestoreAfterScare(useOverridePosition, overridePosition, overrideRotation);
    }

    public void RestoreRemoteVisualState() =>  scareTransform?.RestoreRemoteVisualOffsets();

    public bool BeginRemoteScare(string animationTrigger, AudioClip audioClip = null)
    {
        if (!TryBeginScare())
            return false;

        CaptureState();
        SetAnimationOverride(true);
        DisableRootMotion();

        if (audioClip != null)
            PlayAudio(audioClip);
        else
            PlayDefaultAudio();

        TriggerAnimation(animationTrigger);

        return true;
    }

    public void FinishRemoteScare()
    {
        if (!isScareActive)
            return;

        RestoreRootMotion();
        SetAnimationOverride(false);
        RestoreRemoteVisualState();
        EndScare();
    }
    public void LockLocalPlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.isFrozen = true;
            playerMovement.SetScareAnimationOverride(true);
        }

        SetForcedMeshVisible(true);
    }

    public void UnlockLocalPlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.SetScareAnimationOverride(false);
            playerMovement.isFrozen = false;
        }

        SetForcedMeshVisible(false);
    }

    public void SetAnimationOverride(bool active)
    {
        if (playerMovement != null)
            playerMovement.SetScareAnimationOverride(active);
    }

    public void BeginCameraFollow()
    {
        scareCamera?.BeginFollow();
    }

    public IEnumerator ResetCameraRoutine()
    {
        if (scareCamera != null)
            yield return scareCamera.ResetRoutine();
    }

    public void ResetCameraImmediately() => scareCamera?.StopImmediately();
   
    public void PlayEffects(float duration)
    {
        if (scareEffects != null)
            StartCoroutine(scareEffects.PlayRoutine(duration));
    }

    public void ResetEffects() => scareEffects?.ResetImmediately();
    public void PlayDefaultAudio() => scareAudio?.PlayDefault();
    public void PlayAudio(AudioClip clip) => scareAudio?.Play(clip);
    public void TriggerAnimation(string triggerName) => scareAnimation?.Trigger(triggerName);
    public void EnableRootMotion() => scareAnimation?.EnableRootMotion();
    public void DisableRootMotion() => scareAnimation?.DisableRootMotion();
    public void RestoreRootMotion() => scareAnimation?.RestoreRootMotionState();

    public Vector3 GetAnimatorDeltaPosition()
    {
        return scareAnimation != null ? scareAnimation.GetDeltaPosition() : Vector3.zero;
    }

    public Quaternion GetAnimatorDeltaRotation()
    {
        return scareAnimation != null ? scareAnimation.GetDeltaRotation() : Quaternion.identity;
    }

    public void AlignPlayer(Vector3 worldPosition, Quaternion worldRotation) => scareTransform?.AlignRoot(worldPosition, worldRotation);
    
    private void SetForcedMeshVisible(bool visible)
    {
        if (!forceLocalMeshVisibleDuringScare || localMeshVisibility == null)
            return;

        localMeshVisibility.SetForcedLocalVisible(visible);
    }
}