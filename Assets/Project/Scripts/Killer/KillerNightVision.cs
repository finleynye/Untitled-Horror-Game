using UnityEngine;
using UnityEngine.InputSystem;

//hello finnix i took the liberty of remaking the night vision <3
public class KillerNightVision : MonoBehaviour
{
    [SerializeField] private Camera killerCamera;
    [SerializeField] private bool nightVisionActive;
    private PlayerInput _playerInput;
    
    public void OnEnable()
    {
        _playerInput = new PlayerInput();
        _playerInput.Player.KillerNightVision.performed += OnNightVisionPressed;
        _playerInput.Enable();

        SetNightVision(false);
    }

    private void OnNightVisionPressed(InputAction.CallbackContext context)
        => ToggleNightVision();

    private void ToggleNightVision()
        => SetNightVision(!nightVisionActive);

    private void SetNightVision(bool value)
    {
        nightVisionActive = value;

        NightVisionRenderer.IsActive = value;
        NightVisionRenderer.ActiveCamera = value ? killerCamera : null;
    }

    public void OnDisable()
    {
        if (_playerInput != null)
        {
            _playerInput.Player.KillerNightVision.performed -= OnNightVisionPressed;
            _playerInput.Disable();
            _playerInput.Dispose();
            _playerInput = null;
        }
        
        if (NightVisionRenderer.ActiveCamera == killerCamera)
        {
            NightVisionRenderer.IsActive = false;
            NightVisionRenderer.ActiveCamera = null;
        }
    }
}