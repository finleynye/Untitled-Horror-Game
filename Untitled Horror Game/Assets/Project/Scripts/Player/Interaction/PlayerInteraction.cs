using UnityEngine;
using Mirror;

public class PlayerInteraction : NetworkBehaviour
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
        if (!isOwned) return;
        if (isPaused || isFrozen) return;

        currentInteractable = FindClosestInteractable();

        if (currentInteractable == null)
            return;
        
        NetworkIdentity targetIdentity = currentInteractable.GetComponentInParent<NetworkIdentity>();

        if (targetIdentity == null) return;

        CmdTryInteract(targetIdentity);
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

    [Command]
    private void CmdTryInteract(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null) return;

        Interactable interactable = targetIdentity.GetComponentInChildren<Interactable>();

        if (interactable == null) return;

        float distance = Vector3.Distance(transform.position, interactable.transform.position);

        if (distance > interactionCheckRadius)
            return;
        
        interactable.ServerInteract();
    }
}