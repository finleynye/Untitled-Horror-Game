using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class KillerNightVisionSwitcher : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Volume postProcessingVolume;

    [Header("Post Processing Profiles")]
    [SerializeField] private VolumeProfile normalProfile;
    [SerializeField] private VolumeProfile nightVisionProfile;

    [Header("State")]
    [SerializeField] private bool nightVisionActive;

    private PlayerInput _playerInput;

    public override void OnStartAuthority()
    {
        if (!isOwned)
            return;

        _playerInput = new PlayerInput();
        _playerInput.Player.KillerNightVision.performed += OnNightVisionPressed;

        _playerInput.Enable();

        //make sure the killer starts in normal vision
        SetNightVision(false);
    }
    private void OnNightVisionPressed(InputAction.CallbackContext context) => ToggleNightVision();
    private void ToggleNightVision() => SetNightVision(!nightVisionActive);
    private void SetNightVision(bool value)
    {
        nightVisionActive = value;

        if (postProcessingVolume == null)
            return;

        if (nightVisionActive)
        {
            if (nightVisionProfile != null)
                postProcessingVolume.profile = nightVisionProfile;

            return;
        }

        if (normalProfile != null)
            postProcessingVolume.profile = normalProfile;
    }
    
    public override void OnStopAuthority()
    {
        if (_playerInput != null)
        {
            //remove input when this player loses control
            _playerInput.Player.KillerNightVision.performed -= OnNightVisionPressed;

            _playerInput.Disable();
            _playerInput = null;
        }

        //reset back to normal when leaving the killer
        SetNightVision(false);
    }
}