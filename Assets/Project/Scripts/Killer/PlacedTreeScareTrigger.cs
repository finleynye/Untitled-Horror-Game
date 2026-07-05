using UnityEngine;
using Mirror;

public class PlacedTreeScareTrigger : NetworkBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;

    [SyncVar] private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkServer.active)
            return;

        if (triggerOnce && hasTriggered)
            return;

        PlayerMovement playerMovement = FindPlayerMovementFromHit(other);

        if (playerMovement == null)
            return;

        ServerPlayFallScare(playerMovement);
    }

    [Server]
    private void ServerPlayFallScare(PlayerMovement playerMovement)
    {
        if (triggerOnce && hasTriggered)
            return;

        PlayerController playerController = FindPlayerController(playerMovement);

        if (playerController == null || playerController.role == PlayerRole.Killer)
            return;

<<<<<<< HEAD
        playerMovement = GetCurrentRoleMovement(playerController) ?? playerMovement;

        PlayerFallScareController fallScare = FindFallScare(playerMovement, playerController);
=======
        PlayerFallScareController fallScare = playerMovement.GetComponentInChildren<PlayerFallScareController>(true);

        if (fallScare == null)
            fallScare = playerMovement.GetComponentInParent<PlayerFallScareController>();
>>>>>>> parent of 4897da4 (Jump Scare Client Side Fix)

        if (fallScare == null)
            return;

        NetworkConnectionToClient conn = playerController.connectionToClient;

        if (conn == null && playerController.netIdentity != null)
            conn = playerController.netIdentity.connectionToClient;

        if (conn == null)
            return;

        fallScare.TargetPlayTreeFallScare(conn, transform.position);
        fallScare.RpcPlayTreeFallScareForObservers(transform.position);

        if (triggerOnce)
            hasTriggered = true;
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestFallScare(NetworkIdentity playerIdentity)
    {
        if (playerIdentity == null)
            return;

        PlayerController playerController = playerIdentity.GetComponent<PlayerController>();

        if (playerController == null)
            playerController = playerIdentity.GetComponentInChildren<PlayerController>(true);

        PlayerMovement playerMovement = GetCurrentRoleMovement(playerController);

        if (playerMovement == null)
            playerMovement = playerIdentity.GetComponentInChildren<PlayerMovement>();

        if (playerMovement == null)
            playerMovement = playerIdentity.GetComponentInChildren<PlayerMovement>(true);

        ServerPlayFallScare(playerMovement);
    }
    private PlayerFallScareController FindFallScare(PlayerMovement playerMovement, PlayerController playerController)
    {
        PlayerFallScareController fallScare = null;

<<<<<<< HEAD
        if (playerMovement != null)
        {
            fallScare = playerMovement.GetComponentInChildren<PlayerFallScareController>(true);

            if (fallScare == null)
                fallScare = playerMovement.GetComponentInParent<PlayerFallScareController>();
        }

        if (fallScare == null && playerController != null)
            fallScare = playerController.GetComponentInChildren<PlayerFallScareController>(true);

        return fallScare;
    }

    private bool IsKiller(PlayerMovement playerMovement)
    {
        PlayerController playerController = FindPlayerController(playerMovement);
        return playerController != null && playerController.role == PlayerRole.Killer;
    }

    private PlayerController FindPlayerController(PlayerMovement playerMovement)
=======
    private PlayerMovement FindPlayerMovementFromHit(Collider other)
>>>>>>> parent of 4897da4 (Jump Scare Client Side Fix)
    {
        if (playerMovement == null)
            return null;

        PlayerController playerController = playerMovement.GetComponentInParent<PlayerController>();

        if (playerController != null)
            return playerController;

        if (playerMovement.netIdentity != null)
        {
            playerController = playerMovement.netIdentity.GetComponentInParent<PlayerController>();

            if (playerController != null)
                return playerController;

            playerController = playerMovement.netIdentity.GetComponentInChildren<PlayerController>(true);

            if (playerController != null)
                return playerController;
        }

        NetworkConnectionToClient conn = playerMovement.connectionToClient;

        if (conn == null && playerMovement.netIdentity != null)
            conn = playerMovement.netIdentity.connectionToClient;

        if (conn != null && conn.identity != null)
            return conn.identity.GetComponent<PlayerController>();

        return null;
    }

    private NetworkIdentity GetPlayerIdentity(PlayerMovement playerMovement)
    {
        PlayerController playerController = FindPlayerController(playerMovement);

        if (playerController != null)
            return playerController.netIdentity;

        return playerMovement != null ? playerMovement.netIdentity : null;
    }

    private PlayerMovement GetCurrentRoleMovement(PlayerController playerController)
    {
        if (playerController == null)
            return null;

        GameObject currentRoleObject = playerController.GetCurrentRoleObject();

        if (currentRoleObject == null)
            return null;

        return currentRoleObject.GetComponentInChildren<PlayerMovement>(true);
    }

    private PlayerMovement FindPlayerMovementFromHit(Collider other)
    {
        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
            return playerMovement;

        PlayerController playerController = other.GetComponentInParent<PlayerController>();

        return GetCurrentRoleMovement(playerController);
    }
}