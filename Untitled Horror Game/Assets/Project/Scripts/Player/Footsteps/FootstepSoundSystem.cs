using Mirror;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepSoundSystem : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private AudioSource footstepAudioSource;

    [Header("Step Distance")]
    [SerializeField] private float walkStepDistance = 0.8f;
    [SerializeField] private float sprintStepDistance = 0.55f;
    [SerializeField] private float crouchStepDistance = 1.2f;

    [SerializeField] private float minStepDelay = 0.25f;
    private float stepDelayTimer;

    [Header("Footstep Audio")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepVolume = 1f;

    [Header("Jump & Land Audio")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private float jumpVolume = 1f;
    [SerializeField] private float landVolume = 1f;

    [Header("Pitch")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    private Transform playerRoot;
    private Vector3 lastPosition;
    private float distanceTravelled;

    private bool wasGroundedLastFrame;

    private void Awake()
    {
        if (footstepAudioSource == null)
            footstepAudioSource = GetComponent<AudioSource>();

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (playerMovement != null)
            playerRoot = playerMovement.transform;
        else
            playerRoot = transform.root;
    }

    private void Start()
    {
        if (playerRoot != null)
            lastPosition = playerRoot.position;

        if (characterController != null)
            wasGroundedLastFrame = characterController.isGrounded;
    }

    private void Update()
    {
        if (!isOwned)
            return;

        if (playerRoot == null)
            return;

        stepDelayTimer -= Time.deltaTime;

        HandleLanding();
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        if (characterController == null)
            return;

        if (playerMovement == null)
            return;

        if (!characterController.isGrounded)
        {
            lastPosition = playerRoot.position;
            distanceTravelled = 0f;
            return;
        }

        Vector3 currentPosition = playerRoot.position;
        Vector3 horizontalMovement = currentPosition - lastPosition;
        horizontalMovement.y = 0f;

        float movementAmount = horizontalMovement.magnitude;

        if (movementAmount <= 0.001f)
        {
            lastPosition = currentPosition;
            return;
        }

        distanceTravelled += movementAmount;

        float currentStepDistance = GetCurrentStepDistance();

        if (distanceTravelled >= currentStepDistance && stepDelayTimer <= 0f)
        {
            distanceTravelled = 0f;
            stepDelayTimer = minStepDelay;

            PlayFootstep();
        }

        lastPosition = currentPosition;
    }

    private void HandleLanding()
    {
        if (characterController == null)
            return;

        bool isGroundedNow = characterController.isGrounded;

        if (!wasGroundedLastFrame && isGroundedNow)
            PlayLandSound();
        

        wasGroundedLastFrame = isGroundedNow;
    }

    private float GetCurrentStepDistance()
    {
        if (playerMovement.IsCrouching)
            return crouchStepDistance;

        bool isMovingForward = playerMovement._moveInput.y > 0.1f;

        if (playerMovement._isSprinting && isMovingForward)
            return sprintStepDistance;

        return walkStepDistance;
    }

    private void PlayFootstep()
    {
        int clipIndex = GetRandomFootstepClipIndex();

        if (clipIndex < 0)
            return;

        if (!isServer)
            PlayFootstepLocal(clipIndex);

        CmdPlayFootstep(clipIndex);
    }

    public void PlayJumpSound()
    {
        if (jumpClip == null)
            return;

        if (!isOwned)
            return;

        if (!isServer)
            PlayJumpSoundLocal();

        CmdPlayJumpSound();
    }

    private void PlayLandSound()
    {
        if (landClip == null)
            return;

        if (!isServer)
            PlayLandSoundLocal();

        CmdPlayLandSound();
    }

    [Command]
    private void CmdPlayFootstep(int clipIndex)
    {
        PlayFootstepLocal(clipIndex);
        RpcPlayFootstep(clipIndex);
    }

    [ClientRpc]
    private void RpcPlayFootstep(int clipIndex)
    {
        if (isOwned)
            return;

        PlayFootstepLocal(clipIndex);
    }

    [Command]
    private void CmdPlayJumpSound()
    {
        PlayJumpSoundLocal();
        RpcPlayJumpSound();
    }

    [ClientRpc]
    private void RpcPlayJumpSound()
    {
        if (isOwned)
            return;

        PlayJumpSoundLocal();
    }

    [Command]
    private void CmdPlayLandSound()
    {
        PlayLandSoundLocal();
        RpcPlayLandSound();
    }

    [ClientRpc]
    private void RpcPlayLandSound()
    {
        if (isOwned)
            return;

        PlayLandSoundLocal();
    }

    private void PlayFootstepLocal(int clipIndex)
    {
        if (footstepAudioSource == null)
            return;

        if (footstepClips == null || footstepClips.Length == 0)
            return;

        if (clipIndex < 0 || clipIndex >= footstepClips.Length)
            return;

        AudioClip selectedClip = footstepClips[clipIndex];

        if (selectedClip == null)
            return;

        PlayClipLocal(selectedClip, footstepVolume);
    }

    private void PlayJumpSoundLocal()
    {
        PlayClipLocal(jumpClip, jumpVolume);
    }

    private void PlayLandSoundLocal()
    {
        PlayClipLocal(landClip, landVolume);
    }

    private void PlayClipLocal(AudioClip clip, float volume)
    {
        if (footstepAudioSource == null)
            return;

        if (clip == null)
            return;

        footstepAudioSource.pitch = Random.Range(minPitch, maxPitch);
        footstepAudioSource.PlayOneShot(clip, volume);
    }

    private int GetRandomFootstepClipIndex()
    {
        if (footstepClips == null || footstepClips.Length == 0)
            return -1;

        return Random.Range(0, footstepClips.Length);
    }
}