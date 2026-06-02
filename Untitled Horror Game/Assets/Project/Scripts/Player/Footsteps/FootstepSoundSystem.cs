using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class FootstepSoundSystem : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float walkStepDistance = 0.8f;
    [SerializeField] private float sprintStepDistance = .55f;
    [SerializeField] private float crouchStepDistance = 1.2f;

    [SerializeField] bool isSprinting;
    [SerializeField] bool isCrouching;

    public UnityEvent onFootstep;

    private Vector3 lastPosition;
    private float distanceTravelled;

    void Start()
    {
        if(characterController == null)
            characterController = GetComponent<CharacterController>();

        lastPosition = transform.position;
    }
    private void Update()
    {
        HandleFoosteps();
    }
    private void HandleFoosteps()
    {
        if (characterController == null) return;

        //only play footsteps whilst on the ground
        if(characterController.isGrounded == false)
        {
            lastPosition = transform.position;
            return;
        }

        //only count horizontal movements
        Vector3 currentPosition = transform.position;
        Vector3 horizontalMovement = currentPosition - lastPosition;
        horizontalMovement.y = 0;

        float movementAmount = horizontalMovement.magnitude;

        //if the player is barely moving do nothing
        if (movementAmount <= 0.001f)
        {
            lastPosition = currentPosition;
            return;
        }

        distanceTravelled += movementAmount;

        float currentStepDistance = GetCurrentStepDistance(); //determines whether we are walking, sprinting or crouching for how often step sound plays

        if(distanceTravelled >= currentStepDistance)
        {
            distanceTravelled = 0f;

            onFootstep?.Invoke();
        }

        lastPosition = currentPosition;
    }

    private float GetCurrentStepDistance()
    {
        if(isCrouching == true)
            return crouchStepDistance;
        

        if(isSprinting == true)
            return sprintStepDistance;
        

        return walkStepDistance;
    }

    public void SetSprinting(bool value) => isSprinting = value;

    public void SetCrouching(bool value) => isCrouching = value;
}
