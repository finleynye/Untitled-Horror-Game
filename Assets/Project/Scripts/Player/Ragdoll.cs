using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    private Rigidbody[] rigidbodies;
    private Animator animator;

    [SerializeField] private CapsuleCollider capsuleCollider;

    private void Awake()
    {
        ResolveReferences();
        DeactivateRagdoll();
    }

    private void ResolveReferences()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (capsuleCollider == null)
            capsuleCollider = GetComponentInParent<CapsuleCollider>();

        if (capsuleCollider == null)
            capsuleCollider = GetComponentInParent<CapsuleCollider>();
    }

    public void DeactivateRagdoll()
    {
        ResolveReferences();

        foreach (Rigidbody rigidBody in rigidbodies)
            rigidBody.isKinematic = true;

        if (animator != null)
            animator.enabled = true;

        if (capsuleCollider != null)
            capsuleCollider.enabled = true;
    }

    public void ActivateRagdoll()
    {
        ResolveReferences();

        foreach (Rigidbody rigidBody in rigidbodies)
            rigidBody.isKinematic = false;

        if (animator != null)
            animator.enabled = false;

        if (capsuleCollider != null)
            capsuleCollider.enabled = false;
    }
}