using Mirror;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
public class CameraMovement : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerStamina playerStamina;
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

    [Header("Post Processing")]
    [SerializeField] private Volume playerVolume;
    private Vignette vignette;

    [Header("Exhausted Vignette")]
    [SerializeField] private float normalVignetteIntensity = 0.2f;
    [SerializeField] private float exhaustedVignetteMin = 0.35f; //minimum for exhausted vignette amount
    [SerializeField] private float exhaustedVignetteMax = 0.65f; //maximum for exhausted vignette amount
    [SerializeField] private float exhaustedVignetteSpeed = 1f; //speed the exhuasted states lerps between min and max
    [SerializeField] private float vignetteLerpSpeed = 2.5f; //how quickly the the effect appears on screen once exhuasted

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerStamina == null)
            playerStamina = GetComponent<PlayerStamina>();

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

        if (playerVolume == null)
            playerVolume = GetComponentInChildren<Volume>(true);

        if (playerVolume != null && playerVolume.profile != null)
        {
            playerVolume.profile = Instantiate(playerVolume.profile);
            playerVolume.profile.TryGet(out vignette);
        }
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
        if (playerStamina == null) return;

        if (playerMovement.isPaused)
        {
            ExhaustedVignette();
            return;
        }

        HandleLook();
        HandleFOV();
        ExhaustedVignette();
    }

    private void HandleFOV()
    {
        var isMovingForward = playerMovement._moveInput.y > 0.1f;
        var targetFOV = playerMovement._isSprinting && isMovingForward ? sprintFOV : defaultFOV;

        playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
    }

    private void ExhaustedVignette()
    {
        if (vignette == null) return;
        if (playerStamina == null) return;

        float targetIntensity = normalVignetteIntensity;

        if (playerStamina.IsStaminaEmpty)
        {
            //creates a slow breathing pulse from min to max
            float pulse = Mathf.Sin(Time.time * exhaustedVignetteSpeed);

            //converts the pulse from -1 to 1 into 0 to 1
            float normalisedPulse = (pulse + 1f) * 0.5f;

            targetIntensity = Mathf.Lerp(exhaustedVignetteMin, exhaustedVignetteMax, normalisedPulse);
        }

        targetIntensity = Mathf.Clamp01(targetIntensity); //clamps between 0,1 just incase lerp overshoots values

        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, vignetteLerpSpeed * Time.deltaTime);

        //clamps the final vignette amount, just in case lerp overshootts
        vignette.intensity.value = Mathf.Clamp01(vignette.intensity.value);
    }

    private void HandleLook()
    {
        if (playerMovement == null) return;
        if (playerStamina == null) return;
        if (playerMovement.isPaused) return;

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
        
        _playerInput?.Disable();
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
