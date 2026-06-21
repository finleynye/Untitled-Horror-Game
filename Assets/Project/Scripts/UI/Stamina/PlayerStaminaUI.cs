using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerStaminaUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerStamina playerStamina;
    public Image wheelBackground;
    public Image wheelFill;
    public bool showStaminaText;

    [Header("Stamina Colours")]
    public Color normalColour = new Color32(125, 207, 160, 255);   
    public Color lowColour = new Color32(214, 184, 90, 255);       
    public Color criticalColour = new Color32(196, 91, 91, 255); 

    [Header("Colour Settings")]
    [Range(0f, 1f)] public float lowStaminaPoint = 0.5f;
    [Range(0f, 1f)] public float criticalStaminaPoint = 0.2f;
    public float colourLerpSpeed = 8f;

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private float canvasDistanceFromCamera = 0.2f;

    void Start()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (playerStamina == null)
            FindLocalPlayerStamina();

        FindLocalPlayerCamera();
    }

    void Update()
    {
        if (playerStamina == null || !playerStamina.isActiveAndEnabled)
        {
            FindLocalPlayerStamina();
            FindLocalPlayerCamera();
            return;
        }

        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (playerStamina == null)
            return;

        float maxStamina = Mathf.Max(1f, playerStamina.MaxStamina);
        float staminaPercent = Mathf.Clamp01(playerStamina.CurrentStamina / maxStamina);

        if (wheelFill != null)
        {
            wheelFill.fillAmount = staminaPercent;
            wheelFill.color = Color.Lerp(wheelFill.color, GetStaminaColour(staminaPercent), Time.deltaTime * colourLerpSpeed);
        }

        if (wheelBackground != null)
        {
            wheelBackground.fillAmount = 1f;
        }
    }

    private Color GetStaminaColour(float staminaPercent)
    {
        if (staminaPercent <= criticalStaminaPoint)
            return criticalColour;

        if (staminaPercent <= lowStaminaPoint)
        {
            float colourBlend = Mathf.InverseLerp(criticalStaminaPoint, lowStaminaPoint, staminaPercent);
            return Color.Lerp(criticalColour, lowColour, colourBlend);
        }

        float normalColourBlend = Mathf.InverseLerp(lowStaminaPoint, 1f, staminaPercent);
        return Color.Lerp(lowColour, normalColour, normalColourBlend);
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

    private void FindLocalPlayerCamera()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (canvas == null)
            return;

        GameObject roleObject = GetLocalRoleObject();

        if (roleObject == null)
            return;

        Camera localCamera = roleObject.GetComponentInChildren<Camera>(true);

        if (localCamera == null)
            return;
    }

    private GameObject GetLocalRoleObject()
    {
        if (NetworkClient.localPlayer == null)
            return null;

        PlayerController playerController = NetworkClient.localPlayer.GetComponent<PlayerController>();

        if (playerController == null)
            return null;

        return playerController.GetCurrentRoleObject();
    }
}