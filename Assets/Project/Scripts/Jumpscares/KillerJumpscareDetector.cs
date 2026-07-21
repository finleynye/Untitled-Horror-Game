using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class KillerJumpscareDetector : NetworkBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference interactionAction;

    [Header("Jumpscare")]
    [SerializeField] private Animator animator;

    [Header("Paired Scare")]
    [SerializeField] private PairedScareDefinition scareDefinition;
    [SerializeField] private PlayerScareController killerScareController;

    [Header("Reusable Jumpscare Modules")]
    [SerializeField] private JumpscareTargetFinder targetFinder = new JumpscareTargetFinder();
    [SerializeField] private JumpscareVictimModule victim = new JumpscareVictimModule();
    [SerializeField] private JumpscareArmIKModule armIK = new JumpscareArmIKModule();

    private GameObject currentTarget;
    private Coroutine armIkResetRoutine;
    private Coroutine serverTimeoutRoutine;
    private uint activeVictimNetId;
    private bool isJumpscaring;

    private static readonly HashSet<uint> ActiveParticipants = new HashSet<uint>();

    public GameObject CurrentTarget => currentTarget;
    public bool IsJumpscaring => isJumpscaring;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (killerScareController == null)
            killerScareController = GetComponentInParent<PlayerScareController>();

        if (killerScareController == null)
            killerScareController = GetComponentInChildren<PlayerScareController>();

        //victim?.Initialise(this);
        armIK?.Initialise(animator);
    }

    private void OnEnable()
    {
        if (!isOwned || interactionAction?.action == null)
            return;

        interactionAction.action.Enable();
        interactionAction.action.performed += OnInteractionPerformed;
    }

    private void OnDisable()
    {
        interactionAction.action.Disable();
        if (interactionAction?.action != null)
            interactionAction.action.performed -= OnInteractionPerformed;

        if (isServer)
            ServerFinishJumpscare(activateRagdoll: false);

        CleanupJumpscareWithoutRagdoll();
    }

    private void Update()
    {
        if (!isOwned || isJumpscaring)
            return;

        currentTarget = targetFinder?.FindBestTarget(transform);
    }

    private void OnInteractionPerformed(InputAction.CallbackContext context)
    {
        TryJumpscare();
    }

    private void TryJumpscare()
    {
        if (!isOwned || isJumpscaring)
            return;

        currentTarget = targetFinder?.FindBestTarget(transform);

        JumpscareTarget target = currentTarget.GetComponent<JumpscareTarget>();

        if (target.NetId == 0)
        {
            BeginLocalPlaceholderJumpscare(target);
            return;
        }

        CmdTryStartJumpscare(target.NetId);
    }

    //called by the existing animation event at the end of the jumpscare
    public void EndJumpscare()
    {
        if (!isOwned || !isJumpscaring)
            return;

        if (activeVictimNetId == 0)
        {
            FinishClientPresentation(activateRagdoll: true);
            return;
        }

        if (isServer)
            ServerFinishJumpscare(activateRagdoll: true);
        else
            CmdFinishJumpscare();
    }

    [Command]
    private void CmdTryStartJumpscare(uint victimNetId)
    {
        if (!ServerCanStartJumpscare(victimNetId, out _, out string rejectionReason))
            return;
        
        activeVictimNetId = victimNetId;
        isJumpscaring = true;
        ActiveParticipants.Add(netId);
        ActiveParticipants.Add(victimNetId);

        RpcBeginJumpscare(victimNetId, transform.position, transform.rotation);
        RestartServerTimeout();
    }

    [Command]
    private void CmdFinishJumpscare()
    {
        ServerFinishJumpscare(activateRagdoll: true);
    }

    [ClientRpc]
    private void RpcBeginJumpscare(uint victimNetId, Vector3 killerPosition, Quaternion killerRotation)
    {
        if (!ResolveTarget(victimNetId, out JumpscareTarget target))
            return;
        
        BeginClientPresentation(target, killerPosition, killerRotation);
    }

    [ClientRpc]
    private void RpcFinishJumpscare(uint victimNetId, bool activateRagdoll)
    {
        FinishClientPresentation(activateRagdoll);
    }

    private bool ServerCanStartJumpscare(uint victimNetId, out JumpscareTarget serverTarget, out string rejectionReason)
    {
        serverTarget = null;
        rejectionReason = string.Empty;

        PlayerScareController targetScareController = serverTarget.ScareController;

        GameObject bestTarget = targetFinder?.FindBestTarget(transform);
        JumpscareTarget bestNetworkTarget = bestTarget.GetComponent<JumpscareTarget>();

        return true;
    }

    private void BeginLocalPlaceholderJumpscare(JumpscareTarget target)
    {
        BeginClientPresentation(target, transform.position, transform.rotation);

        if (isJumpscaring)
            StartCoroutine(LocalPlaceholderTimeoutRoutine());
    }

    private IEnumerator LocalPlaceholderTimeoutRoutine()
    {
        float timeout = Mathf.Max(0.1f, scareDefinition.duration + 0.5f);
        yield return new WaitForSeconds(timeout);

        if (isJumpscaring && activeVictimNetId == 0)
            FinishClientPresentation(activateRagdoll: true);
    }

    private void BeginClientPresentation(JumpscareTarget target, Vector3 killerPosition, Quaternion killerRotation)
    {
        if (scareDefinition == null || target == null)
            return;

        StopArmIkResetRoutine();
        CleanupJumpscareWithoutRagdoll();

        currentTarget = target.gameObject;
        killerScareController?.AlignPlayer(killerPosition, killerRotation);

        bool victimGrabbed = victim == null || victim.Grab(target, scareDefinition.victimLocalPosition, Quaternion.Euler(scareDefinition.victimLocalEuler));

        bool killerIsLocal = isOwned;
        bool victimIsLocal = target.IsLocallyOwned;

        bool killerStarted = TryBeginScareController(
            killerScareController,
            killerIsLocal,
            scareDefinition.killerTrigger,
            scareDefinition.killerAudio,
            followCamera: false);

        if (!killerStarted)
            killerStarted = TriggerAnimator(animator, scareDefinition.killerTrigger);

        PlayerScareController victimScareController = target.ScareController;
        bool victimStarted = TryBeginScareController(
            victimScareController,
            victimIsLocal,
            scareDefinition.victimTrigger,
            scareDefinition.victimAudio,
            scareDefinition.followVictimHead);

        if (!victimStarted)
            victimStarted = TriggerAnimator(target.GetComponentInChildren<Animator>(), scareDefinition.victimTrigger);

        if (!killerStarted && !victimStarted && !victimGrabbed)
        {
            CleanupJumpscareWithoutRagdoll();
            return;
        }

        activeVictimNetId = target.NetId;
        isJumpscaring = true;
        armIK?.Activate();
        victim?.SetJumpscareLook(true);
    }

    private void FinishClientPresentation(bool activateRagdoll)
    {
        if (!isJumpscaring && armIkResetRoutine == null)
            return;

        bool killerWasLocal = isOwned;
        PlayerScareController victimScareController = victim?.ScareController;
        bool victimWasLocal = ResolveTarget(activeVictimNetId, out JumpscareTarget target) && target.IsLocallyOwned;

        victim?.SetJumpscareLook(false);
        victim?.Release(activateRagdoll);

        FinishScareController(victimScareController, victimWasLocal);
        FinishScareController(killerScareController, killerWasLocal);

        if (armIK == null)
        {
            CompleteJumpscare();
            return;
        }

        StopArmIkResetRoutine();
        armIkResetRoutine = StartCoroutine(ResetArmIkRoutine());
    }

    private bool TryBeginScareController(PlayerScareController controller, bool isLocal, string trigger, AudioClip audioClip, bool followCamera)
    {
        if (controller == null)
            return false;

        return isLocal ? controller.BeginLocalScare(trigger, scareDefinition.duration, audioClip, followCamera) : controller.BeginRemoteScare(trigger, audioClip);
    }

    private void FinishScareController(PlayerScareController controller, bool wasLocal)
    {
        if (controller == null)
            return;

        if (wasLocal)
            controller.FinishLocalScare();
        else
            controller.FinishRemoteScare();
    }

    private bool TriggerAnimator(Animator targetAnimator, string trigger)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(trigger))
            return false;

        targetAnimator.ResetTrigger(trigger);
        targetAnimator.SetTrigger(trigger);
        return true;
    }

    [Server]
    private void ServerFinishJumpscare(bool activateRagdoll)
    {
        if (!isJumpscaring)
            return;

        StopServerTimeout();

        uint victimNetId = activeVictimNetId;
        ActiveParticipants.Remove(netId);
        ActiveParticipants.Remove(victimNetId);

        RpcFinishJumpscare(victimNetId, activateRagdoll);

        isJumpscaring = false;
        activeVictimNetId = 0;
    }

    [Server]
    private void RestartServerTimeout()
    {
        StopServerTimeout();

        float timeout = Mathf.Max(0.1f, scareDefinition.duration + 0.5f);
        serverTimeoutRoutine = StartCoroutine(ServerTimeoutRoutine(timeout));
    }

    [Server]
    private void StopServerTimeout()
    {
        if (serverTimeoutRoutine == null)
            return;

        StopCoroutine(serverTimeoutRoutine);
        serverTimeoutRoutine = null;
    }

    [Server]
    private IEnumerator ServerTimeoutRoutine(float timeout)
    {
        yield return new WaitForSeconds(timeout);

        serverTimeoutRoutine = null;
        ServerFinishJumpscare(activateRagdoll: true);
    }

    private IEnumerator ResetArmIkRoutine()
    {
        yield return armIK.ResetRoutine();

        armIkResetRoutine = null;
        CompleteJumpscare();
    }

    private void CompleteJumpscare()
    {
        currentTarget = null;
        activeVictimNetId = 0;
        isJumpscaring = false;
    }

    private void CleanupJumpscareWithoutRagdoll()
    {
        StopArmIkResetRoutine();

        victim?.Cleanup();
        armIK?.ResetImmediately();

        currentTarget = null;
        activeVictimNetId = 0;
        isJumpscaring = false;
    }

    private void StopArmIkResetRoutine()
    {
        if (armIkResetRoutine == null)
            return;

        StopCoroutine(armIkResetRoutine);
        armIkResetRoutine = null;
    }

    private bool ResolveTarget(uint victimNetId, out JumpscareTarget target)
    {
        target = null;

        if (victimNetId == 0)
            return false;

        if (!NetworkClient.spawned.TryGetValue(victimNetId, out NetworkIdentity identity))
            return false;

        return TryGetActiveTarget(identity, out target);
    }

    [Server]
    private bool ResolveServerTarget(uint victimNetId, out JumpscareTarget target)
    {
        target = null;

        if (!NetworkServer.spawned.TryGetValue(victimNetId, out NetworkIdentity identity))
            return false;

        return TryGetActiveTarget(identity, out target);
    }

    private static bool TryGetActiveTarget(NetworkIdentity identity, out JumpscareTarget target)
    {
        target = null;

        if (identity == null)
            return false;

        JumpscareTarget[] targets = identity.GetComponentsInChildren<JumpscareTarget>(false);

        foreach (JumpscareTarget candidate in targets)
        {
            if (candidate.isActiveAndEnabled)
            {
                target = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool Reject(string reason, out string rejectionReason)
    {
        rejectionReason = reason;
        return false;
    }
}


