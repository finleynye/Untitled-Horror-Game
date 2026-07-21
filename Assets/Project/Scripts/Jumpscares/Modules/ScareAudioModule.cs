using UnityEngine;

[System.Serializable]
public class ScareAudioModule
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip defaultScareSound;

    public void Initialise(AudioSource fallbackAudioSource) => audioSource = fallbackAudioSource;
    public void PlayDefault() => Play(defaultScareSound);
    public void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    public void Stop() => audioSource.Stop();
    
}