using Mirror;
using Steamworks;
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

    [Header("Crouch Camera")]
    [SerializeField] private float standingCameraY = 0.8f;
    [SerializeField] private float crouchingCameraY = 0.25f;
    [SerializeField] private float crouchCameraSpeed = 12f;

    [Header("Emote Camera")]
    [SerializeField] private Transform emoteCameraTransform;
    [SerializeField] private float emoteCameraMoveSpeed = 8f;
    [SerializeField] private float emoteCameraRotateSpeed = 8f;

    private bool isUsingEmoteCamera;
    private Vector3 normalCameraLocalPosition;
    private Quaternion normalCameraLocalRotation;

    public Vector2 _lookInput;
    public float verticalRotation;
    public float horizontalRotation;

    public Transform PlayerCameraTransform => playerCam != null ? playerCam.transform : null;

    private PlayerInput _playerInput;

    [Header("Post Processing")]
    [SerializeField] private Volume playerVolume;
    private Vignette vignette;
    private ColorAdjustments _colourAdjustment;

    [Header("Render Texture")]
    [SerializeField] private RenderTexture cameraRenderTexture;
    [SerializeField] private bool useRenderTexture = true;

    [Header("Exhausted Vignette Shape")]
    [SerializeField] private float exhaustedVignetteMin = 0.35f; //minimum for exhausted vignette amount
    [SerializeField] private float exhaustedVignetteMax = 0.65f; //maximum for exhausted vignette amount
    [SerializeField] private float exhaustedVignetteSpeed = 1f; //speed the exhuasted states lerps between min and max
    [SerializeField] private float vignetteLerpSpeed = 2.5f; //how quickly the the effect appears on screen once exhuasted

    [Header("Exhausted & Normal Vignette Colour")]
    [SerializeField] private Color normalVignetteColor = Color.black;
    [SerializeField] private Color exhaustedVignetteColor = new Color(0.45f, 0f, 0f); //dark red

    [Header("Normal Vignette Shape")]
    [SerializeField] private float normalVignetteIntensity = 0.2f;
    [SerializeField] private float normalVignetteSmoothness = 0.6f;
    [SerializeField] private float exhaustedSmoothnessMin = 0.35f;
    [SerializeField] private float exhaustedSmoothnessMax = 0.75f;

    private void Awake()
    {
        if (playerStamina == null)
            playerStamina = GetComponent<PlayerStamina>();

        if (playerBody == null)
            playerBody = transform.root;

        if (camHolder == null)
        {
            var foundHolder = transform.Find("Player/CameraHolder");

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
            //make a unique copy so this player camera does not edit the shared volume asset
            playerVolume.profile = Instantiate(playerVolume.profile);
            playerVolume.profile.TryGet(out vignette);
            playerVolume.profile.TryGet(out _colourAdjustment);

            if (vignette != null)
            {
                //default camera vignette state
                vignette.color.value = normalVignetteColor;
                vignette.intensity.value = normalVignetteIntensity;
                vignette.smoothness.value = normalVignetteSmoothness;
            }
        }

        if (camHolder != null)
        {
            normalCameraLocalPosition = camHolder.localPosition;
            normalCameraLocalRotation = camHolder.localRotation;
        }
    }

    private void SetupInput()
    {
        if (_playerInput != null)
            return;

        _playerInput = new PlayerInput();
        _playerInput.Enable();
    }
    
    public void SetEmoteCamera(bool value)
    {
        isUsingEmoteCamera = value;
        if (!isUsingEmoteCamera && camHolder != null)
        {
            camHolder.localPosition = normalCameraLocalPosition;
            camHolder.localRotation = normalCameraLocalRotation;
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
        
        SetCameraState(true);
        SetCameraRenderTexture();

        SetupInput();
        SetCursorState();
    }
    
    private void Update()
    {
        if (!isOwned) return;

        ApplyBrightnessSetting();
        
        var isLobby = SceneManager.GetActiveScene().name == "Lobby";

        if (isLobby)
        {
            if (isUsingEmoteCamera)
                HandleEmoteCamera();
     
            return;
        }

        if (playerMovement == null || playerStamina == null) 
            return;

        if (playerMovement.isPaused)
        {
            ExhaustedVignette();
            return;
        }

        HandleFOV();
        ExhaustedVignette();

        if (isUsingEmoteCamera)
        {
            HandleEmoteCamera();
            return;
        }

        HandleCrouchCamera();
        HandleLook();
    }

    private void HandleFOV()
    {
        if (playerCam == null || playerMovement == null) 
            return;

        var isMovingForward = playerMovement._moveInput.y > 0.1f;
        var targetFOV = playerMovement._isSprinting && isMovingForward ? sprintFOV : defaultFOV;

        playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
    }

    private void ExhaustedVignette()
    {
        if (vignette == null) return;
        if (playerStamina == null) return;

        var targetIntensity = normalVignetteIntensity;
        var targetSmoothness = normalVignetteSmoothness;
        var targetColor = normalVignetteColor;

        if (playerStamina.IsStaminaEmpty)
        {
            //creates a breathing pulse from -1 to 1
            var pulse = Mathf.Sin(Time.time * exhaustedVignetteSpeed);

            //converts the pulse from -1 to 1 into 0 to 1
            var normalisedPulse = (pulse + 1f) * 0.5f;

            //grow and shrink the vignette darkness
            targetIntensity = Mathf.Lerp(exhaustedVignetteMin, exhaustedVignetteMax, normalisedPulse);
            targetSmoothness = Mathf.Lerp(exhaustedSmoothnessMin, exhaustedSmoothnessMax, normalisedPulse);

            //change colour when exhausted
            targetColor = exhaustedVignetteColor;
        }

        targetIntensity = Mathf.Clamp01(targetIntensity);
        targetSmoothness = Mathf.Clamp01(targetSmoothness);

        //smoothly move the values towards their targets
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, vignetteLerpSpeed * Time.deltaTime);

        vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, targetSmoothness, vignetteLerpSpeed * Time.deltaTime);

        vignette.color.value = Color.Lerp(vignette.color.value, targetColor, vignetteLerpSpeed * Time.deltaTime);

        //safety clamps
        vignette.intensity.value = Mathf.Clamp01(vignette.intensity.value);
        vignette.smoothness.value = Mathf.Clamp01(vignette.smoothness.value);
    }

    private void ApplyBrightnessSetting()
    {
        var baseline = SettingsManager.Instance.PlayerSettings.brightness;
        _colourAdjustment.postExposure.value = baseline - 1f;
        Debug.Log("b" + baseline);
    }

    private void HandleLook()
    {
        if (playerMovement == null || playerStamina == null) return;
        if (playerMovement.isPaused) return;

        if (_playerInput == null)
        {
            SetupInput();

            if (_playerInput == null)
                return;
        }

        if (camHolder == null)
            return;

        if (playerBody == null)
            return;

        //read look input directly from the input actions
        _lookInput = _playerInput.Player.Look.ReadValue<Vector2>();

        var currentSensitivity = mouseSensitivity * SettingsManager.Instance.PlayerSettings.mouseSensitivity;
        Debug.Log("cs" + currentSensitivity);
        var mouseX = _lookInput.x * currentSensitivity;
        var mouseY = _lookInput.y * currentSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -65f, 80f);

        horizontalRotation += mouseX;

        //camera looks up and down
        camHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        //player root/body turns left and right
        playerBody.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
    }

    private void HandleCrouchCamera()
    {
        if (camHolder == null || playerMovement == null) return;

        var targetY = playerMovement.IsCrouching ? crouchingCameraY : standingCameraY;
        var currentPosition = camHolder.localPosition;

        currentPosition.y = Mathf.Lerp(currentPosition.y, targetY, crouchCameraSpeed * Time.deltaTime);

        camHolder.localPosition = currentPosition;
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
        if (playerCam != null)
            playerCam.targetTexture = null;

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

    private void SetCameraRenderTexture()
    {
        if (playerCam == null)
            return;

        if (!useRenderTexture)
        {
            playerCam.targetTexture = null;
            return;
        }

        if (cameraRenderTexture == null) return;
       
        playerCam.targetTexture = cameraRenderTexture;
    }

    private void HandleEmoteCamera()
    {
        if (camHolder == null || emoteCameraTransform == null) return;

        camHolder.position = Vector3.Lerp(camHolder.position, emoteCameraTransform.position, emoteCameraMoveSpeed * Time.deltaTime);
        camHolder.rotation = Quaternion.Lerp(camHolder.rotation, emoteCameraTransform.rotation, emoteCameraRotateSpeed * Time.deltaTime);
    }
}
