using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject playerModel;
    [SerializeField] private CameraMovement cam;
    [SerializeField] private Transform nametag;
    private CharacterController _controller;
    private InputSystem_Actions _playerInput;

    [Header("Settings")]
    [SerializeField] private float walkSpeed = 5;
    [SerializeField] private float sprintSpeed = 7;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpForce = 5;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float mouseSensitivity = .1f;


    private bool _isCrouching;
    public bool _isSprinting;

    private Vector3 _velocity;
    public Vector2 _moveInput;
    public Vector2 _lookInput;
    private float _coyoteTimer;

    [SerializeField] private float coyoteTime = 0.12f;
    public bool isPaused;
    public bool isFrozen;
    public bool IsCrouching => _isCrouching;

    bool isOwned = false;
    bool canJump = false;
    private void Awake()
    {
        canJump = true;
        _controller = GetComponent<CharacterController>();
    }

    public void Start()
    {
        if (!isOwned) return;

        _playerInput = new InputSystem_Actions();
       
        _playerInput.Player.Jump.performed += _ => Jump();

        _playerInput.Enable();
    }

    private void OnDisable()
        => _playerInput?.Disable();

    private void Update()
    {
        if (isFrozen)
        {
            _velocity = Vector3.zero;
            return;
        }
        Movement();


        CanCrouch(_isCrouching);
    }

    public void HandleMovement(InputAction.CallbackContext context)
    {
        _moveInput = isPaused ? Vector2.zero : context.ReadValue<Vector2>();
    }
    public void Sprint(InputAction.CallbackContext context)
    {
        _isSprinting = context.performed;
    }
    public void Crouch(InputAction.CallbackContext context)
    {
        _isCrouching = context.performed;
    }
    private void Movement()
    {
        if (_controller.isGrounded)
        {
            _coyoteTimer = coyoteTime;
            if (_velocity.y < 0)
                _velocity.y = -2f;
        }
        else
            _coyoteTimer -= Time.deltaTime;

        var currentSpeed = walkSpeed;
        if (_isSprinting)
            currentSpeed = sprintSpeed;
        if (_isCrouching)
            currentSpeed = crouchSpeed;

        var moveDir = cam.transform.right * _moveInput.x + cam.transform.forward * _moveInput.y;
        _controller.Move(moveDir * (currentSpeed * Time.deltaTime));

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
    public void Jump()
    {
        if (_controller == null) return; //will throw errors without this, but still works regardless???
        if (isFrozen) return;


        if (_coyoteTimer > 0f && !_isCrouching && canJump)
        {
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            _coyoteTimer = 0f;
        }
    }
    private void CanCrouch(bool newValue)
    {
        if (isPaused) return;

        var height = newValue ? .6f : 1; //size of crouched player : size of regular player
        playerModel.transform.localScale = new Vector3(1, height, 1);;

        _controller.height = newValue ? 1.2f : 2;
        _controller.center = newValue ? new Vector3(0, -.4f, 0) : Vector3.zero;
    }

}
