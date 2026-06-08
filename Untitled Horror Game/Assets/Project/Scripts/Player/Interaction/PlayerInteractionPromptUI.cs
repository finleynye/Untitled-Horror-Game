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
        if (promptPanel != null)
            promptPanel.SetActive(false);

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
            promptPanel.SetActive(false);
            return;
        }

        Interactable interactable = playerInteraction.CurrentInteractable;

        if (interactable == null || !interactable.CanShowPrompt())
        {
            promptPanel.SetActive(false);
            return;
        }

        promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = interactable.GetInteractionPrompt();
    }
}