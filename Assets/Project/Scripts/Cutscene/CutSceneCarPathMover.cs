using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class CutsceneCarPathMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Path Points")]
    [SerializeField] private Transform[] pathPoints;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float turnSpeed = 4f;
    [SerializeField] private float pointReachDistance = 1f;

    [Header("Crash Settings")]
    [SerializeField] private bool stopOnCrashTrigger = true;
    [SerializeField] private float crashStopDrag = 8f;

    [Header("Engine Audio")]
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private AudioClip engineLoopClip;
    [SerializeField] private float minEnginePitch = 0.75f;
    [SerializeField] private float maxEnginePitch = 1.35f;
    [SerializeField] private float pitchSmoothSpeed = 4f;

    [Header("Events")]
    public UnityEvent OnPathFinished;
    public UnityEvent OnCrashTriggered;

    private int currentPointIndex;
    private bool isMoving = true;
    private bool hasCrashed;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (engineAudioSource != null && engineLoopClip != null)
        {
            engineAudioSource.clip = engineLoopClip;
            engineAudioSource.loop = true;
            engineAudioSource.playOnAwake = false;
            engineAudioSource.pitch = minEnginePitch;
            engineAudioSource.Play();
        }
    }
    private void Update()
    {
        UpdateEngineAudio();
    }
    private void FixedUpdate()
    {
        if (!isMoving) return;
        if (hasCrashed) return;
        if (pathPoints == null || pathPoints.Length == 0) return;

        MoveToCurrentPoint();
    }

    private void MoveToCurrentPoint()
    {
        Transform targetPoint = pathPoints[currentPointIndex];

        if (targetPoint == null) return;

        Vector3 direction = targetPoint.position - rb.position;
        direction.y = 0f;

        if (direction.magnitude <= pointReachDistance)
        {
            GoToNextPoint();
            return;
        }

        Vector3 moveDirection = direction.normalized;

        Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        //Quaternion targetRotation = Quaternion.LookRotation(-moveDirection, Vector3.up);
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, rb.rotation, turnSpeed * Time.fixedDeltaTime);

        rb.MoveRotation(newRotation);
    }

    private void GoToNextPoint()
    {
        currentPointIndex++;

        if (currentPointIndex >= pathPoints.Length)
        {
            StopCar();
            OnPathFinished?.Invoke();
        }
    }

    public void StopCar()
    {
        isMoving = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void TriggerCrash()
    {
        if (hasCrashed) return;

        hasCrashed = true;
        isMoving = false;

        rb.linearDamping = crashStopDrag;
        rb.angularDamping = crashStopDrag;

        rb.linearVelocity *= 0.25f;
        rb.angularVelocity *= 0.25f;

        OnCrashTriggered?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!stopOnCrashTrigger) return;

        if (other.CompareTag("CrashTrigger"))
            TriggerCrash();
        
    }

    private void UpdateEngineAudio()
    {
        if (engineAudioSource == null) return;

        float currentSpeed = rb.linearVelocity.magnitude;

        float speedPercent = Mathf.InverseLerp(0f, moveSpeed, currentSpeed);
        float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, speedPercent);

        engineAudioSource.pitch = Mathf.Lerp(
            engineAudioSource.pitch,
            targetPitch,
            Time.deltaTime * pitchSmoothSpeed
        );
    }
}