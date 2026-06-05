using System;
using UnityEngine;
using UnityEngine.Audio;

public enum SoundType
{
    UI_BUTTON_HOVER,
    UI_BUTTON_PRESSED,
    INTERACT,
    JUMP,
    LAND,
    FOOTSTEP,
    STAMINA_EXHAUST
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
        //stop duplicate sound managers from existing
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        //set the global reference
        instance = this;

        //keep this object alive between scenes
        DontDestroyOnLoad(gameObject);

        //get the audio source if it has not been assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
    }

    public static void PlaySound(SoundType sound, float volume = 1f)
    {

        int soundIndex = (int)sound;
        SoundList soundList = instance.soundsList[soundIndex]; //<- THIS WAS TEH BOMBOCLART ISSUE MAN (INSTANCE NEVER ASSIGNED)
        
        AudioClip randomClip = soundList.sounds[UnityEngine.Random.Range(0, soundList.sounds.Length)];
        float randomPitch = UnityEngine.Random.Range(soundList.minPitch, soundList.maxPitch);

        instance.audioSource.outputAudioMixerGroup = soundList.mixer;
        instance.audioSource.pitch = randomPitch;
        instance.audioSource.PlayOneShot(randomClip, volume * soundList.volume);

        //reset so the next sound starts from normal pitch unless changed again
        instance.audioSource.pitch = 1f;
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


            if (soundsList[i].minPitch <= 0f)
                soundsList[i].minPitch = 0.95f;


            if (soundsList[i].maxPitch <= 0f)
                soundsList[i].maxPitch = 1.05f;


            if (soundsList[i].maxPitch < soundsList[i].minPitch)
                soundsList[i].maxPitch = soundsList[i].minPitch;

        }
    }
#endif
}

[Serializable]
public struct SoundList
{
    [HideInInspector] public string name;

    [Range(0, 1)] public float volume;

    [Header("Pitch Randomisation")]
    [Range(0.5f, 2f)] public float minPitch;
    [Range(0.5f, 2f)] public float maxPitch;

    public AudioMixerGroup mixer;

    public AudioClip[] sounds;
}