using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStaminaUI : MonoBehaviour
{
    public PlayerStamina playerStamina;
    public Slider staminaSlider;
    public TextMeshProUGUI staminaText;
    public bool showStaminaText;

    void Start()
    {
        if(playerStamina == null)
        {
            playerStamina = FindAnyObjectByType<PlayerStamina>();
        }

        if(staminaSlider != null && playerStamina != null)
        {
            staminaSlider.maxValue = playerStamina.maxStamina;
            staminaSlider.value = playerStamina.currentStamina;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (playerStamina == null)
            return;

        if(staminaSlider != null)
        {
            staminaSlider.maxValue = playerStamina.maxStamina;
            staminaSlider.value = playerStamina.currentStamina;
        }

        if(staminaText != null && showStaminaText)
        {
            staminaText.text = Mathf.RoundToInt(playerStamina.currentStamina).ToString();
        }
    }
}
