using System;
using UnityEngine;

[Serializable]
public class JumpscareVictimModule
{
    [SerializeField] private Transform victimGrabPoint;

    private JumpscareTarget target;
    private Transform victim;
    private Transform originalParent;
    private CharacterController characterController;
    private bool controllerWasEnabled;
    private PlayerLookIK lookIK;
    private PlayerScareController scareController;
    private bool parentedToGrabPoint;

    public bool HasVictim => victim != null;
    public PlayerScareController ScareController => scareController;

    public bool Grab(GameObject victimObject)
    {
        if (victimObject == null)
            return false;

        return Grab(victimObject.GetComponent<JumpscareTarget>());
    }

    public bool Grab(JumpscareTarget jumpscareTarget, Vector3 localPosition = default, Quaternion localRotation = default)
    {
        if (jumpscareTarget == null || HasVictim)
            return false;

        if (localRotation == default)
            localRotation = Quaternion.identity;

        target = jumpscareTarget;
        scareController = target.ScareController;
        victim = target.PlayerRoot;

        if (victim == null)
        {
            ClearReferences();
            return false;
        }

        originalParent = victim.parent;
        characterController = victim.GetComponent<CharacterController>() ?? victim.GetComponentInChildren<CharacterController>();

        if (characterController != null)
        {
            controllerWasEnabled = characterController.enabled;
            characterController.enabled = false;
        }

        lookIK = victim.GetComponentInChildren<PlayerLookIK>();

        if (victimGrabPoint != null)
        {
            parentedToGrabPoint = true;
            victim.SetParent(victimGrabPoint, false);
            victim.SetLocalPositionAndRotation(localPosition, localRotation);
        }
        else
            parentedToGrabPoint = false;
        

        Physics.SyncTransforms();
        return true;
    }

    public void SetJumpscareLook(bool active)
    {
        lookIK?.SetJumpscareLook(active);
    }

    public void Release(bool activateRagdoll)
    {
        if (victim == null)
        {
            ClearReferences();
            return;
        }

        JumpscareTarget releasedTarget = target;
        Transform releasedVictim = victim;

        if (parentedToGrabPoint)
        {
            Vector3 worldPosition = releasedVictim.position;
            Quaternion worldRotation = releasedVictim.rotation;

            releasedVictim.SetParent(originalParent, true);
            releasedVictim.SetPositionAndRotation(worldPosition, worldRotation);
        }

        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;

        ClearReferences();

        if (activateRagdoll && releasedTarget != null)
            releasedTarget.ActivateJumpscareRagdoll();
    }

    public void Cleanup()
    {
        SetJumpscareLook(false);
        Release(activateRagdoll: false);
    }

    private void ClearReferences()
    {
        target = null;
        victim = null;
        originalParent = null;
        characterController = null;
        controllerWasEnabled = false;
        scareController = null;
        lookIK = null;
        parentedToGrabPoint = false;
    }
}
