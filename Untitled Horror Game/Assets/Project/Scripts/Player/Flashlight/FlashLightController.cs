using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
public class FlashLightController : NetworkBehaviour
{
    [Header("Flashlight References")]
    [SerializeField] Light flashlight_Light;
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
        if(flashlight_Light == null)
            flashlight_Light = GetComponentInChildren<Light>(true);

    }

    public override void OnStartAuthority()
    {
        if (!isOwned) return;

        playerInput = new PlayerInput();

        //toggle flashlight when the input button is pressed
        playerInput.Player.Flashlight.performed += ToggleFlashlight;

        playerInput.Enable();
    }
    private void ToggleFlashlight(InputAction.CallbackContext context)
    {
        if (!isOwned) return;

        CmdToggleFlashlight();
    }

    [Command]
    private void CmdToggleFlashlight()
    {
        flashlightActive = !flashlightActive;

        RpcPlayFlashlightSound();

        Debug.Log("Flashlight toggled on server: " + flashlightActive);
    }

    private void OnFlashlightStateChanged(bool oldValue, bool newValue)
    {
        SetFlashlightState(newValue);
    }

    private void SetFlashlightState(bool state)
    {
        flashlightActive = state;

        if (flashlight_Light != null)
            flashlight_Light.enabled = flashlightActive;
    }

    public override void OnStopAuthority()
    {
        if (playerInput != null)
        {
            playerInput.Player.Flashlight.performed -= ToggleFlashlight;
            playerInput.Disable();
        }
    }

    [ClientRpc]
    private void RpcPlayFlashlightSound()
    {
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
}
