using System.Collections;
using Mirror;
using UnityEngine;

public enum PayphoneState
{
    NeedsQuarter,
    Ringing,
    PickedUp,
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
    [SerializeField] private string pickUpPhonePrompt = "Pick Up Phone";
    [SerializeField] private string finishedPrompt = "The line is dead";

    [Header("Audio")]
    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private AudioClip insertMoneySound;
    [SerializeField] private AudioClip ringStartSound;
    [SerializeField] private AudioClip ringLoopSound;
    [SerializeField] private AudioClip treeStalkerVoiceLine;
    [SerializeField] private AudioClip putDownPhoneSound;
    [SerializeField] private AudioClip scarySting;
    [SerializeField] private float audioVolume = 1f;

    [Header("Completion Popup")]
    [SerializeField] private bool showCompletionPopup = true;
    [SerializeField] private string completionLocationText = "The Line Goes Dead";
    [SerializeField] private string completionObjectiveText = "The phone is useless. Explore the camp.";

    private Coroutine ringRoutine;

    private void Awake()
    {
        if (phoneInteractable == null)
            phoneInteractable = GetComponent<Interactable>();

        if (phoneAudioSource == null)
            phoneAudioSource = GetComponent<AudioSource>();

        if (phoneModel == null)
            phoneModel = transform;

    }

    private void Start()
    {
        UpdatePrompt();
    }
    public void InteractWithPhone()
    {
        if (!isServer) return;

        if (currentState == PayphoneState.NeedsQuarter)
        {
            InsertQuarter();
            return;
        }

        if (currentState == PayphoneState.Ringing)
        {
            PickUpPhone();
            return;
        }
    }

    [Server]
    private void InsertQuarter()
    {
        currentState = PayphoneState.Ringing;

        RpcPlayInsertQuarterFeedback();
        RpcStartRinging();
    }

    [Server]
    private void PickUpPhone()
    {
        currentState = PayphoneState.PickedUp;

        RpcPickUpPhoneFeedback();

        //after the voiceline finishes, mark the phone as finished
        float finishDelay = treeStalkerVoiceLine != null ? treeStalkerVoiceLine.length : 1f;
        StartCoroutine(FinishPhoneAfterDelay(finishDelay));
    }

    [Server]
    private IEnumerator FinishPhoneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        currentState = PayphoneState.Finished;

        RpcPlayPutDownPhoneSound();

        yield return new WaitForSeconds(putDownPhoneSound != null ? putDownPhoneSound.length : 0.2f);

        RpcShowCompletionPopup();
    }

    private void OnPhoneStateChanged(PayphoneState oldState, PayphoneState newState)
    {
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (phoneInteractable == null) return;

        if (currentState == PayphoneState.NeedsQuarter)
        {
            phoneInteractable.interactionPrompt = insertQuarterPrompt;
            phoneInteractable.isInteractable = true;
            return;
        }

        if (currentState == PayphoneState.Ringing)
        {
            phoneInteractable.interactionPrompt = pickUpPhonePrompt;
            phoneInteractable.isInteractable = true;
            return;
        }

        if (currentState == PayphoneState.PickedUp)
        {
            phoneInteractable.interactionPrompt = "...";
            phoneInteractable.isInteractable = false;
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
    private void RpcStartRinging()
    {
        StopRinging();

        ringRoutine = StartCoroutine(RingPhoneRoutine());
    }

    [ClientRpc]
    private void RpcPickUpPhoneFeedback()
    {
        StopRinging();

        if (phoneAudioSource != null && treeStalkerVoiceLine != null)
        {
            phoneAudioSource.loop = false;
            phoneAudioSource.clip = null;
            phoneAudioSource.PlayOneShot(treeStalkerVoiceLine, audioVolume);
        }
    }

    [ClientRpc]
    private void RpcPlayPutDownPhoneSound()
    {
        if (phoneAudioSource == null) return;
        if (putDownPhoneSound == null) return;

        phoneAudioSource.PlayOneShot(putDownPhoneSound, audioVolume);
    }

    [ClientRpc]
    private void RpcShowCompletionPopup()
    {
        if (!showCompletionPopup) return;

        if (LocalPopupUI.Instance != null)
        {
            LocalPopupUI.Instance.ShowPopup(completionLocationText, completionObjectiveText);
            phoneAudioSource.PlayOneShot(scarySting, audioVolume);  
            return;
        }
    }

    private IEnumerator RingPhoneRoutine()
    {
        if (phoneAudioSource == null) yield break;

        if (ringStartSound != null)
        {
            phoneAudioSource.PlayOneShot(ringStartSound, audioVolume);
            yield return new WaitForSeconds(ringStartSound.length);
        }

        if (ringLoopSound != null)
        {
            phoneAudioSource.clip = ringLoopSound;
            phoneAudioSource.loop = true;
            phoneAudioSource.volume = audioVolume;
            phoneAudioSource.Play();
        }
    }

    private void StopRinging()
    {
        if (ringRoutine != null)
        {
            StopCoroutine(ringRoutine);
            ringRoutine = null;
        }

        if (phoneAudioSource != null)
        {
            phoneAudioSource.loop = false;
            phoneAudioSource.Stop();
            phoneAudioSource.clip = null;
        }
    }
}