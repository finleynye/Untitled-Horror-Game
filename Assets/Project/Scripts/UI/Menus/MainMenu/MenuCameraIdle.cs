using UnityEngine;

public class MenuCameraIdle : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera menuCamera;

    [Header("Idle Rotation")]
    [SerializeField] private float horizontalLookAmount = 2.5f;
    [SerializeField] private float verticalLookAmount = 1.2f;
    [SerializeField] private float horizontalLookSpeed = 0.25f;
    [SerializeField] private float verticalLookSpeed = 0.18f;

    [Header("FOV Breathing")]
    [SerializeField] private bool useFOVBreathing = true;
    [SerializeField] private float fovBreathAmount = 1.5f;
    [SerializeField] private float fovBreathSpeed = 0.35f;

    [Header("Micro Jolt")]
    [SerializeField] private bool useMicroJolt = true;
    [SerializeField] private float joltChance = 0.005f;
    [SerializeField] private float joltAmount = 0.6f;
    [SerializeField] private float joltReturnSpeed = 4f;

    private Quaternion startRotation;
    private float startFOV;

    private Vector3 currentJolt;

    private void Start()
    {
        startRotation = transform.localRotation;

        if (menuCamera == null)
            menuCamera = GetComponent<Camera>();
        
        if (menuCamera != null)
            startFOV = menuCamera.fieldOfView;
    }

    private void Update()
    {
        HandleIdleRotation();
        HandleFOVBreathing();
    }

    private void HandleIdleRotation()
    {
        float horizontalLook = Mathf.Sin(Time.time * horizontalLookSpeed) * horizontalLookAmount;
        float verticalLook = Mathf.Sin(Time.time * verticalLookSpeed) * verticalLookAmount;

        if (useMicroJolt && Random.value < joltChance)
            currentJolt = new Vector3(Random.Range(-joltAmount, joltAmount), Random.Range(-joltAmount, joltAmount), Random.Range(-joltAmount, joltAmount));

        currentJolt = Vector3.Lerp(currentJolt, Vector3.zero, Time.deltaTime * joltReturnSpeed);

        Quaternion idleRotation = Quaternion.Euler(verticalLook + currentJolt.x, horizontalLook + currentJolt.y, currentJolt.z);

        transform.localRotation = startRotation * idleRotation;
    }

    private void HandleFOVBreathing()
    {
        if (!useFOVBreathing)
            return;

        if (menuCamera == null)
            return;

        float fovOffset = Mathf.Sin(Time.time * fovBreathSpeed) * fovBreathAmount;

        menuCamera.fieldOfView = startFOV + fovOffset;
    }
}