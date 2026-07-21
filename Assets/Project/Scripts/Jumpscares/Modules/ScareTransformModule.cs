using UnityEngine;

[System.Serializable]
public class ScareTransformModule
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform animatorRoot;
    [SerializeField] private Transform modelRoot;

    [Header("Restoration")]
    [SerializeField] private bool resetModelRootAfterScare = true;

    private TransformSnapshot playerRootSnapshot;
    private TransformSnapshot animatorSnapshot;
    private TransformSnapshot modelRootSnapshot;

    private struct TransformSnapshot
    {
        public Transform target;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 worldPosition;
        public Quaternion worldRotation;

        public bool IsValid => target != null;

        public TransformSnapshot(Transform transform)
        {
            target = transform;

            if (transform == null)
            {
                localPosition = Vector3.zero;
                localRotation = Quaternion.identity;
                worldPosition = Vector3.zero;
                worldRotation = Quaternion.identity;
                return;
            }

            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            worldPosition = transform.position;
            worldRotation = transform.rotation;
        }

        public void RestoreLocal()
        {
            if (target == null)
                return;

            target.localPosition = localPosition;
            target.localRotation = localRotation;
        }
    }

    public void Initialise(CharacterController fallbackController, Transform fallbackPlayerRoot, Transform fallbackAnimatorRoot, Transform fallbackModelRoot)
    {
        if (characterController == null)
            characterController = fallbackController;

        if (playerRoot == null)
            playerRoot = fallbackPlayerRoot;

        if (animatorRoot == null)
            animatorRoot = fallbackAnimatorRoot;

        if (modelRoot == null)
            modelRoot = fallbackModelRoot;
    }

    public void Capture()
    {
        playerRootSnapshot = new TransformSnapshot(playerRoot);

        animatorSnapshot = new TransformSnapshot(animatorRoot);

        modelRootSnapshot = new TransformSnapshot(modelRoot);
    }

    public void RestoreAfterScare(bool useOverridePosition, Vector3 overridePosition, Quaternion overrideRotation)
    {
        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        RestorePlayerRoot(useOverridePosition, overridePosition, overrideRotation);

        if (resetModelRootAfterScare)
            RestoreLocalIfDistinct(modelRootSnapshot, playerRoot);
        

        RestoreLocalIfDistinct(animatorSnapshot, playerRoot, modelRoot);

        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;
    }

    public void RestoreRemoteVisualOffsets()
    {
        if (resetModelRootAfterScare)
            RestoreLocalIfDistinct(modelRootSnapshot, playerRoot);
        
        RestoreLocalIfDistinct(animatorSnapshot, playerRoot, modelRoot);

        Physics.SyncTransforms();
    }

    public void AlignRoot(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (playerRoot == null)
            return;

        bool controllerWasEnabled = characterController != null && characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        playerRoot.SetPositionAndRotation( worldPosition, worldRotation);

        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = controllerWasEnabled;
    }

    private void RestorePlayerRoot(bool useOverridePosition, Vector3 overridePosition, Quaternion overrideRotation)
    {
        if (playerRoot == null)
            return;

        if (useOverridePosition)
        {
            playerRoot.SetPositionAndRotation(overridePosition, overrideRotation);

            return;
        }

        if (playerRootSnapshot.IsValid)
            playerRoot.rotation = playerRootSnapshot.worldRotation;
    }

    private static void RestoreLocalIfDistinct(TransformSnapshot snapshot, params Transform[] excludedTransforms)
    {
        if (!snapshot.IsValid)
            return;

        foreach (Transform excludedTransform in excludedTransforms)
        {
            if (snapshot.target == excludedTransform)
                return;
        }

        snapshot.RestoreLocal();
    }
}