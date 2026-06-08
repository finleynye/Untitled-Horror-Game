using TMPro;
using UnityEngine;

public class UITitleFlicker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Title Rotation Settings")]
    [SerializeField] private float rotationAmount = 2f;
    [SerializeField] private float rotationSpeed = 1.5f;

    [Header("Flicker Settings")]
    [SerializeField] private float flickerSpeed = 0.08f;
    [SerializeField] private float minAlpha = 0.55f;
    [SerializeField] private float maxAlpha = 1f;

    [Header("Flicker Glitch Settings")]
    [SerializeField] private bool useGlitchRotation = true;
    [SerializeField] private float glitchChance = 0.02f;
    [SerializeField] private float glitchRotationAmount = 5f;

    private Color originalColor;
    private float flickerTimer;

    private void Start()
    {
        if (titleText == null)
            titleText = GetComponent<TextMeshProUGUI>();
        
        if (titleText != null)
            originalColor = titleText.color;
    }

    private void Update()
    {
        RotateTitle();
        FlickerTitle();
    }

    private void RotateTitle()
    {
        float zRotation = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount;

        if (useGlitchRotation && Random.value < glitchChance)
            zRotation += Random.Range(-glitchRotationAmount, glitchRotationAmount);
        

        transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    private void FlickerTitle()
    {
        if (titleText == null)
            return;

        flickerTimer -= Time.deltaTime;

        if (flickerTimer <= 0f)
        {
            float randomAlpha = Random.Range(minAlpha, maxAlpha);

            Color newColor = originalColor;
            newColor.a = randomAlpha;

            titleText.color = newColor;

            flickerTimer = flickerSpeed;
        }
    }
}