using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Head Bob Settings")]
    [SerializeField] private bool useHeadBob = true;
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.04f;

    [Header("Sprint Bob Settings")]
    [SerializeField] private bool useSprintBob = true;
    [SerializeField] private float sprintBobSpeed = 12f;
    [SerializeField] private float sprintBobAmount = 0.07f;

    [Header("Return Settings")]
    [SerializeField] private float returnSpeed = 8f;

    [Header("State")]
    [SerializeField] private bool isSprinting;

    private Vector3 startLocalPosition;
    private float bobTimer;

    private void Start()
    {
        startLocalPosition = transform.localPosition;

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
        

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
        

        HandleHeadBob();
    }
    private void HandleHeadBob()
    {
        bool isMoving = playerMovement._moveInput.magnitude > 0.1f;
        bool isSprinting = playerMovement._isSprinting;
        bool isCrouching = playerMovement.IsCrouching;

        if (isMoving && isCrouching == false)
        {
            float currentBobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed; //sprint uses a faster bob, walk uses slower bob
            float currentBobAmount = isSprinting ? sprintBobAmount : walkBobAmount;//sprint uses a stronger bob, walk uses weaker bob

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

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }
}
