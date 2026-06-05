using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Mirror;

public class Interactable : NetworkBehaviour
{
    [Header("Event System Based")]
    public UnityEvent InteractEvent;//assign event to interactable to have it perform whatever code you'd like by creating a new script for an action like shooting and assigning it here

    [Header("UI Prompt")]
    public string interactionPrompt = "Interact";//default text to display on screen for interaction

    [Header("Distance & Activation")]
    [SerializeField] private SphereCollider interactableRadius;
    [SerializeField] private float resetTimer = 3f; //reset timer after an interaction is complete
    private float timeElapsed = 0f;

    [Header("Player Ref & States")]
    public Transform playerTransform;
    public bool isNearInteractable = false;//checks true or false when the player is within a certain distance of the interactable
    public bool isCarryingInteractable = false;//checks true or false dependant on whether you carry the interactable
    public bool isInteractable = true;//toggle this for objects that you want to have the script but don't want to be interactable yet (could be an event)

    [Header("Interaction State")]
    [SerializeField] private bool hasInteracted;

    [Tooltip("Toggle this if you would like the interaction to be reused.")]
    public bool isReusable = false;

    [Header("UI")]
    public GameObject interactWidget;
    private TextMeshProUGUI textPrompt;

    private void Start()
    {
        if (interactableRadius == null)
            interactableRadius = GetComponent<SphereCollider>();

        if (interactableRadius != null)
            interactableRadius.isTrigger = true;

        if (interactWidget != null)
        {
            textPrompt = interactWidget.GetComponentInChildren<TextMeshProUGUI>();
            interactWidget.SetActive(false);
        }

        if (textPrompt != null)
            textPrompt.text = interactionPrompt;
    }

    private void Update()
    {
        if (hasInteracted && isReusable)
        {
            timeElapsed += Time.deltaTime;

            if (timeElapsed >= resetTimer)
                ResetInteraction();
        }

        if (textPrompt != null)
            textPrompt.text = interactionPrompt;

        UpdatePrompt();
    }

    //using collider based triggers to get the nearest player
    private void OnTriggerEnter(Collider other)
    {
        TrySetNearbyPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySetNearbyPlayer(other);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement exitingPlayer = other.GetComponentInParent<PlayerMovement>();

        if (exitingPlayer == null)
            return;

        if (!exitingPlayer.isOwned)
            return;

        if (playerTransform == exitingPlayer.transform)
        {
            playerTransform = null;
            isNearInteractable = false;
            UpdatePrompt();
        }
    }

    private void TrySetNearbyPlayer(Collider other)
    {
        PlayerMovement nearbyPlayer = other.GetComponentInParent<PlayerMovement>();

        if (nearbyPlayer == null)
            return;

        //only let the local player control this prompt
        if (!nearbyPlayer.isOwned)
            return;

        playerTransform = nearbyPlayer.transform;
        isNearInteractable = true;

        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (interactWidget == null)
            return;

        bool canShowPrompt =
            isNearInteractable &&
            isInteractable &&
            (!hasInteracted || isReusable);

        interactWidget.SetActive(canShowPrompt);
    }

    public void Interact()
    {
        hasInteracted = true;

        if (interactWidget != null)
            interactWidget.SetActive(false);

        timeElapsed = 0f;

        InteractEvent?.Invoke();
    }

    [Server]
    public void ServerInteract()
    {
        if (!isInteractable)
            return;

        if (hasInteracted && !isReusable)
            return;

        hasInteracted = true;
        timeElapsed = 0f;

        InteractEvent?.Invoke();

        RpcAfterInteract();
    }

    [ClientRpc]
    private void RpcAfterInteract()
    {
        if (interactWidget != null)
            interactWidget.SetActive(false);
    }

    public void ResetInteraction()
    {
        hasInteracted = false;
        timeElapsed = 0f;
        UpdatePrompt();
    }
}