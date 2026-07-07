using UnityEngine;

public class FallScareAnimationEventHelper : MonoBehaviour
{
    private PlayerFallScareController fallScareController;

    private void Awake()
    {
        fallScareController = GetComponentInParent<PlayerFallScareController>();
    }

    public void CaptureGetUpRootMotionPosition()
    {
        PlayerFallScareController controller = GetComponentInParent<PlayerFallScareController>();

        if (controller == null) return;
        controller.CaptureGetUpRootMotionPosition();
    }

}