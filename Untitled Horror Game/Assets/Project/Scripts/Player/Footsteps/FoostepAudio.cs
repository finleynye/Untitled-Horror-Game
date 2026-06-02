using Mirror;
using UnityEngine;

public class FoostepAudio : NetworkBehaviour
{
    private void PlayFootstep()
    {
        SoundManager.PlaySound(SoundType.FOOTSTEP, 1f);
        CmdPlayFootstep();
    }

    [Command]
    private void CmdPlayFootstep()
    {
        RpcPlayFootstep();
    }

    [ClientRpc(includeOwner = false)]
    private void RpcPlayFootstep()
    {
        SoundManager.PlaySound(SoundType.FOOTSTEP, 1f);
    }
}
