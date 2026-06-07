using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Button Event")]
    public UnityEvent buttonEvent; //assign whatever you want the button doing here

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
    [SerializeField] private float rotationInterpSpeed = 12f; //how quicklty the button rotates

    private Quaternion originalRotation;
    private Quaternion targetRotation;
    
    private Vector3 originalScale;
    private Vector3 targetScale;

    private Button button;
    private bool IsInteractable => button == null || button.interactable; //get the button interactable by default if it exists, else just like idk
    
    private void Start()
    {
        button = GetComponent<Button>();
        
        originalScale = transform.localScale;
        targetScale = originalScale;

        originalRotation = transform.localRotation;
        targetRotation = originalRotation;
    }
    
    private void Update()
    {
        if (!IsInteractable)
        {
            isHovering = false;
            isClicking = false;
            targetScale = originalScale;
            targetRotation = originalRotation;
        }
        
        if (useScaleEffects)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleInterpSpeed * Time.unscaledDeltaTime);

        if(useRotationEffects)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationInterpSpeed * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isHovering = true;

        if(!isClicking)
            targetScale = originalScale * hoverScale;

        if (useRotationEffects)
        {
            float randomZ = Random.Range(-randomRotationAmount, randomRotationAmount);
            targetRotation = originalRotation * Quaternion.Euler(0f, 0f, randomZ);
        }

        SoundManager.PlaySound(SoundType.UI_BUTTON_HOVER, 1f);
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!IsInteractable) return;
        
        isClicking = true;

        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        isClicking = false;

        if (isHovering)
            targetScale = originalScale * hoverScale;
        else
            targetScale = originalScale;

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable) return;

        Debug.Log("UI Button Clicked: " + gameObject.name);


        SoundManager.PlaySound(SoundType.UI_BUTTON_PRESSED,1f);

        buttonEvent?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!IsInteractable) return;

        isHovering = false;
        isClicking = false;

        targetScale = originalScale;
        targetRotation = originalRotation;
    }
    
    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }
    
}
