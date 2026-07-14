using UnityEngine;

[System.Serializable]
public class SettingsData
{
    public float masterVolume = .75f;
    public float musicVolume = .75f;
    public float sfxVolume = .75f;

    public int resolutionIndex;
    public int graphicsIndex = 1; //0 = poor, 1 = medium, 2 = high?
    public bool isFullscreen = true;
    
    public float mouseSensitivity;
    public float brightness; //dont allow player to increase too much, maybe have note saying dimmer = better
}
