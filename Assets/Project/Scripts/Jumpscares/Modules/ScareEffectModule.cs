using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class ScareEffectsModule
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Volume scareVolume;

    [Header("FOV")]
    [SerializeField] private float scareFOV = 95f;
    [SerializeField] private float fovInTime = 0.2f;
    [SerializeField] private float fovOutTime = 0.8f;

    [Header("Post Processing")]
    [SerializeField]
    private Color scareFilterColor =
        new Color(1f, 0.25f, 0.25f);

    [SerializeField] private float scareSaturation = -35f;
    [SerializeField] private float scareContrast = 30f;
    [SerializeField] private float scareVignetteIntensity = 0.55f;
    [SerializeField] private float scareChromaticAberration = 0.6f;

    private float originalFOV;

    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private ChromaticAberration chromaticAberration;

    private Color originalColourFilter;
    private float originalSaturation;
    private float originalContrast;
    private float originalVignetteIntensity;
    private float originalChromaticAberration;

    public void Initialise(Camera fallbackCamera)
    {
        if (playerCamera == null)
            playerCamera = fallbackCamera;

        if (playerCamera != null)
            originalFOV = playerCamera.fieldOfView;

        CachePostProcessingValues();
    }

    //play camera effect (post processing, vignette, colour changes
    public IEnumerator PlayRoutine(float totalDuration)
    {
        float timer = 0f;

        while (timer < fovInTime)
        {
            timer += Time.deltaTime;

            float t = fovInTime > 0f ? Mathf.Clamp01(timer / fovInTime) : 1f;

            ApplyFOV(Mathf.Lerp(originalFOV, scareFOV, t));
            ApplyPostProcessing(t);

            yield return null;
        }

        float holdTime = Mathf.Max(0f, totalDuration - fovInTime - fovOutTime);

        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

        timer = 0f;

        while (timer < fovOutTime)
        {
            timer += Time.deltaTime;

            float t = fovOutTime > 0f ? Mathf.Clamp01(timer / fovOutTime) : 1f;

            ApplyFOV(Mathf.Lerp(scareFOV, originalFOV, t));
            ApplyPostProcessing(1f - t);

            yield return null;
        }

        ResetImmediately();
    }

    public void ResetImmediately()
    {
        ApplyFOV(originalFOV);
        ApplyPostProcessing(0f);
    }

    private void ApplyFOV(float value)
    {
        if (playerCamera != null)
            playerCamera.fieldOfView = value;
    }

    private void CachePostProcessingValues()
    {
        if (scareVolume == null || scareVolume.profile == null)
            return;

        scareVolume.profile.TryGet(out colorAdjustments);
        scareVolume.profile.TryGet(out vignette);
        scareVolume.profile.TryGet(out chromaticAberration);

        if (colorAdjustments != null)
        {
            originalColourFilter = colorAdjustments.colorFilter.value;
            originalSaturation = colorAdjustments.saturation.value;
            originalContrast = colorAdjustments.contrast.value;
        }

        if (vignette != null)
            originalVignetteIntensity = vignette.intensity.value;

        if (chromaticAberration != null)
            originalChromaticAberration = chromaticAberration.intensity.value;

    }

    private void ApplyPostProcessing(float amount)
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(originalColourFilter, scareFilterColor, amount);
            colorAdjustments.saturation.value = Mathf.Lerp(originalSaturation, scareSaturation, amount);
            colorAdjustments.contrast.value = Mathf.Lerp(originalContrast, scareContrast, amount);
        }

        vignette.intensity.value = Mathf.Lerp(originalVignetteIntensity, scareVignetteIntensity, amount);
        chromaticAberration.intensity.value = Mathf.Lerp(originalChromaticAberration, scareChromaticAberration, amount);
    }
}