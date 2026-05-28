using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Interactable currentInteractable; //current interactable object
    [SerializeField] private float interactionCheckRadius = 3f; //interaction radius for player

    [SerializeField] private LayerMask interactableLayer; //layer of interactable objects

    [Header("Player States")]
    public bool isPaused;
    public bool isFrozen;


    public void Interact(InputAction.CallbackContext context)
    {
        if (isPaused) return;
        if (isFrozen) return;

        if (!context.performed)
            return;

        currentInteractable = FindClosestInteractable(); //finds the closest interactable

        if (currentInteractable == null)
        {
            Debug.Log("No interactable found.");
            return;
        }

        Debug.Log("Trying to interact with: " + currentInteractable.gameObject.name);

        currentInteractable.Interact(); //interacts with the closest interactable
    }

    private Interactable FindClosestInteractable()
    {
        //checks within a sphere at the players position and check radius
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionCheckRadius, interactableLayer);

        Interactable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        //loop through all collider hits within the radius and object layer
        foreach (Collider hit in hits)
        {
            Interactable interactable = hit.GetComponent<Interactable>(); //get the objects interactable component

            if (interactable == null)
                interactable = hit.GetComponentInParent<Interactable>();

            if (interactable == null)
                continue;

            float distance = Vector3.Distance(transform.position, interactable.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
    }
}