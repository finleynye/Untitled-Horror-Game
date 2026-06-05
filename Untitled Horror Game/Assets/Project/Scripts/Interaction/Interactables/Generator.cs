using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class Generator : MonoBehaviour
{
    [Header("Generator Parts Requirements")]
    [SerializeField] private int requiredParts = 4;
    [SerializeField] private int requiredFuel = 1;

    private int currentParts = 0;
    private int currentFuel = 0;

    private bool hasGeneratorStarted = false;
    private bool isShaking = false;

    [Header("References")]
    [SerializeField] private Interactable generatorInteractable;
    [SerializeField] private GameObject generatorLight;
    [SerializeField] private ParticleSystem smokeParticles;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text errorText;

    [Header("Events")]
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

        //generator starts off
        if (generatorLight != null)
            generatorLight.SetActive(false);

        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }

    public void AddGeneratorPart()
    {
        if (hasGeneratorStarted) return;

        currentParts++;

        if (currentParts > requiredParts)
            currentParts = requiredParts;

        if (au_generator != null && addPartSound != null)
            au_generator.PlayOneShot(addPartSound);

        if (errorText != null)
            errorText.text = "";

        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }

    public void AddFuel()
    {
        if (hasGeneratorStarted) return;

        currentFuel++;

        if (currentFuel > requiredFuel)
            currentFuel = requiredFuel;

        if (au_generator != null && addFuelSound != null)
            au_generator.PlayOneShot(addFuelSound);

        if (errorText != null)
            errorText.text = "";

        UpdateGeneratorUI();
        UpdateGeneratorUIPrompt();
    }

    public void TryStartGenerator()
    {
        ShakeGenerator();

        //if generator is already running
        if (hasGeneratorStarted)
        {
            onGeneratorAlreadyRunning?.Invoke();

            if (errorText != null)
                errorText.text = "Generator is already running.";

            return;
        }

        //if not enough parts
        if (currentParts < requiredParts)
        {
            onMissingParts?.Invoke();

            if (errorText != null)
                errorText.text = "Missing generator parts.";

            UpdateGeneratorUIPrompt();
            return;
        }

        //if not enough fuel
        if (currentFuel < requiredFuel)
        {
            onMissingFuel?.Invoke();

            if (errorText != null)
                errorText.text = "Missing fuel.";

            UpdateGeneratorUIPrompt();
            return;
        }

        StartGenerator();
    }

    public void StartGenerator()
    {
        hasGeneratorStarted = true;

        if (generatorLight != null)
            generatorLight.SetActive(true);

        if (smokeParticles != null)
            smokeParticles.Stop();

        if (au_generator != null && runningSound != null)
        {
            au_generator.clip = runningSound;
            au_generator.loop = true;
            au_generator.Play();
        }

        errorText.text = "";

        if (generatorInteractable != null)
        {
            generatorInteractable.interactionPrompt = "Generator Started";
            generatorInteractable.isReusable = false;
        }
        UpdateGeneratorUI();

        onGeneratorStarted?.Invoke();
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

        //save the generator position when the shake starts
        Vector3 originalPosition = transform.localPosition;

        while (timer < shakeDuration)
        {
            //shake all axis randomly
            float randomX = Random.Range(-shakeAmount, shakeAmount); 
            float randomY = Random.Range(-shakeAmount, shakeAmount);
            float randomZ = Random.Range(-shakeAmount, shakeAmount);

            transform.localPosition = originalPosition + new Vector3(randomX, randomY, randomZ);

            timer += Time.deltaTime;

            yield return null;
        }

        //return to the position it had before this specific shake
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
            return;
        }

        progressText.text = $"Current Parts: {currentParts} / {requiredParts}\n" + $"Current Fuel: {currentFuel} / {requiredFuel}"; //on a new line now rather than before it was cramped with no space
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

}