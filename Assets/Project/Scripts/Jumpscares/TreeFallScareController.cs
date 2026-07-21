using System.Collections;
using Mirror;
using UnityEngine;

public class TreeFallScareController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerScareController playerScare;

    [Header("Tree Fall Animation")]
    [SerializeField] private string fallTriggerName = "FallBack";
    [SerializeField, Min(0f)] private float totalScareDuration = 3f;

    [Header("Tree Fall Root Motion")]
    [SerializeField] private bool useRootMotionForFall = true;
    [SerializeField] private bool moveControllerWithRootMotion;
    [SerializeField] private bool applyRootYawRotation;
    [SerializeField] private float scareGravity = -18f;

    private Transform playerRoot;

    private bool isPlayingScare;
    private bool scareRootMotionActive;

    private float scareVerticalVelocity;

    //position captured from anim event near end of get up
    private bool hasGetUpEventPosition;
    private Vector3 getUpEventPosition;
    private Quaternion getUpEventRotation;

    public bool IsPlayingScare => isPlayingScare;
    private void Awake()
    {
        if (playerScare == null)
            playerScare = GetComponent<PlayerScareController>();

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        playerRoot = characterController != null ? characterController.transform : transform;
    }
    public void PlayTreeFallScare()
    {
        if (!isOwned || isPlayingScare)
            return;

        if (!CanPlayLocalScare())
            return;


        //tell other clients to only play visual version
        CmdPlayTreeFallScareForObservers();
        StartCoroutine(TreeFallRoutine());
    }
    private bool CanPlayLocalScare()
    {
        return playerScare != null && characterController != null;
    }

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

    private IEnumerator TreeFallRoutine()
    {
        if (playerScare == null)
            yield break;

        hasGetUpEventPosition = false;
        isPlayingScare = true;

        bool started = playerScare.BeginLocalScare(
            fallTriggerName,
            totalScareDuration,
            audioClip: null,
            followCamera: true);

        if (!started)
        {
            isPlayingScare = false;
            yield break;
        }

        if (useRootMotionForFall)
            yield return RunRootMotionScareForDuration();
        else
            yield return RunNonRootMotionScareForDuration(
                totalScareDuration);

        CompleteLocalTreeFall();
    }
    private void CompleteLocalTreeFall()
    {
        playerScare.FinishLocalScare(hasGetUpEventPosition, getUpEventPosition, getUpEventRotation);

        scareRootMotionActive = false;
        isPlayingScare = false;
    }
    private IEnumerator RemoteVisualFallRoutine()
    {
        if (playerScare == null)
            yield break;

        isPlayingScare = true;

        bool started =
            playerScare.BeginRemoteScare(fallTriggerName);

        if (!started)
        {
            isPlayingScare = false;
            yield break;
        }

        yield return new WaitForSeconds(totalScareDuration);

        playerScare.FinishRemoteScare();

        isPlayingScare = false;
    }
    private void OnAnimatorMove()
    {
        if (!isOwned || !scareRootMotionActive || playerScare == null || characterController == null)
            return;
        
        Vector3 movement = Vector3.zero;

        if (moveControllerWithRootMotion)
        {
            Vector3 rootDelta =
                playerScare.GetAnimatorDeltaPosition();

            movement += new Vector3(rootDelta.x, 0f, rootDelta.z);
        }

        UpdateVerticalVelocity();

        //y is handled by our own gravity below
        movement += Vector3.up * (scareVerticalVelocity * Time.deltaTime);

        characterController.Move(movement);
        Physics.SyncTransforms();

        if (applyRootYawRotation && moveControllerWithRootMotion && playerRoot != null)
        {
            //only copy yaw so animation cant tilt player root
            float rootYaw = playerScare.GetAnimatorDeltaRotation().eulerAngles.y;
            playerRoot.Rotate(0f, rootYaw, 0f);
        }
    }
    private IEnumerator RunRootMotionScareForDuration()
    {
        BeginRootMotionScare();

        yield return WaitForScareDuration();

        EndRootMotionScare();
    }
    private void BeginRootMotionScare()
    {
        if (playerScare == null)
            return;

        scareVerticalVelocity = -2f;
        playerScare.EnableRootMotion();
        scareRootMotionActive = true;
    }
    private void EndRootMotionScare()
    {
        scareRootMotionActive = false;
        playerScare?.RestoreRootMotion();
    }
    private IEnumerator RunNonRootMotionScareForDuration(float duration)
    {
        scareVerticalVelocity = -2f;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            UpdateVerticalVelocity();

            Vector3 movement = Vector3.up * (scareVerticalVelocity * Time.deltaTime);

            characterController.Move(movement);
            Physics.SyncTransforms();

            yield return null;
        }
    }
    private IEnumerator WaitForScareDuration()
    {
        float timer = 0f;

        while (timer < totalScareDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }
    private void UpdateVerticalVelocity()
    {
        if (characterController == null)
            return;

        if (characterController.isGrounded && scareVerticalVelocity < 0f)//small downward force keeps controller grounded
            scareVerticalVelocity = -2f;
        
        scareVerticalVelocity += scareGravity * Time.deltaTime;
    }
    public void SetGetUpRestorePosition(Vector3 worldPosition, Quaternion worldRotation)
    {
        //only accept first valid anim event from the local scare
        if (!isOwned || !isPlayingScare || hasGetUpEventPosition)
            return;
       
        hasGetUpEventPosition = true;
        getUpEventPosition = worldPosition;
        getUpEventRotation = worldRotation;
    }
}