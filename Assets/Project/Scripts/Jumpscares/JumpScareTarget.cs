using Mirror;
using UnityEngine;

public class JumpscareTarget : NetworkBehaviour
{
    [SerializeField] private NetworkIdentity networkIdentity;
    [SerializeField] private Transform facingTransform;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private PlayerScareController scareController;
    [SerializeField] private Ragdoll ragdoll;

    public Transform FacingTransform => facingTransform != null ? facingTransform : transform;

    public Transform PlayerRoot => playerRoot != null ? playerRoot : transform;

    public PlayerScareController ScareController
    {
        get
        {
            if (scareController == null)
                scareController = GetComponentInChildren<PlayerScareController>();

            return scareController;
        }
    }

    public NetworkIdentity NetworkIdentity
    {
        get
        {
            if (networkIdentity == null)
                networkIdentity = GetComponentInParent<NetworkIdentity>();

            return networkIdentity;
        }
    }

    public uint NetId => NetworkIdentity != null ? NetworkIdentity.netId : 0;

    public bool IsLocallyOwned => NetworkIdentity != null && NetworkIdentity.isOwned;

    public void ActivateJumpscareRagdoll()
    {
        if (ragdoll != null)
            ragdoll.ActivateRagdoll();
    }
    
}