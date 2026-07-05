using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class BearTrap : NetworkBehaviour
{
    [SerializeField] private int requiredEscapePresses;
    [SerializeField] private float minPressCooldown; //set to 0 to allow key spamming
    /*[SerializeField] private Animator anim;*/
    [SerializeField] private Collider triggerZone;

    [SyncVar(hook = nameof(OnOccupiedChanged))] private bool _isOccupied;
    [SerializeField] private int _escapePresses;
    private PlayerMovement _trappedPlayer;
    private NetworkConnectionToClient _trappedConn;
    private uint _trappedPlayerNetID;
    
    private static readonly int Snap = Animator.StringToHash("Snap");

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (_isOccupied) return;
        
        var player = other.GetComponentInParent<PlayerController>();
        _trappedPlayer = player.GetComponentInChildren<PlayerMovement>();
        _trappedConn = player.connectionToClient;
        _trappedPlayerNetID = player.netId;
        _escapePresses = 0;
        _isOccupied = true;
        InvokeRepeating(nameof(TickDamage), 2, 1);
        
        triggerZone.enabled = false;
        _trappedPlayer?.TargetSetTrapped(_trappedConn, netIdentity);

        //RpcTrapSnapped();
    }

    private void OnOccupiedChanged(bool _, bool isOccupied)
        => triggerZone.enabled = !isOccupied;
    
    /*[ClientRpc]
    private void RpcTrapSnapped()
        => anim?.SetTrigger(Snap);*/

    [Command(requiresAuthority = false)]
    public void CmdAttemptEscape(NetworkConnectionToClient sender)
    {
        var caller = sender.identity?.GetComponent<PlayerController>();
        if (caller.netId != _trappedPlayerNetID) return;
        
        _escapePresses++;
        TargetEscapeProgress(sender, _escapePresses, requiredEscapePresses);

        if (_escapePresses >= requiredEscapePresses)
            ServerRelease();
    }
    
    [Server]
    private void TickDamage()
    {
        var health = _trappedPlayer.GetComponent<PlayerHealth>();
        health?.ApplyDamage(1);
    }
    
    [TargetRpc]
    private void TargetEscapeProgress(NetworkConnectionToClient conn, int current, int required)
    {
        //show progress through UI if we wanna (use the conn for ts)
        Debug.Log($"presses left: {required - current}");
    }
    
    [Server]
    private void ServerRelease()
    {
        CancelInvoke(nameof(TickDamage));
        _trappedPlayer.TargetSetFree(_trappedConn);
        //play sfx here
        NetworkServer.Destroy(gameObject);
    }
}
