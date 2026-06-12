using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class FlashLightController : NetworkBehaviour
{
    [Header("Flashlight References")]
    [SerializeField] private Light flashlight_Light;
    [SerializeField] private AudioSource flashlightAudioSource;

    [Header("Flashlight Audio")]
    [SerializeField] private AudioClip flashlightToggleSound;
    [SerializeField] private float flashlightVolume = 1f;

    [Header("Flashlight State")]
    [SyncVar(hook = nameof(OnFlashlightStateChanged))]
    private bool flashlightActive = false;

    [Header("Pause Check")]
    [SerializeField] private PlayerMovement playerMovement;
    private bool isPaused = false;

    private PlayerInput playerInput;

    private void Awake()
    {
        if (flashlight_Light == null)
            flashlight_Light = GetComponentInChildren<Light>(true);

        if (flashlightAudioSource == null)
            flashlightAudioSource = GetComponentInChildren<AudioSource>(true);

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        //make sure the light visually matches the synced bool when this player spawns
        SetFlashlightState(flashlightActive);
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        if (!isOwned)
            return;

        playerInput = new PlayerInput();

        //toggle flashlight when the input button is pressed
        playerInput.Player.Flashlight.performed += ToggleFlashlight;

        playerInput.Enable();
    }

    private void ToggleFlashlight(InputAction.CallbackContext context)
    {

        if (!isOwned)
            return;

        if (playerMovement != null && playerMovement.isPaused)
            return;

        if (isPaused)
            return;

        bool newState = !flashlightActive;

        SetFlashlightState(newState);
        PlayFlashlightSound();

        CmdSetFlashlight(newState);
    }

    [Command]
    private void CmdSetFlashlight(bool newState)
    {
        flashlightActive = newState;

        RpcPlayFlashlightSound(newState);
    }

    private void OnFlashlightStateChanged(bool oldValue, bool newValue)
    {
        SetFlashlightState(newValue);
    }

    private void SetFlashlightState(bool state)
    {
        if (flashlight_Light == null)
            return;

        flashlight_Light.enabled = state;
    }

    [ClientRpc(includeOwner = false)]
    private void RpcPlayFlashlightSound(bool newState)
    {
        SetFlashlightState(newState);
        PlayFlashlightSound();
    }

    private void PlayFlashlightSound()
    {
        if (flashlightAudioSource == null)
            return;

        if (flashlightToggleSound == null)
            return;

        flashlightAudioSource.PlayOneShot(flashlightToggleSound, flashlightVolume);
    }
    public void SetPaused(bool value)
    {
        isPaused = value;
    }
    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        playerInput?.Disable();
    }
}
