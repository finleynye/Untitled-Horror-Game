using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Head Bob Settings")]
    [SerializeField] private bool useHeadBob = true;
    [SerializeField] private float walkBobSpeed = 5f;
    [SerializeField] private float walkBobAmount = 0.045f;

    [Header("Sprint Bob Settings")]
    [SerializeField] private bool useSprintBob = true;
    [SerializeField] private float sprintBobSpeed = 7f;
    [SerializeField] private float sprintBobAmount = 0.08f;

    [Header("Crouch Bob Settings")]
    [SerializeField] private bool useCrouchBob = true;
    [SerializeField] private float crouchBobSpeed = 4f;
    [SerializeField] private float crouchBobAmount = .035f;

    [Header("Return Settings")]
    [SerializeField] private float returnSpeed = 8f;

    [Header("State")]
    [SerializeField] private bool isSprinting;

    private Vector3 startLocalPosition;
    private Vector3 lastPosition;
    private float bobTimer;

    private void Start()
    {
        startLocalPosition = transform.localPosition;

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
        

        lastPosition = playerMovement.transform.position;
    }
    private void Update()
    {
        if (useHeadBob == false)
        {
            ReturnToStartPosition();
            return;
        }

        if (characterController == null)
            return;

        if (!playerMovement.isOwned)
            return;

        HandleHeadBob();
    }
    private void HandleHeadBob()
    {
        Vector3 currentPosition = playerMovement.transform.position;

        Vector3 horizontalMovement = currentPosition - lastPosition;
        horizontalMovement.y = 0f;

        float movementAmount = horizontalMovement.magnitude;
        lastPosition = currentPosition;

        bool isMoving = playerMovement._moveInput.magnitude > 0.1f;
        bool isMovingForward = playerMovement._moveInput.y > 0.1f;
        bool isGrounded = characterController.isGrounded;
        bool isSprinting = playerMovement._isSprinting;
        bool isCrouching = playerMovement.IsCrouching;


        if (isMoving && isGrounded)
        {
            float currentBobSpeed = walkBobSpeed; //default for bobbing is walking
            float currentBobAmount = walkBobAmount;

            if(isCrouching && useCrouchBob)
            {
                currentBobAmount = crouchBobAmount;
                currentBobSpeed = crouchBobSpeed;
            }
            
            else if (isSprinting && useSprintBob && isMovingForward)
            {
                currentBobSpeed = sprintBobSpeed;
                currentBobAmount = sprintBobAmount;
            }

            bobTimer += Time.deltaTime * currentBobSpeed;

            float bobX = Mathf.Cos(bobTimer * 0.5f) * currentBobAmount * 0.5f; //use cos wave to add small side to side movement
            float bobY = Mathf.Sin(bobTimer) * currentBobAmount; //use sin wave to  add small up and down movement

            Vector3 targetPosition = startLocalPosition + new Vector3(bobX, bobY, 0f);

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, returnSpeed * Time.deltaTime);
        }
        else
            ReturnToStartPosition();
        

    }

    private void ReturnToStartPosition()
    {
        bobTimer = 0f;

        transform.localPosition = Vector3.Lerp(transform.localPosition, startLocalPosition, returnSpeed * Time.deltaTime);
    }
}
