using Mirror;
using UnityEngine;
using System.Collections;
public class ObjectiveTrigger : MonoBehaviour
{
    [Header("Popup Text")]
    [SerializeField] private string locationText = "Camp Hardwood";
    [SerializeField] private string objectiveText = "Find a Payphone";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popupSound;
    [SerializeField] private float popupSoundVolume = 1f;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool disableColliderAfterTrigger = true;
    [SerializeField] private bool destroyAfterAudio = false;

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
        if (triggerOnce && hasTriggeredLocally)
            return;

        NetworkIdentity networkIdentity = other.GetComponentInParent<NetworkIdentity>();

        if (networkIdentity == null)
            return;

        //only the local players body should trigger this popup
        if (!networkIdentity.isLocalPlayer && !networkIdentity.isOwned)
            return;

        hasTriggeredLocally = true;

        ShowPopup();
        PlayPopupSound();

        if (disableColliderAfterTrigger && triggerCollider != null)
            triggerCollider.enabled = false;

        if (destroyAfterAudio)
            StartCoroutine(DestroyAfterAudioRoutine());
    }

    private void ShowPopup()
    {
        if (LocalPopupUI.Instance == null)
        {
            Debug.LogWarning("No LocalPopupUI found in scene.");
            return;
        }

        LocalPopupUI.Instance.ShowPopup(locationText, objectiveText);
    }

    private void PlayPopupSound()
    {
        if (audioSource == null)
            return;

        if (popupSound == null)
            return;

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