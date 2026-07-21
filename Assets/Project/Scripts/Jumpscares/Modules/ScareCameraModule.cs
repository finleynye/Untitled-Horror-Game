using System.Collections;
using UnityEngine;

[System.Serializable]
public class ScareCameraModule
{
    [Header("References")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform headTarget;
    [SerializeField] private CameraMovement cameraMovement;

    [Header("Head Follow")]
    [SerializeField] private float cameraFollowSpeed = 12f;
    [SerializeField] private float cameraRotationSpeed = 10f;
    [SerializeField] private float cameraResetTime = 0.25f;
    [SerializeField] private float cameraPositionSmoothTime = 0.08f;
    [SerializeField] private float cameraRotationSmoothSpeed = 5f;
    [SerializeField] private bool followHeadRotationDuringScare;
    [SerializeField] private Vector3 scareCameraLocalOffset;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Vector3 followVelocity;
    private Quaternion scareStartWorldRotation;

    private bool originalCameraMovementEnabled;
    private bool hasCameraMovementSnapshot;
    private bool isFollowing;

    public bool IsFollowing => isFollowing;

    public void Initialise(Transform fallbackCameraHolder, Transform fallbackHeadTarget, CameraMovement fallbackCameraMovement)
    {
        if (cameraHolder == null)
            cameraHolder = fallbackCameraHolder;

        if (headTarget == null)
            headTarget = fallbackHeadTarget;

        if (cameraMovement == null)
            cameraMovement = fallbackCameraMovement;

        if (cameraHolder != null)
        {
            originalLocalPosition = cameraHolder.localPosition;
            originalLocalRotation = cameraHolder.localRotation;
        }
    }

    public void BeginFollow()
    {
        followVelocity = Vector3.zero;

        if (cameraHolder != null)
            scareStartWorldRotation = cameraHolder.rotation;

        DisableCameraMovement();
        isFollowing = true;
    }

    public void TickFollow()
    {
        if (!isFollowing || cameraHolder == null || headTarget == null)
            return;

        Vector3 targetPosition = headTarget.position + headTarget.TransformDirection(scareCameraLocalOffset);

        float positionSmoothTime = cameraPositionSmoothTime > 0f ? cameraPositionSmoothTime : 1f / Mathf.Max(cameraFollowSpeed, 0.01f);

        cameraHolder.position = Vector3.SmoothDamp(cameraHolder.position, targetPosition, ref followVelocity, positionSmoothTime);

        Quaternion targetRotation = followHeadRotationDuringScare ? headTarget.rotation : scareStartWorldRotation;

        float rotationSpeed = cameraRotationSmoothSpeed > 0f ? cameraRotationSmoothSpeed : cameraRotationSpeed;

        float rotationT = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);

        cameraHolder.rotation = Quaternion.Slerp(cameraHolder.rotation, targetRotation, rotationT);
    }

    public IEnumerator ResetRoutine()
    {
        isFollowing = false;
        followVelocity = Vector3.zero;

        if (cameraHolder == null)
        {
            RestoreCameraMovement();
            yield break;
        }

        Vector3 startPosition = cameraHolder.localPosition;
        Quaternion startRotation = cameraHolder.localRotation;

        if (cameraResetTime <= 0f)
        {
            cameraHolder.localPosition = originalLocalPosition;
            cameraHolder.localRotation = originalLocalRotation;
            RestoreCameraMovement();
            yield break;
        }

        float timer = 0f;

        while (timer < cameraResetTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / cameraResetTime);

            cameraHolder.localPosition = Vector3.Lerp(startPosition, originalLocalPosition, t);

            cameraHolder.localRotation = Quaternion.Slerp(startRotation, originalLocalRotation, t);

            yield return null;
        }

        cameraHolder.localPosition = originalLocalPosition;
        cameraHolder.localRotation = originalLocalRotation;

        RestoreCameraMovement();
    }

    public void StopImmediately()
    {
        isFollowing = false;
        followVelocity = Vector3.zero;

        if (cameraHolder != null)
        {
            cameraHolder.localPosition = originalLocalPosition;
            cameraHolder.localRotation = originalLocalRotation;
        }

        RestoreCameraMovement();
    }

    private void DisableCameraMovement()
    {
        hasCameraMovementSnapshot = false;

        if (cameraMovement == null) return;

        originalCameraMovementEnabled = cameraMovement.enabled;
        hasCameraMovementSnapshot = true;
        cameraMovement.enabled = false;
    }

    private void RestoreCameraMovement()
    {
        if (!hasCameraMovementSnapshot || cameraMovement == null)
            return;
       
        cameraMovement.enabled = originalCameraMovementEnabled;
        hasCameraMovementSnapshot = false;
    }
}