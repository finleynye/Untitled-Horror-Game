using Mirror;
using UnityEngine;

public class LocalPlayerMeshVisibility : NetworkBehaviour
{
    [Header("Renderer To Hide Locally")]
    [SerializeField] private Renderer playerMeshRenderer;

    [Header("Settings")]
    [SerializeField] private bool hideForLocalPlayer = true;

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        if (!isOwned)
            return;

        SetMeshVisible(!hideForLocalPlayer);
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();

        SetMeshVisible(true);
    }

    private void SetMeshVisible(bool visible)
    {
        if (playerMeshRenderer == null)
            return;
        
        playerMeshRenderer.enabled = visible;
    }
}