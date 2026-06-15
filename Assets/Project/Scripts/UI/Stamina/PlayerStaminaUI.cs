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
        if (playerStamina == null || !playerStamina.isActiveAndEnabled)
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

        float maxStamina = Mathf.Max(1f, playerStamina.MaxStamina);

        if (wheelGreen != null)
            wheelGreen.fillAmount = playerStamina.CurrentStamina / maxStamina;

        if(playerStamina.currentStamina > 1 && playerStamina.isUsingStamina)
        {
            if (wheelRed != null)
                wheelRed.fillAmount = Mathf.Clamp01((playerStamina.CurrentStamina + 5f) / maxStamina);
        }
        else
        {
            if (wheelRed != null)
                wheelRed.fillAmount = Mathf.MoveTowards(wheelRed.fillAmount, 0f, Time.deltaTime);
        }

    }

    private void FindLocalPlayerStamina()
    {
        if (NetworkClient.localPlayer != null)
        {
            //player root has all roles so only use the enabled stamina one
            PlayerStamina[] localStaminaScripts = NetworkClient.localPlayer.GetComponentsInChildren<PlayerStamina>(true);

            foreach (PlayerStamina stamina in localStaminaScripts)
            {
                if (stamina != null && stamina.isActiveAndEnabled)
                {
                    playerStamina = stamina;
                    return;
                }
            }
        }

        PlayerStamina[] staminaScripts = FindObjectsByType<PlayerStamina>(FindObjectsSortMode.None);

        foreach (PlayerStamina stamina in staminaScripts)
        {
            if (stamina == null || !stamina.isActiveAndEnabled)
                continue;

            NetworkIdentity identity = stamina.GetComponentInParent<NetworkIdentity>();

            if (identity != null && (identity.isLocalPlayer || identity.isOwned))
            {
                playerStamina = stamina;

                return;
            }
        }
    }
}
