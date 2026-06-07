using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using Mirror;

public class Generator : NetworkBehaviour
{
    [Header("Generator Parts Requirements")]
    [SerializeField] private int requiredParts = 4;
    [SerializeField] private int requiredFuel = 1;

    [SyncVar(hook = nameof(OnPartsChanged))]
    private int currentParts = 0;

    [SyncVar(hook = nameof(OnFuelChanged))]
    private int currentFuel = 0;

    [SyncVar(hook = nameof(OnGeneratorStartedChanged))]
    private bool hasGeneratorStarted = false;

    private bool isShaking = false;

    [Header("References")]
    [SerializeField] private Interactable generatorInteractable;
    [SerializeField] private GameObject generatorLight;
    [SerializeField] private ParticleSystem smokeParticles;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text errorText;

    [Header("Server Events")]
    public UnityEvent onMissingParts;
    public UnityEvent onMissingFuel;
    public UnityEvent onGeneratorStarted;
    public UnityEvent onGeneratorAlreadyRunning;

    [Header("Text Progress Colour")]
    public Color onMissingPartsColour = Color.red;
    public Color onMissingFuelColour = Color.orange;
    public Color onCompletionColour = Color.green;
    public Color normalTextColour = Color.white;

    [Header("Audio Cues & Ambience")]
    public AudioSource au_generator;
    public AudioClip runningSound;
    public AudioClip addFuelSound;
    public AudioClip addPartSound;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 1f;
    [SerializeField] private float shakeAmount = 0.04f;

    private void Start()
    {
        if (smokeParticles == null)
            smokeParticles = GetComponentInChildren<ParticleSystem>();

        // generator starts off
        if (generatorLight != null)
            generatorLight.SetActive(false);

        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }

    [Server]
    public void AddGeneratorPart()
    {
        if (hasGeneratorStarted) return;

        currentParts++;

        if (currentParts > requiredParts)
            currentParts = requiredParts;

        RpcPlayAddPartFeedback();

        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }

    [Server]
    public void AddFuel()
    {
        if (hasGeneratorStarted) return;

        currentFuel++;

        if (currentFuel > requiredFuel)
            currentFuel = requiredFuel;

        RpcPlayAddFuelFeedback();

        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }

    [Server]
    public void TryStartGenerator()
    {
        if (hasGeneratorStarted)
        {
            InvokeGeneratorAlreadyRunningEvent();

            RpcAlreadyRunningFeedback();
            return;
        }

        if (currentParts < requiredParts)
        {
            InvokeMissingPartsEvent();

            RpcMissingPartsFeedback();
            return;
        }

        if (currentFuel < requiredFuel)
        {
            InvokeMissingFuelEvent();

            RpcMissingFuelFeedback();
            return;
        }

        StartGenerator();
    }

    [Server]
    public void StartGenerator()
    {
        if (hasGeneratorStarted) return;

        hasGeneratorStarted = true;

        InvokeGeneratorStartedEvent();

        RpcGeneratorStartedEffects();
    }

    //server event wrappers (these events are server only, used for objective logic and game states)
    [Server]
    private void InvokeMissingPartsEvent()
    {
        onMissingParts?.Invoke();
    }

    [Server]
    private void InvokeMissingFuelEvent()
    {
        onMissingFuel?.Invoke();
    }

    [Server]
    private void InvokeGeneratorStartedEvent()
    {
        onGeneratorStarted?.Invoke();
    }

    [Server]
    private void InvokeGeneratorAlreadyRunningEvent()
    {
        onGeneratorAlreadyRunning?.Invoke();
    }

    //helper methods for completion events
    public bool IsGeneratorStarted()
    {
        return hasGeneratorStarted;
    }

    public bool HasAllTheParts()
    {
        return currentParts >= requiredParts;
    }

    public bool HasAllFuel()
    {
        return currentFuel >= requiredFuel;
    }

    private void ShakeGenerator()
    {
        if (isShaking) return;

        StartCoroutine(ShakeGeneratorRoutine());
    }

    private IEnumerator ShakeGeneratorRoutine()
    {
        isShaking = true;

        float timer = 0f;

        //shake around the current local position, then return to it
        Vector3 originalPosition = transform.localPosition;

        while (timer < shakeDuration)
        {
            float randomX = Random.Range(-shakeAmount, shakeAmount);
            float randomY = Random.Range(-shakeAmount, shakeAmount);
            float randomZ = Random.Range(-shakeAmount, shakeAmount);

            transform.localPosition = originalPosition + new Vector3(randomX, randomY, randomZ);

            timer += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPosition;

        isShaking = false;

        if (smokeParticles != null && !hasGeneratorStarted)
            smokeParticles.Play();
    }

    private void UpdateGeneratorUI()
    {
        if (progressText == null) return;

        if (hasGeneratorStarted)
        {
            progressText.text = "Generator Online";
            progressText.color = onCompletionColour;
            return;
        }

        progressText.color = normalTextColour;

        progressText.text =
            $"Current Parts: {currentParts} / {requiredParts}\n" +
            $"Current Fuel: {currentFuel} / {requiredFuel}";
    }

    private void UpdateGeneratorUIPrompt()
    {
        if (generatorInteractable == null) return;

        if (hasGeneratorStarted)
        {
            generatorInteractable.interactionPrompt = "Generator Active";
            return;
        }

        if (currentParts < requiredParts)
        {
            generatorInteractable.interactionPrompt = "Needs Parts...";
            return;
        }

        if (currentFuel < requiredFuel)
        {
            generatorInteractable.interactionPrompt = "Needs Fuel...";
            return;
        }

        generatorInteractable.interactionPrompt = "Start Generator";
    }

    private void OnPartsChanged(int oldValue, int newValue)
    {
        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }

    private void OnFuelChanged(int oldValue, int newValue)
    {
        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }

    private void OnGeneratorStartedChanged(bool oldValue, bool newValue)
    {
        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();

        if (newValue)
            ApplyGeneratorStartedVisuals();
    }

    //runs feedback on every client
    private void ApplyGeneratorStartedVisuals()
    {
        if (generatorLight != null)
            generatorLight.SetActive(true);

        if (smokeParticles != null)
            smokeParticles.Stop();

        if (au_generator != null && runningSound != null)
        {
            au_generator.clip = runningSound;
            au_generator.loop = true;

            if (!au_generator.isPlaying)
                au_generator.Play();
        }

        if (generatorInteractable != null)
        {
            generatorInteractable.interactionPrompt = "Generator Started";
            generatorInteractable.isReusable = false;
        }

        if (errorText != null)
            errorText.text = "";
    }

    [ClientRpc]
    private void RpcPlayAddPartFeedback()
    {
        if (au_generator != null && addPartSound != null)
            au_generator.PlayOneShot(addPartSound);

        if (errorText != null)
            errorText.text = "";
    }

    [ClientRpc]
    private void RpcPlayAddFuelFeedback()
    {
        if (au_generator != null && addFuelSound != null)
            au_generator.PlayOneShot(addFuelSound);

        if (errorText != null)
            errorText.text = "";
    }

    [ClientRpc]
    private void RpcMissingPartsFeedback()
    {
        if (errorText != null)
        {
            errorText.text = "Missing generator parts.";
            errorText.color = onMissingPartsColour;
        }

        ShakeGenerator();
        UpdateGeneratorUIPrompt();
    }

    [ClientRpc]
    private void RpcMissingFuelFeedback()
    {
        if (errorText != null)
        {
            errorText.text = "Missing fuel.";
            errorText.color = onMissingFuelColour;
        }

        ShakeGenerator();
        UpdateGeneratorUIPrompt();
    }

    [ClientRpc]
    private void RpcAlreadyRunningFeedback()
    {
        if (errorText != null)
        {
            errorText.text = "Generator is already running.";
            errorText.color = onCompletionColour;
        }

        UpdateGeneratorUIPrompt();
    }

    [ClientRpc]
    private void RpcGeneratorStartedEffects()
    {
        ApplyGeneratorStartedVisuals();
        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }
}