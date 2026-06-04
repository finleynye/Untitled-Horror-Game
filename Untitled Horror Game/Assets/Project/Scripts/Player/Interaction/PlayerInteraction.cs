using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Interactable currentInteractable;
    [SerializeField] private float interactionCheckRadius = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Player States")]
    public bool isPaused;
    public bool isFrozen;

    public void TryInteract()
    {
        if (isPaused || isFrozen) return;

        currentInteractable = FindClosestInteractable();

        if (currentInteractable == null)
        {
            Debug.Log("No interactable found.");
            return;
        }

        currentInteractable.Interact();
    }

    private Interactable FindClosestInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionCheckRadius, interactableLayer, QueryTriggerInteraction.Collide);

        Interactable closestInteractable = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Interactable interactable = hit.GetComponent<Interactable>();

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