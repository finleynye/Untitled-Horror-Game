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

    private PlayerInput playerInput;

    private void Awake()
    {
        if (flashlight_Light == null)
            flashlight_Light = GetComponentInChildren<Light>(true);

        if (flashlightAudioSource == null)
            flashlightAudioSource = GetComponentInChildren<AudioSource>(true);
        
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

        CmdToggleFlashlight();
    }

    [Command]
    private void CmdToggleFlashlight()
    {
        flashlightActive = !flashlightActive;

        RpcPlayFlashlightSound();
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

    [ClientRpc]
    private void RpcPlayFlashlightSound()
    {
        PlayFlashlightSound();
    }

    private void PlayFlashlightSound()
    {
        if (flashlightAudioSource == null) return;
        if (flashlightToggleSound == null) return;
        if (!flashlightAudioSource.gameObject.activeInHierarchy) return;

        flashlightAudioSource.PlayOneShot(flashlightToggleSound, flashlightVolume);
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        playerInput?.Disable();
    }
}