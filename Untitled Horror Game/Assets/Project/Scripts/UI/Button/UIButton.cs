using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Button Event")]
    public UnityEvent buttonEvent; //assign whatever you want the button doing here

    [Header("Scale Effects")]
    [SerializeField] private bool useScaleEffects = true;
    [SerializeField] private float hoverScale = 1.08f; //how big the button gets when hovered
    [SerializeField] private float clickScale = 0.92f; //how small the button gets once clicked
    [SerializeField] private float scaleInterpSpeed = 12f; //how quickly the button scales

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Button States")]
    public bool isHovering = false;
    public bool isClicking = false;
    public bool isInteractable = true;

    [Header("Rotation Effects")]
    [SerializeField] private bool useRotationEffects = true;
    [SerializeField] private float randomRotationAmount = 3f; //small random tilt amount
    [SerializeField] private float rotationInterpSpeed = 12f; //how quicklty the button rotates

    private Quaternion originalRotation;
    private Quaternion targetRotation;


    private Vector3 originalScale;
    private Vector3 targetScale;
    

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        originalRotation = transform.localRotation;
        targetRotation = originalRotation;

        if(audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (useScaleEffects)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleInterpSpeed * Time.unscaledDeltaTime);

        if(useRotationEffects)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationInterpSpeed * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable) return;

        isHovering = true;

        if(!isClicking)
            targetScale = originalScale * hoverScale;

        if (useRotationEffects)
        {
            float randomZ = Random.Range(-randomRotationAmount, randomRotationAmount);
            targetRotation = originalRotation * Quaternion.Euler(0f, 0f, randomZ);
        }


    
        PlaySound(hoverSound);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!isInteractable) return;
        
        isClicking = true;

        targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isInteractable) return;

        isClicking = false;

        if (isHovering)
            targetScale = originalScale * hoverScale;
        else
            targetScale = originalScale;

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;

        Debug.Log("UI Button Clicked: " + gameObject.name);

        PlaySound(clickSound);
        buttonEvent?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!isInteractable) return;

        isHovering = false;
        isClicking = false;

        targetScale = originalScale;
        targetRotation = originalRotation;
    }

    public void PlaySound(AudioClip cliptoPlay)
    {
        if(audioSource == null) return;
        if(cliptoPlay == null) return; 

        audioSource.PlayOneShot(cliptoPlay);
    }

    public void SetInteractable(bool newValue)
    {
        isInteractable = newValue;

        if (!isInteractable)
        {
            isHovering = false;
            isClicking = false;

            targetScale = originalScale;
            targetRotation = originalRotation;
        }
    }
}
