using UnityEngine;

[System.Serializable]
public class ScareAnimationModule
{
    [Header("References")]
    [SerializeField] private Animator animator;

    private bool originalApplyRootMotion;
    private bool hasRootMotionSnapshot;

    public Animator Animator => animator;

    public bool IsValid => animator != null;

    public void Initialise(Animator fallbackAnimator)
    {
        if (animator == null)
            animator = fallbackAnimator;

        CaptureRootMotionState();
    }

    public void Trigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;
        

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    public void ResetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return;
        

        animator.ResetTrigger(triggerName);
    }

    public void SetBool(string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        animator.SetBool(parameterName, value);
    }

    public void SetFloat(string parameterName, float value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        animator.SetFloat(parameterName, value);
    }

    public void CaptureRootMotionState()
    {
        if (animator == null)
        {
            hasRootMotionSnapshot = false;
            return;
        }

        originalApplyRootMotion = animator.applyRootMotion;
        hasRootMotionSnapshot = true;
    }

    public void EnableRootMotion()
    {
        if (animator == null)
            return;

        CaptureRootMotionState();
        animator.applyRootMotion = true;
    }

    public void DisableRootMotion()
    {
        if (animator == null)
            return;

        CaptureRootMotionState();
        animator.applyRootMotion = false;
    }

    public void SetRootMotion(bool enabled)
    {
        if (animator != null) animator.applyRootMotion = enabled;
    }

    public void RestoreRootMotionState()
    {
        if (animator == null || !hasRootMotionSnapshot)
            return;

        animator.applyRootMotion = originalApplyRootMotion;
        hasRootMotionSnapshot = false;
    }

    public Vector3 GetDeltaPosition()
    {
        return animator != null ? animator.deltaPosition : Vector3.zero;
    }

    public Quaternion GetDeltaRotation()
    {
        return animator != null ? animator.deltaRotation : Quaternion.identity;
    }
}