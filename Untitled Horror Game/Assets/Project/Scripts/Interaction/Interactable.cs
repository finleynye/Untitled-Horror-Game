using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Interactable : MonoBehaviour
{
    [Header("Event System Based")]
    public UnityEvent InteractEvent; //assign event to interactable to have it perform whatever code you'd like by creating a new script for an action like shooting and assigning it here

    [Header("UI Prompt")]
    public string interactionPrompt = "Interact"; //default text to display on screen for interaction

    [Header("Distance & Activation")]
    [SerializeField] private float activationDistance = 3f; //distance in which the UI prompt and interaction happens
    [SerializeField] private float resetTimer = 3f; //reset timer after an interaction is complete
    private float timeElapsed = 0;

    [Header("Player Ref & States")]
    public Transform playerTransform;
    public bool isNearInteractable = false; //checks true or false when the player is within a certain distance of the interactable
    public bool isCarryingInteractable = false; //checks true or false dependant on whether you carry the interactable
    public bool isInteractable = false; //toggle this for objects that you want to have the script but don't want to be interactable yet (could be an event)

    [Header("Interaction State")]
    [SerializeField] private bool hasInteracted;

    [Tooltip("Toggle This if you would like the interaction to be reused.")]
    public bool isReusable = false;

    [Header("UI")]
    public GameObject interactWidget;
    private TextMeshProUGUI textPrompt;

    private void Start()
    {
        //get the text component from the interaction widget
        if (interactWidget != null)
        {
            textPrompt = interactWidget.GetComponentInChildren<TextMeshProUGUI>();
            interactWidget.SetActive(false);
        }

        //set the prompt text to match the chosen interaction prompt
        if (textPrompt != null)
        {
            textPrompt.text = interactionPrompt;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player")?.transform;
            if (playerTransform == null) return;
        }

        if (hasInteracted && isReusable)
        {
            timeElapsed += Time.deltaTime;
            if (timeElapsed >= resetTimer)
                ResetInteraction();
        }

        //get the current players position and center
        Vector3 playerPos = playerTransform.position;

        Collider playerCollider = playerTransform.GetComponent<Collider>();

        if (playerCollider != null)
            playerPos = playerCollider.bounds.center;

        //distance check from interactable to player
        float distance = Vector3.Distance(transform.position, playerPos);
        isNearInteractable = distance < activationDistance;

        isInteractable = isNearInteractable;

        //show or hide the interaction widget depending on whether the player is close enough
        if (interactWidget != null)
        {
            bool canShowPrompt = isInteractable && (!hasInteracted || isReusable);
            interactWidget.SetActive(canShowPrompt);
        }

        //keep the prompt text updated in case it is changed in the inspector
        if (textPrompt != null)
        {
            textPrompt.text = interactionPrompt;
        }
    }

    public void Interact()
    {
        if (!isInteractable)
        {
            Debug.Log(gameObject.name + " cannot interact because isInteractable is false");
            return;
        }

        if (hasInteracted && !isReusable)
        {
            Debug.Log(gameObject.name + " has already been interacted with");
            return;
        }

        hasInteracted = true;

        if (interactWidget != null)
            interactWidget.SetActive(false);

        timeElapsed = 0f;

        Debug.Log("Interact event invoked on: " + gameObject.name);

        InteractEvent?.Invoke();
    }
    public void ResetInteraction()
    {
        hasInteracted = false;
        timeElapsed = 0f;
    }
}