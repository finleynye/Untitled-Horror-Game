using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerStaminaUI : MonoBehaviour
{
    public PlayerStamina playerStamina;
    public Image wheelRed, wheelGreen;
    public bool showStaminaText;

    [Header("Canvas")]
    [SerializeField] private Canvas staminaCanvas;

    void Start()
    {

        if (staminaCanvas == null)
            staminaCanvas = GetComponent<Canvas>();

        if(playerStamina == null)
            FindLocalPlayerStamina();
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

    private void FindLocalPlayerStamina()
    {
        PlayerStamina[] staminaScripts = FindObjectsByType<PlayerStamina>(FindObjectsSortMode.None);

        foreach (PlayerStamina stamina in staminaScripts)
        {
            if (stamina.isOwned)
            {
                playerStamina = stamina;

                Camera localCamera = stamina.GetComponentInChildren<Camera>(true);

                if (localCamera != null && staminaCanvas != null)
                {
                    staminaCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    staminaCanvas.worldCamera = localCamera;
                    staminaCanvas.planeDistance = 1f;
                }

                return;
            }
        }
    }
}
