using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Generator : MonoBehaviour
{


    private int requiredParts = 4; //required total parts for fixing it
    private int requiredFuel = 1; //required fuel for turning on generator

    private int currentParts = 0; 
    private int currentFuel = 0; 

    private bool hasGeneratorStarted = false;

    [Header("References")]
    [SerializeField] private Interactable generatorInteractable;
    [SerializeField] private GameObject generatorLight;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text errorText;

    [Header("Events")]
    public UnityEvent onMissingParts;
    public UnityEvent onMissingFuel;
    public UnityEvent onGeneratorStarted;
    public UnityEvent OnGeneratorAlreadyRunning;

    [Header("Text Progress Colour")]
    public Color onMissingPartsColour = Color.red;
    public Color onMissingFuelColour = Color.orange;
    public Color OnCompletionColour = Color.green;

    [Header("Audio Cues & Ambience")]
    public AudioSource au_generator;
    public AudioClip runningSound;
    public AudioClip addFuelSound;
    public AudioClip addPartSound;


    void Start()
    {
        
        if(generatorLight != null)
            generatorLight.SetActive(false);
    }
    public void AddGeneratorPart()
    {
       if(hasGeneratorStarted) return;

       currentParts++;

        if(currentParts > requiredParts)
            currentParts = requiredParts;

        au_generator.PlayOneShot(addPartSound);
        UpdateGeneratorUI();
        UpdateGeneratorUI();
    }


    public void AddFuel()
    {
        if (hasGeneratorStarted) return;
        
        currentFuel++;

        if(currentFuel > requiredFuel)
            currentFuel = requiredFuel;


        au_generator.PlayOneShot(addFuelSound);
        UpdateGeneratorUI();
        UpdateGeneratorUI();
    }

    public void TryStartGenerator()
    {
        Debug.Log("Trying generator. Parts: " + currentParts + "/" + requiredParts +
          " Fuel: " + currentFuel + "/" + requiredFuel);

        if (hasGeneratorStarted)
        {
            OnGeneratorAlreadyRunning?.Invoke();
            progressText.color = OnCompletionColour;
            return;
        }

        if (currentParts < requiredParts)
        {
            Debug.Log("Generator needs more parts.");
            onMissingParts?.Invoke();
            progressText.color = onMissingPartsColour;
            UpdateGeneratorUIPrompt();
            return;
        }

        if (currentFuel < requiredFuel)
        {
            Debug.Log("Generator needs fuel.");
            onMissingFuel?.Invoke();
            progressText.color = onMissingFuelColour;
            UpdateGeneratorUIPrompt();
            return;
        }

        StartGenerator();
    }
    public void StartGenerator()
    {
        hasGeneratorStarted = true;

        if (generatorLight == null)
            generatorLight.SetActive(true);

        progressText.color = OnCompletionColour;

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

    private void UpdateGeneratorUI()
    {
        if (progressText != null) return;

        if (hasGeneratorStarted)
        {
            progressText.text = "Generator Online";
            return;
        }

        progressText.text = $"Current Parts: {currentParts} / {requiredParts}" + $"Current Fuel: {currentFuel} / {requiredFuel}"; //displays a UI prompt with the current parts
                                                                                                                                 //out of the max available parts for machen and also current fuel and max fuel
    }

    private void UpdateGeneratorUIPrompt()
    {
        if (generatorInteractable == null) return;

        if (hasGeneratorStarted)
        {
            generatorInteractable.interactionPrompt = "Generator Active";
            return;
        }

        if(currentParts < requiredParts)
        {
            generatorInteractable.interactionPrompt = "Needs Parts...";
            return;
        }

        if(currentFuel < requiredFuel)
        {
            generatorInteractable.interactionPrompt = "Needs Fuel...";
        }

        generatorInteractable.interactionPrompt = "Start Generator";
    }
}
