using System;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundType
{
    UI_BUTTON_HOVER,
    UI_BUTTON_PRESSED,
    INTERACT,
    JUMP
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [Header("Sound List")]
    public SoundList[] soundsList;

    [Header("Audio Source")]
    public AudioSource audioSource;

    public static SoundManager instance;

    private void Awake()
    {
        //set the global reference 
        instance = this;

        //get the audio source if it has not been assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public static void PlaySound(SoundType sound, float volume = 1f)
    {

        int soundIndex = (int)sound;
        SoundList soundList = instance.soundsList[soundIndex]; //<- THIS WAS TEH BOMBOCLART ISSUE MAN (INSTANCE NEVER ASSIGNED)
        AudioClip randomClip = soundList.sounds[UnityEngine.Random.Range(0, soundList.sounds.Length)];

        instance.audioSource.outputAudioMixerGroup = soundList.mixer;
        instance.audioSource.PlayOneShot(randomClip, volume * soundList.volume);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        string[] names = Enum.GetNames(typeof(SoundType));

        if (soundsList == null || soundsList.Length != names.Length)
        {
            Array.Resize(ref soundsList, names.Length);
        }

        for (int i = 0; i < soundsList.Length; i++)
        {
            soundsList[i].name = names[i];

            
            if (soundsList[i].volume <= 0f)
                soundsList[i].volume = 1f;
            
        }
    }
#endif
}

[Serializable]
public struct SoundList
{
    [HideInInspector] public string name;

    [Range(0, 1)] public float volume;

    public AudioMixerGroup mixer;

    public AudioClip[] sounds;
}