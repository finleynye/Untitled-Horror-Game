using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerStaminaUI : MonoBehaviour
{
    public PlayerStamina playerStamina;
    public Image wheelRed, wheelGreen;
    public bool showStaminaText;

    void Start()
    {

        if(playerStamina == null)
            FindLocalPlayerStamina();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerStamina == null)
        {
            FindLocalPlayerStamina();
            return;
        }

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

    private void FindLocalPlayerStamina()
    {
        PlayerStamina[] staminaScripts = FindObjectsByType<PlayerStamina>(FindObjectsSortMode.None);

        foreach (PlayerStamina stamina in staminaScripts)
        {
            NetworkIdentity identity = stamina.GetComponentInParent<NetworkIdentity>();

            if (identity != null && (identity.isLocalPlayer || identity.isOwned))
            {
                playerStamina = stamina;

                return;
            }
        }
    }
}
