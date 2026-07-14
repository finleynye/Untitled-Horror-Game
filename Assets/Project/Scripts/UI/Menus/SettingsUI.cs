using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Audio")] 
    [SerializeField] private Slider master;
    [SerializeField] private Slider music;
    [SerializeField] private Slider sfx;
    
    [Header("Video")]
    [SerializeField] private TMP_Dropdown resolution;
    [SerializeField] private TMP_Dropdown graphics;
    [SerializeField] private Toggle fullscreen;

    [Header("Gameplay")] 
    [SerializeField] private Slider sensitivity;
    [SerializeField] private Slider brightness;
        
    private Resolution[] _resolutions;

    private void Start()
    {
        InitResolutionDropdown();
        LoadSettingsToUI();
    }

    private void InitResolutionDropdown()
    {
        resolution.ClearOptions();
        _resolutions = Screen.resolutions;
        var options = _resolutions.Select(t => t.width + " x " + t.height).ToList();

        resolution.AddOptions(options);
    }

    private void LoadSettingsToUI()
    {
        var currentData = SettingsManager.Instance.PlayerSettings;

        master.value = currentData.masterVolume;
        music.value = currentData.musicVolume;
        sfx.value = currentData.sfxVolume;

        resolution.value = currentData.resolutionIndex;
        resolution.RefreshShownValue();

        graphics.value = currentData.graphicsIndex;
        graphics.RefreshShownValue();

        fullscreen.isOn = currentData.isFullscreen;

        sensitivity.value = currentData.mouseSensitivity;
        brightness.value = currentData.brightness;
    }
    
    public void OnClickApply()
    {
        var newData = new SettingsData
        {
            masterVolume = master.value,
            musicVolume = music.value,
            sfxVolume = sfx.value,
            resolutionIndex = resolution.value,
            graphicsIndex = graphics.value,
            isFullscreen = fullscreen.isOn,
            mouseSensitivity = sensitivity.value,
            brightness = brightness.value
        };

        SettingsManager.Instance.ApplyAndSave(newData);
    }
}
