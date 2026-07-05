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
            return;

        if (NetworkServer.active)
        {
            ServerPlayFallScare(playerMovement);
            return;
        }

        if (playerMovement.isOwned)
            CmdRequestFallScare(playerMovement.netIdentity);
    }

    [Server]
    private void ServerPlayFallScare(PlayerMovement playerMovement)
    {
        if (triggerOnce && hasTriggered)
            return;

        if (playerMovement == null)
            return;

        if (IsKiller(playerMovement))
            return;

        PlayerFallScareController fallScare = playerMovement.GetComponentInChildren<PlayerFallScareController>(true);

        if (fallScare == null)
            fallScare = playerMovement.GetComponentInParent<PlayerFallScareController>();

        if (fallScare == null)
            return;

        NetworkConnectionToClient conn = playerMovement.connectionToClient;

        if (conn == null && playerMovement.netIdentity != null)
            conn = playerMovement.netIdentity.connectionToClient;

        if (conn == null)
            return;

        fallScare.RpcPlayTreeFallScareForObservers(transform.position);
        fallScare.TargetPlayTreeFallScare(conn, transform.position);

        if (triggerOnce)
            hasTriggered = true;
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestFallScare(NetworkIdentity playerIdentity)
    {
        if (playerIdentity == null)
            return;

        PlayerMovement playerMovement = playerIdentity.GetComponentInChildren<PlayerMovement>(true);

        if (playerMovement == null)
            playerMovement = playerIdentity.GetComponentInParent<PlayerMovement>();

        ServerPlayFallScare(playerMovement);
    }


    private bool IsKiller(PlayerMovement playerMovement)
    {
        PlayerController playerController = playerMovement.GetComponentInParent<PlayerController>();
        return playerController != null && playerController.role == PlayerRole.Killer;
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

}
