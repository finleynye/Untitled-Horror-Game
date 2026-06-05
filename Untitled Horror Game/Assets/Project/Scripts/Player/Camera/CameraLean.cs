using UnityEngine;

public class CameraLean : MonoBehaviour
{
    [SerializeField]private PlayerMovement playerMovement;

    [SerializeField] private bool useCameraLean = true;

    [SerializeField] private float sideLeanAmount = 4f; //angle of the lean
    [SerializeField] private float forwardLeanAmount = 2f;
    [SerializeField] private float backwardLeanAmount = 1.5f;

    [SerializeField] private float leanSpeed = 8f;
    [SerializeField] private float returnSpeed = 10f;

    [SerializeField] private bool strongerSprintLean = true;
    [SerializeField] private float sprintLeanMultiplier = 1.35f;

    [SerializeField] private bool reduceLeanWhenCrouching = true;
    [SerializeField] private float crouchLeanMultiplier = 0.5f;

    private Quaternion startLocalRotation;
    void Start()
    {
        startLocalRotation = transform.localRotation;

        if(playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if(playerMovement == null) return;

        if(!playerMovement.isOwned) return;

        if(playerMovement.isPaused || playerMovement.isFrozen)
        {
            ReturnToStartRotation();
            return;
        }

        if(useCameraLean == false)
        {
            ReturnToStartRotation();
            return;
        }

        HandleCameraLeaning();
    }

    private void HandleCameraLeaning()
    {
        Vector2 moveInput = playerMovement._moveInput;

        float leanMultplier = 1f;

        if(strongerSprintLean && playerMovement._isSprinting) 
            leanMultplier *= sprintLeanMultiplier;

        if(reduceLeanWhenCrouching && playerMovement.IsCrouching)
            leanMultplier *= crouchLeanMultiplier;

        //left / right lean angle which rolls the camera
        float targetCameraRoll = -moveInput.x * sideLeanAmount * leanMultplier;

        //forward / backward pitches the camera slightly
        float targetPitch = 0f;

        if (moveInput.y > 0.1f)
            targetPitch = -moveInput.y * forwardLeanAmount * leanMultplier;

        else if (moveInput.y < 0.1f)
            targetPitch = -moveInput.y *backwardLeanAmount * leanMultplier;

        Quaternion targetRotation = startLocalRotation * Quaternion.Euler(targetPitch, 0, targetCameraRoll);

        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, leanSpeed * Time.deltaTime);

    }

    private void ReturnToStartRotation()
    {
        transform.localRotation = Quaternion.Lerp(transform.localRotation, startLocalRotation, returnSpeed * Time.deltaTime);
    }
}
