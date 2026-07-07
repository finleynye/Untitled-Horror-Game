using UnityEngine;
using Mirror;

public class PlacedTreeScareTrigger : NetworkBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;

    [SyncVar] private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered)
            return;

        PlayerMovement playerMovement = FindPlayerMovementFromHit(other);

        if (playerMovement == null)
        {
            return;
        }

        PlayerFallScareController fallScare = playerMovement.GetComponentInChildren<PlayerFallScareController>(true);

        if (fallScare == null)
            fallScare = playerMovement.GetComponentInParent<PlayerFallScareController>();

        if (fallScare == null)
        {
            return;
        }

        if (triggerOnce)
            SetTriggered();

        fallScare.PlayTreeFallScare(transform.position);
    }

    private void SetTriggered()
    {
        hasTriggered = true;

        if (NetworkClient.active && !NetworkServer.active)
            CmdSetTriggered();
    }

    private PlayerMovement FindPlayerMovementFromHit(Collider other)
    {
        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
            return playerMovement;

        PlayerController playerController = other.GetComponentInParent<PlayerController>();

        if (playerController == null)
            return null;

        GameObject currentRoleObject = playerController.GetCurrentRoleObject();

        if (currentRoleObject == null)
            return null;

        return currentRoleObject.GetComponentInChildren<PlayerMovement>(true);
    }

    [Command(requiresAuthority = false)]
    private void CmdSetTriggered()
    {
        hasTriggered = true;
    }
}