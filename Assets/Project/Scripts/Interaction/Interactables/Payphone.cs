using System.Collections;
using Mirror;
using UnityEngine;

public enum PayphoneState
{
    NeedsQuarter,
    Finished
}

public class Payphone : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Interactable phoneInteractable;
    [SerializeField] private Transform phoneModel;

    [Header("Phone State")]
    [SyncVar(hook = nameof(OnPhoneStateChanged))]
    [SerializeField] private PayphoneState currentState = PayphoneState.NeedsQuarter;

    [Header("Prompt Text")]
    [SerializeField] private string insertQuarterPrompt = "Insert Quarter";
    [SerializeField] private string waitingPrompt = "...";
    [SerializeField] private string finishedPrompt = "The line is dead";

    [Header("Dead Line Timing")]
    [SerializeField] private float deadLineDelay = 1.2f;

    [Header("Phone Animation")]
    [SerializeField] private Animator phoneAnimator;
    [SerializeField] private string deadTriggerName = "GoDead";
    [SerializeField] private string idleTriggerName = "GoIdle";

    [Header("Audio")]
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip insertMoneySound;
    [SerializeField] private AudioClip lineDeadSound;
    [SerializeField] private AudioClip scarySting;
    [SerializeField] private float audioVolume = 1f;

    [Header("Completion Popup")]
    [SerializeField] private bool showCompletionPopup = true;
    [SerializeField] private string completionLocationText = "The Line Goes Dead";
    [SerializeField] private string completionObjectiveText = "The phone is useless. Explore the camp.";

    private bool isProcessingPhone;

    private void Awake()
    {
        if (phoneInteractable == null)
            phoneInteractable = GetComponent<Interactable>();

        if (phoneAudioSource == null)
            phoneAudioSource = GetComponent<AudioSource>();

        if (phoneModel == null)
            phoneModel = transform;

        if (phoneAnimator == null)
            phoneAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        UpdatePrompt();
    }

    public void InteractWithPhone()
    {
        if (isServer)
        {
            ServerInteractWithPhone();
            return;
        }

        CmdInteractWithPhone();
    }

    [Command(requiresAuthority = false)]
    private void CmdInteractWithPhone()
    {
        ServerInteractWithPhone();
    }

    [Server]
    private void ServerInteractWithPhone()
    {
        if (currentState != PayphoneState.NeedsQuarter)
            return;

        if (isProcessingPhone)
            return;

        StartCoroutine(InsertQuarterThenKillLine());
    }

    [Server]
    private IEnumerator InsertQuarterThenKillLine()
    {
        isProcessingPhone = true;

        RpcPlayInsertQuarterFeedback();
        RpcSetWaitingPrompt();

        yield return new WaitForSeconds(deadLineDelay);

        currentState = PayphoneState.Finished;

        RpcPayphoneGoesDead();

        isProcessingPhone = false;
    }

    private void OnPhoneStateChanged(PayphoneState oldState, PayphoneState newState)
    {
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (phoneInteractable == null)
            return;

        if (currentState == PayphoneState.NeedsQuarter)
        {
            phoneInteractable.interactionPrompt = insertQuarterPrompt;
            phoneInteractable.isInteractable = true;
            return;
        }

        if (currentState == PayphoneState.Finished)
        {
            phoneInteractable.interactionPrompt = finishedPrompt;
            phoneInteractable.isInteractable = false;
        }
    }

    [ClientRpc]
    private void RpcPlayInsertQuarterFeedback()
    {
        if (phoneAudioSource != null && insertMoneySound != null)
            phoneAudioSource.PlayOneShot(insertMoneySound, audioVolume);
    }

    [ClientRpc]
    private void RpcSetWaitingPrompt()
    {
        if (phoneInteractable == null)
            return;

        phoneInteractable.interactionPrompt = waitingPrompt;
        phoneInteractable.isInteractable = false;
    }

    [ClientRpc]
    private void RpcPayphoneGoesDead()
    {
        PlayDeadFeedback();
        ShowDeadPopup();
    }

    private void PlayDeadFeedback()
    {
        if (phoneAudioSource != null)
        {
            phoneAudioSource.loop = false;
            phoneAudioSource.Stop();
            phoneAudioSource.clip = null;

            if (lineDeadSound != null)
                phoneAudioSource.PlayOneShot(lineDeadSound, audioVolume);
        }

        if (phoneAnimator != null && !string.IsNullOrEmpty(deadTriggerName))
            phoneAnimator.SetTrigger(deadTriggerName);

        StartCoroutine(ReturnToIdleAfterLineSound());
    }
    private IEnumerator ReturnToIdleAfterLineSound()
    {
        if (lineDeadSound != null)
            yield return new WaitForSeconds(lineDeadSound.length);
        else
            yield return new WaitForSeconds(1f);

        if (phoneAnimator != null && !string.IsNullOrEmpty(idleTriggerName))
            phoneAnimator.SetTrigger(idleTriggerName);
    }

    private void ShowDeadPopup()
    {
        if (!showCompletionPopup)
            return;

        if (LocalPopupUI.Instance != null)
            LocalPopupUI.Instance.ShowPopup(completionLocationText, completionObjectiveText);

        if (phoneAudioSource != null && scarySting != null)
            phoneAudioSource.PlayOneShot(scarySting, audioVolume);
    }
}