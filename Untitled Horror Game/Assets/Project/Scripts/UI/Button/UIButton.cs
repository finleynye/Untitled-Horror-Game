using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Button Event")]
    public UnityEvent buttonEvent; //assign whatever you want the button doing here

    [Header("Button References")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Button Colours")]
    [SerializeField] private bool useColourEffects = true;

    [SerializeField] private Color normalBackgroundColour = new Color32(216, 208, 189, 255); //SO MANY FUCKING COLOURS IDK GOO GOO HELPED ME
    [SerializeField] private Color normalTextColour = new Color32(23, 19, 19, 255);
    [SerializeField] private Color hoverBackgroundColour = new Color32(139, 30, 30, 255);
    [SerializeField] private Color hoverTextColour = new Color32(242, 232, 216, 255);
    [SerializeField] private Color clickedBackgroundColour = new Color32(74, 13, 13, 255);
    [SerializeField] private Color clickedTextColour = new Color32(255, 245, 230, 255);
    [SerializeField] private Color disabledBackgroundColour = new Color32(74, 69, 64, 255);
    [SerializeField] private Color disabledTextColour = new Color32(138, 129, 119, 255);

    [SerializeField] private float colourInterpSpeed = 12f;

    [Header("Scale Effects")]
    [SerializeField] private bool useScaleEffects = true;
    [SerializeField] private float hoverScale = 1.08f; //how big the button gets when hovered
    [SerializeField] private float clickScale = 0.92f; //how small the button gets once clicked
    [SerializeField] private float scaleInterpSpeed = 12f; //how quickly the button scales

    [Header("Button States")]
    public bool isHovering;
    public bool isClicking;

    [Header("Rotation Effects")]
    [SerializeField] private bool useRotationEffects = true;
    [SerializeField] private float randomRotationAmount = 3f; //small random tilt amount
    [SerializeField] private float rotationInterpSpeed = 12f; //how quickly the button rotates

    private Quaternion originalRotation;
    private Quaternion targetRotation;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private Color targetBackgroundColour;
    private Color targetTextColour;

    private Button button;
    private bool IsInteractable => button == null || button.interactable; //get the button interactable by default if it exists, else just like idk

    private void Start()
    {
        button = GetComponent<Button>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        originalScale = transform.localScale;
        targetScale = originalScale;

        originalRotation = transform.localRotation;
        targetRotation = originalRotation;

        targetBackgroundColour = normalBackgroundColour;
        targetTextColour = normalTextColour;

        ApplyButtonColoursInstant();
    }

    private void Update()
    {
        if (!IsInteractable)
        {
            isHovering = false;
            isClicking = false;

            targetScale = originalScale;
            targetRotation = originalRotation;

            targetBackgroundColour = disabledBackgroundColour;
            targetTextColour = disabledTextColour;
        }

        if (useScaleEffects)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleInterpSpeed * Time.unscaledDeltaTime);

        if (useRotationEffects)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationInterpSpeed * Time.unscaledDeltaTime);

        if (useColourEffects)
            UpdateButtonColours();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isHovering = true;

        if (!isClicking)
        {
            targetScale = originalScale * hoverScale;
            targetBackgroundColour = hoverBackgroundColour;
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
        targetBackgroundColour = clickedBackgroundColour;
        targetTextColour = clickedTextColour;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isClicking = false;

        if (isHovering)
        {
            targetScale = originalScale * hoverScale;
            targetBackgroundColour = hoverBackgroundColour;
            targetTextColour = hoverTextColour;
        }
        else
        {
            targetScale = originalScale;
            targetBackgroundColour = normalBackgroundColour;
            targetTextColour = normalTextColour;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        SoundManager.PlaySound(SoundType.UI_BUTTON_PRESSED, 1f);

        buttonEvent?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isHovering = false;
        isClicking = false;

        targetScale = originalScale;
        targetRotation = originalRotation;

        targetBackgroundColour = normalBackgroundColour;
        targetTextColour = normalTextColour;
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;

        if (value)
        {
            targetBackgroundColour = normalBackgroundColour;
            targetTextColour = normalTextColour;
        }
        else
        {
            targetBackgroundColour = disabledBackgroundColour;
            targetTextColour = disabledTextColour;
        }
    }

    private void UpdateButtonColours()
    {
        if (buttonImage != null)
            buttonImage.color = Color.Lerp(buttonImage.color, targetBackgroundColour, colourInterpSpeed * Time.unscaledDeltaTime);

        if (buttonText != null)
            buttonText.color = Color.Lerp(buttonText.color, targetTextColour, colourInterpSpeed * Time.unscaledDeltaTime);
    }

    private void ApplyButtonColoursInstant()
    {
        if (buttonImage != null)
            buttonImage.color = targetBackgroundColour;

        if (buttonText != null)
            buttonText.color = targetTextColour;
    }
}