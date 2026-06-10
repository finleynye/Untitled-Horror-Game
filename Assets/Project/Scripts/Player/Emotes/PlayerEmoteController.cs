using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEmoteController : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    private PlayerInput _playerInput;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraMovement cameraMovement;

    [Header("Emote Settings")]
    [SerializeField]
    private string[] emoteTriggerNames = new string[3]
    {
        "Emote_One",
        "Emote_Two",
        "Emote_Three"
    };

    [Header("Cooldown")]
    [SerializeField] private float emoteCooldown = 3f;
    [SerializeField] private float nextEmoteTime;

    [Header("State")]
    [SerializeField] private bool isEmoting;

    [Header("Cancel Settings")]
    [SerializeField] private float movementCancelThreshold = 0.1f;
    [SerializeField] private float movementCancelDelay = 0.25f;

    [Header("Visibility")]
    [SerializeField] private LocalPlayerMeshVisibility localPlayerMeshVisibility;

    [Header("Emote Audio")]
    [SerializeField] private AudioSource emoteAudioSource;
    [SerializeField] private AudioClip[] emoteJingles;
    [SerializeField] private float emoteJingleVolume = 1f;

    private float emoteStartTime;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (cameraMovement == null)
            cameraMovement = GetComponent<CameraMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (localPlayerMeshVisibility == null)
            localPlayerMeshVisibility = GetComponentInChildren<LocalPlayerMeshVisibility>(true);
    }

    public override void OnStartAuthority()
    {
        if (!isOwned) return;

        _playerInput = new PlayerInput();
        _playerInput.Enable();
    }

    public override void OnStopAuthority()
        =>_playerInput?.Disable();
    

    private void Update()
    {
        if (!isOwned) return;

        HandleEmoteInput();
        HandleMovementCancel();
    }

    private void HandleEmoteInput()
    {
        if (_playerInput == null) return;

        if (_playerInput.Player.Emote.WasPressedThisFrame())
        {
            if (isEmoting)
            {
                StopEmote();
                return;
            }

            if (Time.time < nextEmoteTime)
                return;
            
            StartRandomEmote();
        }
    }

    private void HandleMovementCancel()
    {
        if (!isEmoting) return;
        if (playerMovement == null) return;

        //prevents the emote cancelling instantly after pressing B
        if (Time.time < emoteStartTime + movementCancelDelay) return;

        Vector2 moveInput = playerMovement._moveInput;

        if (moveInput.magnitude > movementCancelThreshold)
            StopEmote();
        
    }

    private void StartRandomEmote()
    {
        if (emoteTriggerNames == null || emoteTriggerNames.Length == 0) return;

        int randomIndex = Random.Range(0, emoteTriggerNames.Length);
        string selectedTrigger = emoteTriggerNames[randomIndex];

        isEmoting = true;
        emoteStartTime = Time.time;

        PlayRandomEmoteJingle();

        if (localPlayerMeshVisibility != null)
            localPlayerMeshVisibility.SetForcedLocalVisible(true);

        if (cameraMovement != null)
            cameraMovement.SetEmoteCamera(true);

        CmdPlayEmote(selectedTrigger);
    }
    private void StopEmote()
    {
        isEmoting = false;

        StopEmoteJingle();

        if (localPlayerMeshVisibility != null)
            localPlayerMeshVisibility.SetForcedLocalVisible(false);

        if (cameraMovement != null)
            cameraMovement.SetEmoteCamera(false);

        CmdStopEmote();
    }
    private void StopEmoteJingle()
    {
        if (emoteAudioSource == null) return;

        emoteAudioSource.Stop();
    }
    private void PlayRandomEmoteJingle()
    {
        if (emoteAudioSource == null) return;
        if (emoteJingles == null || emoteJingles.Length == 0) return;

        int randomIndex = Random.Range(0, emoteJingles.Length);
        AudioClip selectedJingle = emoteJingles[randomIndex];

        if (selectedJingle == null) return;

        emoteAudioSource.PlayOneShot(selectedJingle, emoteJingleVolume);
    }

    [Command]
    private void CmdPlayEmote(string triggerName)
    {
        RpcPlayEmote(triggerName);
    }

    [ClientRpc]
    private void RpcPlayEmote(string triggerName)
    {
        if (animator == null) return;

        animator.ResetTrigger("StopEmote");
        animator.SetTrigger(triggerName);
    }

    [Command]
    private void CmdStopEmote()
    {
        RpcStopEmote();
    }

    [ClientRpc]
    private void RpcStopEmote()
    {
        if (animator == null) return;

        animator.SetTrigger("StopEmote");
    }
}