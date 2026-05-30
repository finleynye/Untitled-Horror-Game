using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStaminaUI : MonoBehaviour
{
    public PlayerStamina playerStamina;
    public Image wheelRed, wheelGreen;
    public bool showStaminaText;

    void Start()
    {
        if(playerStamina == null)
        {
            playerStamina = FindAnyObjectByType<PlayerStamina>();
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


        wheelGreen.fillAmount = playerStamina.currentStamina / 100;
        if(playerStamina.currentStamina > 1 && playerStamina.isUsingStamina)
        {
            wheelRed.fillAmount = (playerStamina.currentStamina + 5) / 100;
        }
        else
        {
            wheelRed.fillAmount--;
        }

    }
}
