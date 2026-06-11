using System.Collections;
using Mirror;
using UnityEngine;

public class LocalPopupTrigger : MonoBehaviour
{
    [Header("Popup Text")]
    [SerializeField] private string locationText = "Welcome to Camp HardWood";
    [SerializeField] private string objectiveText = "Call for Help - Find a Payphone";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popupSound;
    [SerializeField] private float popupSoundVolume = 1f;

    [Header("Trigger Settings")]
    [SerializeField] private bool destroyAfterLocalTrigger = true;

    private bool hasTriggeredLocally;
    private Collider triggerCollider;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        triggerCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggeredLocally) return;

        NetworkIdentity networkIdentity = other.GetComponentInParent<NetworkIdentity>();

        if (networkIdentity == null) return;

        //only trigger for the local client's own player
        if (!networkIdentity.isLocalPlayer) return;

        hasTriggeredLocally = true;

        //stop this trigger being used again, but keep the object alive for audio
        if (triggerCollider != null)
            triggerCollider.enabled = false;

        ShowPopup();
        PlayPopupSound();

        if (destroyAfterLocalTrigger)
            StartCoroutine(DestroyAfterAudioRoutine());
    }

    private void ShowPopup()
    {
        if (LocalPopupUI.Instance != null)
        {
            LocalPopupUI.Instance.ShowPopup(locationText, objectiveText);
            return;
        }
    }

    private void PlayPopupSound()
    {
        if (audioSource == null) return;
        if (popupSound == null) return;

        audioSource.PlayOneShot(popupSound, popupSoundVolume);
    }

    private IEnumerator DestroyAfterAudioRoutine()
    {
        if (audioSource == null || popupSound == null)
        {
            Destroy(gameObject);
            yield break;
        }

        yield return new WaitForSeconds(popupSound.length);

        Destroy(gameObject);
    }
}