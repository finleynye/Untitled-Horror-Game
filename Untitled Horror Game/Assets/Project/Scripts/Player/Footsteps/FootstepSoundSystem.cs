using Mirror;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSoundSystem : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Step Distance")]
    [SerializeField] private float walkStepDistance = 0.8f;
    [SerializeField] private float sprintStepDistance = 0.55f;
    [SerializeField] private float crouchStepDistance = 1.2f;

    [SerializeField] private float minStepDelay = 0.25f;
    private float stepDelayTimer;

    [Header("Audio")]
    [SerializeField] private SoundType footstepSound = SoundType.FOOTSTEP;
    [SerializeField] private float footstepVolume = 1f;

    private Vector3 lastPosition;
    private float distanceTravelled;

    private void Start()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!isOwned)
            return;

        stepDelayTimer -= Time.deltaTime;

        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        if (characterController == null)
            return;

        if (playerMovement == null)
            return;

        //only play footsteps while grounded
        if (characterController.isGrounded == false)
        {
            lastPosition = transform.position;
            distanceTravelled = 0f;
            return;
        }

        //only count horizontal movement
        Vector3 currentPosition = transform.position;
        Vector3 horizontalMovement = currentPosition - lastPosition;
        horizontalMovement.y = 0f;

        float movementAmount = horizontalMovement.magnitude;

        //ignore tiny movement jitter
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
        //play instantly for this player only if we are not the server/host
        //this avoids host mode double-playing the sound
        if (!isServer)
        {
            SoundManager.PlaySound(footstepSound, footstepVolume);
        }

        // ask the server to tell the other clients
        CmdPlayFootstep();
    }


    [Command]
    private void CmdPlayFootstep()
    {
        //server/host plays it once here
        SoundManager.PlaySound(footstepSound, footstepVolume);

        //then tell other clients
        RpcPlayFootstep();
    }

    [ClientRpc]
    private void RpcPlayFootstep()
    {
        //do not replay the sound on the player who owns this object
        if (isOwned)
            return;

        SoundManager.PlaySound(footstepSound, footstepVolume);
    }
}