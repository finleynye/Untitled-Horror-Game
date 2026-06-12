using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITextButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Button Event")]
    public UnityEvent buttonEvent;

    [Header("Text Reference")]
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Text Colours")]
    [SerializeField] private bool useColourEffects = true;

    [SerializeField] private Color normalTextColour = new Color32(235, 220, 190, 255);
    [SerializeField] private Color hoverTextColour = new Color32(255, 80, 70, 255);
    [SerializeField] private Color clickedTextColour = new Color32(170, 20, 20, 255);
    [SerializeField] private Color disabledTextColour = new Color32(120, 110, 100, 180);

    [SerializeField] private float colourInterpSpeed = 12f;

    [Header("Scale Effects")]
    [SerializeField] private bool useScaleEffects = true;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float clickScale = 0.92f;
    [SerializeField] private float scaleInterpSpeed = 12f;

    [Header("Rotation Effects")]
    [SerializeField] private bool useRotationEffects = true;
    [SerializeField] private float randomRotationAmount = 3f;
    [SerializeField] private float rotationInterpSpeed = 12f;

    [Header("Fade Settings")]
    [SerializeField] private bool fadeBeforeClickEvent = false;
    [SerializeField] private ScreenFade screenFade;

    [Header("Unity Button Support")]
    [SerializeField] private Button button;
    [SerializeField] private bool disableUnityButtonOnClick = true;

    [Header("Text Button States")]
    public bool isHovering;
    public bool isClicking;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private Quaternion originalRotation;
    private Quaternion targetRotation;

    private Color targetTextColour;

    private bool manualInteractable = true;
    private bool hasClickedThisFrame;

    private bool IsInteractable => manualInteractable && (button == null || button.interactable);

    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponent<TextMeshProUGUI>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (screenFade == null)
            screenFade = FindFirstObjectByType<ScreenFade>();

        if (button != null)
        {
            button.transition = Selectable.Transition.None;

            if (disableUnityButtonOnClick)
                button.onClick.RemoveAllListeners();
        }

        originalScale = transform.localScale;
        targetScale = originalScale;

        originalRotation = transform.localRotation;
        targetRotation = originalRotation;

        targetTextColour = normalTextColour;

        ApplyTextColourInstant();
    }

    private void LateUpdate()
    {
        hasClickedThisFrame = false;
    }

    private void Update()
    {
        if (!IsInteractable)
        {
            isHovering = false;
            isClicking = false;

            targetScale = originalScale;
            targetRotation = originalRotation;
            targetTextColour = disabledTextColour;
        }

        if (useScaleEffects)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleInterpSpeed * Time.unscaledDeltaTime);

        if (useRotationEffects)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationInterpSpeed * Time.unscaledDeltaTime);

        if (useColourEffects)
            UpdateTextColour();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isHovering = true;

        if (!isClicking)
        {
            targetScale = originalScale * hoverScale;
            targetTextColour = hoverTextColour;
        }

        if (useRotationEffects)
        {
            float randomZ = Random.Range(-randomRotationAmount, randomRotationAmount);
            targetRotation = originalRotation * Quaternion.Euler(0f, 0f, randomZ);
        }

        SoundManager.PlaySound(SoundType.UI_BUTTON_HOVER, 1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isClicking = true;

        targetScale = originalScale * clickScale;
        targetTextColour = clickedTextColour;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isClicking = false;

        if (isHovering)
        {
            targetScale = originalScale * hoverScale;
            targetTextColour = hoverTextColour;
        }
        else
        {
            targetScale = originalScale;
            targetTextColour = normalTextColour;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        RunClickEvent();
    }

    public void RunClickEvent()
    {
        if (!IsInteractable) return;

        if (hasClickedThisFrame)
            return;

        hasClickedThisFrame = true;

        SoundManager.PlaySound(SoundType.UI_BUTTON_PRESSED, 1f);

        if (fadeBeforeClickEvent && screenFade != null)
        {
            screenFade.FadeOutThenRun(buttonEvent);
            return;
        }

        buttonEvent?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isHovering = false;
        isClicking = false;

        targetScale = originalScale;
        targetRotation = originalRotation;
        targetTextColour = normalTextColour;
    }

    public void SetInteractable(bool value)
    {
        manualInteractable = value;

        if (button != null)
            button.interactable = value;

        targetTextColour = value ? normalTextColour : disabledTextColour;
    }

    private void UpdateTextColour()
    {
        if (buttonText != null)
            buttonText.color = Color.Lerp(buttonText.color, targetTextColour, colourInterpSpeed * Time.unscaledDeltaTime);
    }

    private void ApplyTextColourInstant()
    {
        if (buttonText != null)
            buttonText.color = targetTextColour;
    }
}