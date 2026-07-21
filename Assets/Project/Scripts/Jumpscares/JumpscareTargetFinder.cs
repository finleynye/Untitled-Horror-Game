using System;
using UnityEngine;

[Serializable]
public class JumpscareTargetFinder
{
    [SerializeField] private string survivorTag = "Survivor";
    [SerializeField, Min(0f)] private float range = 2.5f;

    [Tooltip("How directly the killer must face the survivor")]
    [SerializeField, Range(-1f, 1f)]
    private float minimumFacingDot = 0.7f;

    [Tooltip("How closely the killer must be behind the survivor")]
    [SerializeField, Range(-1f, 1f)]
    private float minimumBehindDot = 0.5f;

    public GameObject FindBestTarget(Transform killer)
    {
        if (killer == null)
            return null;

        GameObject bestTarget = null;
        float bestFacingDot = minimumFacingDot;

        foreach (GameObject survivor in GameObject.FindGameObjectsWithTag(survivorTag))
        {
            if (!survivor.activeInHierarchy)
                continue;

            if (!TryEvaluateTarget(killer, survivor, out float facingDot))
                continue;

            if (facingDot < bestFacingDot)
                continue;

            bestFacingDot = facingDot;
            bestTarget = survivor;
        }

        return bestTarget;
    }

    private bool TryEvaluateTarget(Transform killer, GameObject survivor, out float killerFacingDot)
    {
        killerFacingDot = -1f;

        Vector3 direction = Flatten(survivor.transform.position - killer.position);

        if (direction.sqrMagnitude <= Mathf.Epsilon || direction.sqrMagnitude > range * range)
            return false;
        

        direction.Normalize();

        Vector3 killerForward = Flatten(killer.forward);

        if (killerForward.sqrMagnitude <= Mathf.Epsilon)
            return false;

        killerForward.Normalize();

        JumpscareTarget target = survivor.GetComponent<JumpscareTarget>();

        Transform facingTransform = target != null ? target.FacingTransform : survivor.transform;

        Vector3 survivorForward = Flatten(facingTransform.forward);

        if (survivorForward.sqrMagnitude <= Mathf.Epsilon)
            return false;

        survivorForward.Normalize();

        killerFacingDot = Vector3.Dot(killerForward, direction);
        float behindDot = Vector3.Dot(survivorForward, direction);

        return killerFacingDot >= minimumFacingDot &&
               behindDot >= minimumBehindDot;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}