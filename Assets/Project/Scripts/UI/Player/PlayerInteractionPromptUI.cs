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
        EnsurePromptReferences();
        HidePrompt();

        if (playerInteraction == null)
            FindLocalPlayerInteraction();
        
    }

    private void Update()
    {
        EnsurePromptReferences();

        if (playerInteraction != null && (!playerInteraction.isActiveAndEnabled || !playerInteraction.isOwned))
            playerInteraction = null;

        if (playerInteraction == null)
            FindLocalPlayerInteraction();
        
        UpdatePromptUI();
    }

    private void FindLocalPlayerInteraction()
    {
        PlayerInteraction[] interactions = FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None);

        foreach (PlayerInteraction interaction in interactions)
        {
            if (interaction != null && interaction.isActiveAndEnabled && interaction.isOwned)
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

    private void EnsurePromptReferences()
    {
        if (promptPanel != null && promptText != null)
            return;

        if (promptPanel == null)
        {
            promptPanel = new GameObject("Interaction Prompt", typeof(RectTransform));
            promptPanel.transform.SetParent(transform, false);

            RectTransform promptRect = promptPanel.GetComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = new Vector2(0f, -160f);
            promptRect.sizeDelta = new Vector2(520f, 80f);
        }

        if (promptText == null)
        {
            promptText = promptPanel.GetComponentInChildren<TMP_Text>(true);

            if (promptText == null)
            {
                GameObject textObject = new GameObject("Prompt Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(promptPanel.transform, false);

                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                promptText = textObject.GetComponent<TextMeshProUGUI>();
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.fontSize = 28f;
                promptText.color = Color.white;
                promptText.raycastTarget = false;
            }
        }
    }
}
