using UnityEngine;

public class FoostepAudio : MonoBehaviour
{
    public void PlayFootstep()
    {
        SoundManager.PlaySound(SoundType.FOOTSTEP, 1f);
    }
}
