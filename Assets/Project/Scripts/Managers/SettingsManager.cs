using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    [SerializeField] private AudioMixer audioMixer;
    public SettingsData PlayerSettings { get; private set; } = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
            Destroy(gameObject);
    }
    
    private void Start()
        => ApplyAndSave(PlayerSettings);

    public void ApplyAndSave(SettingsData newSettings)
    {
        PlayerSettings = newSettings;
        
        //logarithmic scale from -80dB to 0dB (i dont fucking know i saw it on youtube)
        audioMixer?.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(0.0001f, PlayerSettings.masterVolume)) * 20);
        audioMixer?.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, PlayerSettings.musicVolume)) * 20);
        audioMixer?.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, PlayerSettings.sfxVolume)) * 20);
        
        var resolutions = Screen.resolutions;
        if (PlayerSettings.resolutionIndex < resolutions.Length)
        {
            var resolution = resolutions[PlayerSettings.resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, PlayerSettings.isFullscreen);
        }
        
        QualitySettings.SetQualityLevel(PlayerSettings.graphicsIndex);
        
        //sensitivity/brightness will have to be read from elsewhere
        
        SaveSettings();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", PlayerSettings.masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", PlayerSettings.musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", PlayerSettings.sfxVolume);
        PlayerPrefs.SetInt("ResolutionIdx", PlayerSettings.resolutionIndex);
        PlayerPrefs.SetInt("GraphicsIdx", PlayerSettings.graphicsIndex);
        PlayerPrefs.SetInt("Fullscreen", PlayerSettings.isFullscreen ? 1 : 0);
        PlayerPrefs.SetFloat("Sensitivity", PlayerSettings.mouseSensitivity);
        PlayerPrefs.SetFloat("Brightness", PlayerSettings.brightness);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        var resolutionIndex = 0;
        var resolutions = Screen.resolutions;
        for (var i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                resolutionIndex = i;
                break;
            }
        }
        
        
        PlayerSettings.masterVolume = PlayerPrefs.GetFloat("MasterVol", defaultValue: 0.75f);
        PlayerSettings.musicVolume = PlayerPrefs.GetFloat("MusicVol", defaultValue: 0.75f);
        PlayerSettings.sfxVolume = PlayerPrefs.GetFloat("SFXVol", defaultValue: 0.75f);
        PlayerSettings.resolutionIndex = PlayerPrefs.GetInt("ResolutionIdx", defaultValue: resolutionIndex);
        PlayerSettings.graphicsIndex = PlayerPrefs.GetInt("GraphicsIdx", defaultValue: 1);
        PlayerSettings.isFullscreen = PlayerPrefs.GetInt("Fullscreen", defaultValue: 1) == 1;
        PlayerSettings.mouseSensitivity = PlayerPrefs.GetFloat("Sensitivity", defaultValue: 1f);
        PlayerSettings.brightness = PlayerPrefs.GetFloat("Brightness", defaultValue: 1f);
    }
}