using System.Collections;
using UnityEngine;
using Mirror;

public class DrawerInteractAction : NetworkBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openBoolName = "Open";

    [Header("Interaction Prompt")]
    [SerializeField] private Interactable drawerInteractable;
    [SerializeField] private string openPrompt = "Open Drawer";
    [SerializeField] private string closePrompt = "Close Drawer";

    [Header("Interaction Sounds")]
    [SerializeField] private AudioSource drawerSource;
    [SerializeField] private AudioClip openDrawer;
    [SerializeField] private AudioClip closeDrawer;
    [SerializeField] private AudioClip scareDrawer;

    [Header("Scare Sound Chance")]
    [SerializeField, Range(0f, 1f)] private float scareDrawerChance = 0.08f;

    [Header("Drawer State")]
    [SyncVar(hook = nameof(OnDrawerStateChanged))]
    [SerializeField] private bool isOpen;

    [Header("Stored Interactables")]
    [SerializeField] private Interactable[] storedInteractables;

    [SyncVar]
    private bool isBusy;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (drawerInteractable == null)
            drawerInteractable = GetComponent<Interactable>();

        if (drawerSource == null)
            drawerSource = GetComponent<AudioSource>();

        ApplyDrawerState(isOpen);
    }

    [Server]
    public void ToggleDrawer()
    {
        if (isBusy)
            return;

        bool opening = !isOpen;

        AudioClip clipToPlay = GetDrawerClip(opening);

        isOpen = opening;
        ApplyDrawerState(isOpen);

        RpcPlayDrawerSound(opening, clipToPlay == scareDrawer);

        StartCoroutine(UnlockAfterSound(clipToPlay != null ? clipToPlay.length : 0f));
    }

    [Server]
    private IEnumerator UnlockAfterSound(float delay)
    {
        isBusy = true;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        isBusy = false;
    }

    private AudioClip GetDrawerClip(bool opening)
    {
        if (opening)
            return openDrawer;

        if (scareDrawer != null && Random.value <= scareDrawerChance)
            return scareDrawer;

        return closeDrawer;
    }
    [ClientRpc]
    private void RpcPlayDrawerSound(bool opening, bool useScareSound)
    {
        if (drawerSource == null)
            return;

        AudioClip clipToPlay = useScareSound
            ? scareDrawer
            : opening ? openDrawer : closeDrawer;

        if (clipToPlay != null)
            drawerSource.PlayOneShot(clipToPlay);
    }

    private void OnDrawerStateChanged(bool oldValue, bool newValue)
    {
        ApplyDrawerState(newValue);
    }

    private void ApplyDrawerState(bool open)
    {
        if (animator != null)
            animator.SetBool(openBoolName, open);

        UpdateDrawerPrompt(open);
        UpdateStoredInteractables(open);
    }

    private void UpdateDrawerPrompt(bool open)
    {
        if (drawerInteractable == null)
            return;

        string newPrompt = open ? closePrompt : openPrompt;
        drawerInteractable.SetInteractionPrompt(newPrompt);
    }

    private void UpdateStoredInteractables(bool open)
    {
        foreach (Interactable storedInteractable in storedInteractables)
        {
            if (storedInteractable == null)
                continue;

            storedInteractable.isInteractable = open;

            if (storedInteractable.interactWidget != null && !open)
                storedInteractable.interactWidget.SetActive(false);
        }
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}