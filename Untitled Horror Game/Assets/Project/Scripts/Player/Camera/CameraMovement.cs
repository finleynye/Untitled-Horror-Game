using Mirror;
using Mirror.Examples.BilliardsPredicted;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class CameraMovement : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform camHolder;
    [SerializeField] private Transform playerBody;
    [SerializeField] private Camera playerCam;
    [SerializeField] private AudioListener audioListener;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private bool unlockCursor = true;

    [Header("FOV Settings")]
    [SerializeField] private float defaultFOV = 70;
    [SerializeField] private float sprintFOV = 90;
    [SerializeField] private float fovSpeed = 10;

    public Vector2 _lookInput;
    public float verticalRotation;
    public float horizontalRotation;

    private PlayerInput _playerInput;

    private void Awake()
    {

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerBody == null)
            playerBody = transform;

        if (camHolder == null)
        {
            Transform foundHolder = transform.Find("Player/CameraHolder");

            if (foundHolder != null)
                camHolder = foundHolder;
        }

        if (playerCam == null)
            playerCam = GetComponentInChildren<Camera>(true);

        if (audioListener == null)
            audioListener = GetComponentInChildren<AudioListener>(true);
    }

    public override void OnStartClient()
    {
        //every player starts with their camera disabled
        SetCameraState(false);
    }

    public override void OnStartAuthority()
    {
        if (!isOwned) return;

        //only this players own camera turns on
        SetCameraState(true);

        _playerInput = new PlayerInput();
        _playerInput.Enable();

        SetCameraState(true);
        SetCursorState();

    }
    void Update()
    {
        if (!isOwned) return;
        if (SceneManager.GetActiveScene().name == "Lobby") return;

        if (playerMovement == null) return;
        if (playerMovement.isPaused) return;

        HandleLook();
        HandleFOV();
    }

    private void HandleFOV()
    {
        var isMovingForward = playerMovement._moveInput.y > 0.1f;
        var targetFOV = playerMovement._isSprinting && isMovingForward ? sprintFOV : defaultFOV;

        playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
    }

    private void HandleLook()
    {
        if (_playerInput == null) return;
        if (camHolder == null) return;
        if (playerBody == null) return;

        // read look input directly from the input actions
        _lookInput = _playerInput.Player.Look.ReadValue<Vector2>();

        float mouseX = _lookInput.x * mouseSensitivity;
        float mouseY = _lookInput.y * mouseSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        //camera holder looks up and down
        camHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        //player body turns left and right
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void SetCameraState(bool state)
    {
        if (playerCam != null)
            playerCam.enabled = state;

        if (audioListener != null)
            audioListener.enabled = state;
    }

    public override void OnStopAuthority()
    {
        SetCameraState(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetCursorState()
    {
        if (unlockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
