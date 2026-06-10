using UnityEngine;

public class LanternSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [SerializeField] private float swayAmount = 8f;
    [SerializeField] private float swaySpeed = 1.5f;
    [SerializeField] private bool randomiseStartOffset = true;

    private Quaternion startRotation;
    private float startOffset;

    private void Start()
    {
        startRotation = transform.localRotation;

        if (randomiseStartOffset)
            startOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float sway = Mathf.Sin((Time.time + startOffset) * swaySpeed) * swayAmount;

        transform.localRotation = startRotation * Quaternion.Euler(sway, 0f, 0f);
    }
}