using Mirror;
using UnityEngine;

public class LocalPlayerMeshVisibility : NetworkBehaviour
{
    [Header("Renderers To Hide Locally")]
    [SerializeField] private SkinnedMeshRenderer[] playerMeshRenderers;

    [Header("Settings")]
    [SerializeField] private bool hideForLocalPlayer = true;

    private bool isForcedVisible;

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        RefreshVisibility();
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();

        SetMeshVisible(true);
    }

    public void SetForcedLocalVisible(bool visible)
    {
        if (!isOwned) return;

        isForcedVisible = visible;

        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (!isOwned) return;

        //emotes need body visible but normal first person hides it
        if (isForcedVisible)
            SetMeshVisible(true);
        else
            ApplyDefaultVisibility();
    }

    private void ApplyDefaultVisibility()
    {
        SetMeshVisible(!hideForLocalPlayer);
    }

    private void SetMeshVisible(bool visible)
    {
        if (playerMeshRenderers == null) return;

        foreach (SkinnedMeshRenderer meshRenderer in playerMeshRenderers)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = visible;
        }
    }
}
