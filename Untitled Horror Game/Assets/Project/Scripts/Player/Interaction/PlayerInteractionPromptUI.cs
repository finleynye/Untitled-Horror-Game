using UnityEngine;
using TMPro;

public class PlayerInteractionPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;

    private void Start()
    {
        HidePrompt();

        if (playerInteraction == null)
            FindLocalPlayerInteraction();
        
    }

    private void Update()
    {
        if (playerInteraction == null)
            FindLocalPlayerInteraction();
        
        UpdatePromptUI();
    }

    private void FindLocalPlayerInteraction()
    {
        PlayerInteraction[] interactions = FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None);

        foreach (PlayerInteraction interaction in interactions)
        {
            if (interaction.isOwned)
            {
                playerInteraction = interaction;
                return;
            }
        }
    }

    private void UpdatePromptUI()
    {
        if (promptPanel == null)
            return;
        

        if (playerInteraction == null)
        {
            HidePrompt();
            return;
        }

        Interactable interactable = playerInteraction.CurrentInteractable;

        if (interactable == null || !interactable.CanShowPrompt())
        {
            HidePrompt();
            return;
        }

        ShowPrompt(interactable.GetInteractionPrompt());
    }

    private void ShowPrompt(string newPromptText)
    {
        promptPanel.SetActive(true);

        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = newPromptText;
        }
    }

    private void HidePrompt()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
        

        if (promptText != null)
            promptText.text = "";
    }
}