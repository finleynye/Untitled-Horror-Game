using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    public PlayerStamina playerStamina;
    [FormerlySerializedAs("wheelRed")] public Image sprintBarBackground;
    [FormerlySerializedAs("wheelGreen")] public Image sprintAmountBar;
    public bool showStaminaText;

    [Header("Fade")]
    [SerializeField] private CanvasGroup localPopUpUI;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float hideDelay = 1f;

    private float fullStaminaTimer;
    private float backgroundStartXScale = 1f;
    private float amountStartXScale = 1f;

    private void Start()
    {
        if (playerStamina == null)
            FindLocalPlayerStamina();

        SetImageToBar(sprintBarBackground);
        SetImageToBar(sprintAmountBar);

        if (sprintBarBackground != null)
            backgroundStartXScale = sprintBarBackground.rectTransform.localScale.x;

        if (sprintAmountBar != null)
            amountStartXScale = sprintAmountBar.rectTransform.localScale.x;

        if (localPopUpUI != null)
            localPopUpUI.alpha = 0f;
    }

    private void Update()
    {
        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (playerStamina == null || sprintAmountBar == null)
            return;

        float sprintAmount = Mathf.Clamp01(playerStamina.CurrentStamina / playerStamina.MaxStamina);

        SetXScale(sprintAmountBar.rectTransform, amountStartXScale * sprintAmount);

        if (sprintBarBackground != null)
            SetXScale(sprintBarBackground.rectTransform, backgroundStartXScale);

        UpdateFade(sprintAmount);
    }

    private void UpdateFade(float sprintAmount)
    {
        if (localPopUpUI == null)
            return;

        bool shouldShow = playerStamina.isUsingStamina || sprintAmount < 1f;

        if (shouldShow)
        {
            fullStaminaTimer = 0f;
            localPopUpUI.alpha = Mathf.MoveTowards(localPopUpUI.alpha, 1f, fadeSpeed * Time.deltaTime);
            return;
        }

        fullStaminaTimer += Time.deltaTime;

        if (fullStaminaTimer >= hideDelay)
            localPopUpUI.alpha = Mathf.MoveTowards(localPopUpUI.alpha, 0f, fadeSpeed * Time.deltaTime);
    }

    private void SetImageToBar(Image image)
    {
        if (image == null)
            return;

        image.type = Image.Type.Simple;
        image.fillAmount = 1f;
        image.rectTransform.pivot = new Vector2(0.5f, image.rectTransform.pivot.y);
    }

    private void SetXScale(RectTransform rectTransform, float xScale)
    {
        Vector3 scale = rectTransform.localScale;
        scale.x = xScale;
        rectTransform.localScale = scale;
    }

    private void FindLocalPlayerStamina()
    {
        PlayerStamina[] staminaScripts = FindObjectsByType<PlayerStamina>(FindObjectsSortMode.None);

        foreach (PlayerStamina stamina in staminaScripts)
        {
            if (stamina.isOwned)
            {
                playerStamina = stamina;
                return;
            }
        }
    }
}
