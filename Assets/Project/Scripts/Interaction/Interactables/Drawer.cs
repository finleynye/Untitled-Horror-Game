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

    [Header("Drawer State")]
    [SyncVar(hook = nameof(OnDrawerStateChanged))]
    [SerializeField] private bool isOpen;

    [Header("Stored Interactables")]
    [SerializeField] private Interactable[] storedInteractables;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (drawerInteractable == null)
            drawerInteractable = GetComponent<Interactable>();

        ApplyDrawerState(isOpen);
    }

    [Server]
    public void ToggleDrawer()
    {
        isOpen = !isOpen;
        ApplyDrawerState(isOpen);
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