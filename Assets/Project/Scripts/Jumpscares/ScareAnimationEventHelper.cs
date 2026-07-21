using UnityEngine;

public class ScareAnimationEventHelper : MonoBehaviour
{
    [Header("Tree Fall Restoration")]
    [SerializeField] private Vector3 getUpRestoreLocalOffset = new Vector3(0f, 0f, -0.5f);

    [SerializeField] private KillerJumpscareDetector jumpscareDetector;

    private TreeFallScareController fallScareController;

    private Vector3 capturedPosition;
    private Quaternion capturedRotation;

    private void Awake()
    {
        fallScareController =
            GetComponentInParent<TreeFallScareController>();
    }

    public void CaptureGetUpRootMotionPosition()
    {
        if (fallScareController == null)
            return;

        Vector3 worldOffset =
            transform.TransformDirection(getUpRestoreLocalOffset);

        capturedPosition =
            transform.position + worldOffset;

        capturedRotation =
            transform.rotation;

        fallScareController.SetGetUpRestorePosition(capturedPosition, capturedRotation);

        Debug.Log($"Captured get-up restore position: {capturedPosition}");
    }

    //call from animation event at end of jumpscare
    public void EndJumpscare()
    {
        if (jumpscareDetector == null)
        {
            Debug.LogWarning("jumpscare detector is not assigned");
            return;
        }

        jumpscareDetector.EndJumpscare();
    }
}