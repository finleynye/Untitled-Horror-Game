using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[Serializable]
public class JumpscareArmIKModule
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private TwoBoneIKConstraint leftArm;
    [SerializeField] private TwoBoneIKConstraint rightArm;

    [Header("Settings")]
    [SerializeField] private string armsLayerName = "Arms";
    [SerializeField, Min(0f)] private float resetDuration = 0.25f;

    private IKPose[] poses;
    private int armsLayerIndex = -1;

    public void Initialise(Animator fallbackAnimator)
    {
        if (animator == null)
            animator = fallbackAnimator;

        armsLayerIndex = animator != null ? animator.GetLayerIndex(armsLayerName) : -1;

        poses = new[]
        {
            new IKPose(leftArm != null ? leftArm.data.target : null),
            new IKPose(leftArm != null ? leftArm.data.hint : null),
            new IKPose(rightArm != null ? rightArm.data.target : null),
            new IKPose(rightArm != null ? rightArm.data.hint : null)
        };

        ResetImmediately();
    }

    public void Activate()
    {
        SetWeight(1f);
    }

    public void SetWeight(float weight)
    {
        leftArm.weight = weight;
        rightArm.weight = weight;

        if (animator != null && armsLayerIndex >= 0)
            animator.SetLayerWeight(armsLayerIndex, weight);
    }

    public IEnumerator ResetRoutine(Action onComplete = null)
    {
        if (poses == null)
        {
            SetWeight(0f);
            onComplete?.Invoke();
            yield break;
        }

        foreach (IKPose pose in poses) pose.CaptureCurrent();

        float elapsed = 0f;

        while (elapsed < resetDuration)
        {
            elapsed += Time.deltaTime;

            float t = resetDuration > 0f
                ? Mathf.Clamp01(elapsed / resetDuration)
                : 1f;

            //smoothstep prevents the IK targets stopping suddenlty
            t = t * t * (3f - 2f * t);

            foreach (IKPose pose in poses)
                pose.LerpToStart(t);

            yield return null;
        }

        RestorePoses();
        SetWeight(0f);
        onComplete?.Invoke();
    }

    public void ResetImmediately()
    {
        RestorePoses();
        SetWeight(0f);
    }

    private void RestorePoses()
    {
        if (poses == null)
            return;

        foreach (IKPose pose in poses) pose.Restore();
    }

    private sealed class IKPose
    {
        private readonly Transform target;
        private readonly Vector3 startPosition;
        private readonly Quaternion startRotation;

        private Vector3 currentPosition;
        private Quaternion currentRotation;

        public IKPose(Transform target)
        {
            this.target = target;

            if (target == null)
                return;

            startPosition = target.localPosition;
            startRotation = target.localRotation;
        }

        public void CaptureCurrent()
        {
            if (target == null)
                return;

            currentPosition = target.localPosition;
            currentRotation = target.localRotation;
        }

        public void LerpToStart(float t)
        {
            if (target == null)
                return;

            target.localPosition = Vector3.Lerp(currentPosition, startPosition, t);
            target.localRotation = Quaternion.Slerp(currentRotation, startRotation, t);
        }

        public void Restore()
        {
            if (target == null)
                return;

            target.SetLocalPositionAndRotation(startPosition, startRotation);
        }
    }
}