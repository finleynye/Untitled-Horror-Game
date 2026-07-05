using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

//hello finnix i took the liberty of remaking the night vision <3
public class KillerNightVision : NetworkBehaviour
{
    [SerializeField] private Camera killerCamera;
    [SerializeField] private Shader nightVisionShader;
    [SerializeField] private bool nightVisionActive;

    [Header("Look")]
    [SerializeField] private Color tintColor = new Color(0.21f, 0.85f, 0.29f, 1f);
    [SerializeField] private float scanlineIntensity = 0.25f;
    [SerializeField] private float scanlineSpeed = 0.98f;
    [SerializeField] private float noiseIntensity = 0.02f;
    [SerializeField] private float vignetteRadius = 1.12f;
    [SerializeField] private float vignetteSoftness = 2.52f;
    [SerializeField] private float lensGap = 0.18f;
    [SerializeField] private float gain = 25f;
    [SerializeField] private float gammaCurve = 0.7f;

    private static readonly int SourceTexId = Shader.PropertyToID("_BlitTexture");
    private static readonly int SourceTexelSizeId = Shader.PropertyToID("_BlitTexture_TexelSize");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int ScanlineIntensityId = Shader.PropertyToID("_ScanlineIntensity");
    private static readonly int ScanlineSpeedId = Shader.PropertyToID("_ScanlineSpeed");
    private static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int VignetteRadiusId = Shader.PropertyToID("_VignetteRadius");
    private static readonly int VignetteSoftnessId = Shader.PropertyToID("_VignetteSoftness");
    private static readonly int LensGapId = Shader.PropertyToID("_LensGap");
    private static readonly int Time1Id = Shader.PropertyToID("_Time1");
    private static readonly int GainId = Shader.PropertyToID("_Gain");
    private static readonly int GammaCurveId = Shader.PropertyToID("_GammaCurve");

    private PlayerInput _playerInput;
    private PlayerController _playerController;
    private Material _nightVisionMaterial;
    private bool _subscribedToRendering;

    private bool HasLocalControl => isOwned || GetPlayerController()?.isOwned == true;

    public override void OnStartAuthority()
    {
        TryEnableInput();
    }

    private void OnEnable()
    {
        SubscribeToRendering();
        TryEnableInput();
    }

    private void Update()
    {
        if (_playerInput == null)
            TryEnableInput();
    }

    private void TryEnableInput()
    {
        if (!HasLocalControl)
            return;

        EnsureCamera();

        if (_playerInput != null)
            return;

        _playerInput = new PlayerInput();
        _playerInput.Player.KillerNightVision.performed += OnNightVisionPressed;
        _playerInput.Enable();

        SetNightVision(false);
    }

    private PlayerController GetPlayerController()
    {
        if (_playerController != null)
            return _playerController;

        return _playerController = GetComponentInParent<PlayerController>();
    }

    private void EnsureCamera()
    {
        if (killerCamera != null)
            return;

        killerCamera = GetComponentInChildren<Camera>(true);
    }

    private void OnNightVisionPressed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        ToggleNightVision();
    }

    private void ToggleNightVision()
        => SetNightVision(!nightVisionActive);

    private void SetNightVision(bool value)
    {
        if (!HasLocalControl)
            return;

        EnsureCamera();

        nightVisionActive = value && killerCamera != null && EnsureMaterial();

        bool useRendererFeatureFallback = nightVisionActive && killerCamera.targetTexture == null;
        NightVisionRenderer.IsActive = useRendererFeatureFallback;
        NightVisionRenderer.ActiveCamera = useRendererFeatureFallback ? killerCamera : null;
    }

    private bool EnsureMaterial()
    {
        if (_nightVisionMaterial != null)
            return true;

        if (nightVisionShader == null)
            nightVisionShader = Shader.Find("Hidden/NightVision");

        if (nightVisionShader == null)
            return false;

        _nightVisionMaterial = new Material(nightVisionShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        return true;
    }

    private void SubscribeToRendering()
    {
        if (_subscribedToRendering)
            return;

        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        _subscribedToRendering = true;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera renderedCamera)
    {
        if (!nightVisionActive)
            return;

        if (renderedCamera != killerCamera)
            return;

        RenderTexture source = killerCamera != null ? killerCamera.targetTexture : null;

        if (source == null)
            return;

        if (!EnsureMaterial())
            return;

        ApplyMaterialSettings(source);

        RenderTextureDescriptor descriptor = source.descriptor;
        descriptor.depthBufferBits = 0;
        RenderTexture temporary = RenderTexture.GetTemporary(descriptor);

        Graphics.Blit(source, temporary);
        _nightVisionMaterial.SetTexture(SourceTexId, temporary);
        _nightVisionMaterial.SetVector(SourceTexelSizeId, new Vector4(1f / temporary.width, 1f / temporary.height, temporary.width, temporary.height));
        Graphics.Blit(temporary, source, _nightVisionMaterial, 0);

        RenderTexture.ReleaseTemporary(temporary);
    }

    private void ApplyMaterialSettings(Texture source)
    {
        Color safeTintColor = tintColor.maxColorComponent > 0.01f ? tintColor : new Color(0.21f, 0.85f, 0.29f, 1f);

        _nightVisionMaterial.SetTexture(SourceTexId, source);
        _nightVisionMaterial.SetVector(SourceTexelSizeId, new Vector4(1f / source.width, 1f / source.height, source.width, source.height));
        _nightVisionMaterial.SetColor(TintColorId, safeTintColor);
        _nightVisionMaterial.SetFloat(ScanlineIntensityId, Mathf.Clamp01(scanlineIntensity > 0f ? scanlineIntensity : 0.25f));
        _nightVisionMaterial.SetFloat(ScanlineSpeedId, scanlineSpeed > 0f ? scanlineSpeed : 0.98f);
        _nightVisionMaterial.SetFloat(NoiseIntensityId, Mathf.Clamp(noiseIntensity > 0f ? noiseIntensity : 0.02f, 0f, 0.25f));
        _nightVisionMaterial.SetFloat(VignetteRadiusId, vignetteRadius > 0.01f ? vignetteRadius : 1.12f);
        _nightVisionMaterial.SetFloat(VignetteSoftnessId, vignetteSoftness > 0.01f ? vignetteSoftness : 2.52f);
        _nightVisionMaterial.SetFloat(LensGapId, lensGap > 0.001f ? lensGap : 0.18f);
        _nightVisionMaterial.SetFloat(Time1Id, Time.time);
        _nightVisionMaterial.SetFloat(GainId, gain > 0.01f ? gain : 25f);
        _nightVisionMaterial.SetFloat(GammaCurveId, gammaCurve > 0.01f ? gammaCurve : 0.7f);
    }

    private void OnDisable()
    {
        if (_subscribedToRendering)
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            _subscribedToRendering = false;
        }

        DisableInput();
        ClearNightVisionIfOwned();
        DestroyMaterial();
    }

    public override void OnStopAuthority()
    {
        DisableInput();
        ClearNightVisionIfOwned();
    }

    private void DisableInput()
    {
        if (_playerInput == null)
            return;

        _playerInput.Player.KillerNightVision.performed -= OnNightVisionPressed;
        _playerInput.Disable();
        _playerInput = null;
    }

    private void ClearNightVisionIfOwned()
    {
        nightVisionActive = false;

        if (NightVisionRenderer.ActiveCamera != killerCamera)
            return;

        NightVisionRenderer.IsActive = false;
        NightVisionRenderer.ActiveCamera = null;
    }

    private void DestroyMaterial()
    {
        if (_nightVisionMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(_nightVisionMaterial);
        else
            DestroyImmediate(_nightVisionMaterial);

        _nightVisionMaterial = null;
    }
}