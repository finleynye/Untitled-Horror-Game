using UnityEngine;

[CreateAssetMenu(menuName = "Scares/Paired Scare Definition")]
public class PairedScareDefinition : ScriptableObject
{
    [Header("Animations")]
    public string killerTrigger = "Kill";
    public string victimTrigger = "Killed";

    [Header("Alignment")]
    public Vector3 victimLocalPosition;
    public Vector3 victimLocalEuler;

    [Header("Timing")]
    public float duration = 3f;
    public float damageTime = 2.4f;

    [Header("Camera")]
    public bool followVictimHead = true;

    [Header("Audio")]
    public AudioClip victimAudio;
    public AudioClip killerAudio;
}