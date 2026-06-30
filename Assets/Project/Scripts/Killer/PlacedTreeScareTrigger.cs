using UnityEngine;
using Mirror;

public class PlacedTreeScareTrigger : NetworkBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [SyncVar] private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (showDebugLogs)
            Debug.Log("TREE TRIGGER HIT: " + other.name);

        if (triggerOnce && hasTriggered)
        {
            if (showDebugLogs)
                Debug.Log("tree already triggered");

            return;
        }

        PlayerMovement playerMovement = FindPlayerMovementFromHit(other);

        if (playerMovement == null)
        {
            if (showDebugLogs)
                Debug.Log("no PlayerMovement found from hit object");

            return;
        }

        if (showDebugLogs)
            Debug.Log("found PlayerMovement: " + playerMovement.name);

        if (!playerMovement.isOwned)
        {
            if (showDebugLogs)
                Debug.Log("player is not owned by this client");

            return;
        }

        PlayerFallScareController fallScare = playerMovement.GetComponentInChildren<PlayerFallScareController>(true);

        if (fallScare == null)
            fallScare = playerMovement.GetComponentInParent<PlayerFallScareController>();

        if (fallScare == null)
        {
            if (showDebugLogs)
                Debug.Log("no PlayerTreeFallScare found on player");

            return;
        }

        if (showDebugLogs)
            Debug.Log("TREE SCARE SHOULD PLAY NOW");

        fallScare.PlayTreeFallScare(transform.position);

        if (triggerOnce)
            CmdSetTriggered();
    }

    private PlayerMovement FindPlayerMovementFromHit(Collider other)
    {
        //first attempt: useful if the collider is on or under the movement object
        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
            return playerMovement;

        //second attempt: your current setup hits the LocalPlayer root
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