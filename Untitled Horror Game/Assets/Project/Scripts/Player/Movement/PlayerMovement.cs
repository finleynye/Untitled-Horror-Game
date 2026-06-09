using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private const float Gravity = -18f;
    
    [Header("Refs")]
    [SerializeField] private GameObject playerModel;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform nametag;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerStamina playerStamina;
    private CharacterController _controller;
    private PlayerInput _playerInput;


    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime;
    
    [Header("Movement Audio")]
    [SerializeField] private FootstepSoundSystem footstepSoundSystem;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [SyncVar(hook = nameof(OnCrouchChanged))] private bool _isCrouching;
    [SyncVar] public bool _isSprinting;
    
    private Vector3 _velocity;
    private float _verticalRotation;
    [HideInInspector]public Vector2 _moveInput;
    [HideInInspector]public Vector2 lastMoveDirection;
    private Vector2 _lookInput;
    private float _coyoteTimer;

    public bool isPaused;
    public bool isFrozen;
    public bool IsCrouching => _isCrouching;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        playerModel.SetActive(false);

        if (playerInteraction == null)
            playerInteraction = GetComponent<PlayerInteraction>();

        if (footstepSoundSystem == null)
            footstepSoundSystem = GetComponentInChildren<FootstepSoundSystem>();

        if (playerStamina == null)
            playerStamina = GetComponent<PlayerStamina>();

    }
    public override void OnStartAuthority()
    {
        if (!isOwned) return;
        
        _playerInput = new PlayerInput();

        _playerInput.Player.Jump.performed += _ => Jump();
        _playerInput.Player.Sprint.started += _ => TryStartSprint();
        _playerInput.Player.Sprint.canceled += _ => StopSprint();
        _playerInput.Player.Crouch.started += _ => CmdSetCrouch(true);
        _playerInput.Player.Crouch.canceled += _ => CmdSetCrouch(false);

        cameraHolder.gameObject.SetActive(true);
        _playerInput.Enable();
    }
    public override void OnStartLocalPlayer()
        => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Game") return;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "Lobby")
        {
            if (playerModel.activeSelf == false)
            {
                if (isOwned)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    /*DiscordManager.Instance?.Presence.SetPresence("Waiting in the hub");*/
                }
                playerModel.SetActive(true);
            }
        }
    
        if (!isOwned) return;
        if (SceneManager.GetActiveScene().name == "Lobby") return;

        if (isFrozen)
        {
            _velocity = Vector3.zero; 
            return;
        }
        HandleMovement();
        HandleInteraction();
        HandleStamina();
        HandleMovementAnimation();
    }
    private void HandleMovement()
    {
        _moveInput = isPaused ? Vector2.zero : _playerInput.Player.Move.ReadValue<Vector2>();

        if (_controller.isGrounded)
        {
            _coyoteTimer = coyoteTime;
            if (_velocity.y < 0)
                _velocity.y = -2f;
        }
        else
            _coyoteTimer -= Time.deltaTime;
        
        var currentSpeed = walkSpeed;

        bool isMoving = _moveInput.magnitude > 0.1f;

        if (_isSprinting && isMoving)
            currentSpeed = sprintSpeed;

        if (_isCrouching) 
            currentSpeed = crouchSpeed;

        var moveDir = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        _controller.Move(moveDir * (currentSpeed * Time.deltaTime));

        _velocity.y += Gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
    private void HandleStamina()
    {
        if (playerStamina == null)
            return;

        //checks if the player is moving in any direction
        bool isMoving = _moveInput.magnitude > 0.1f;

        //drain stamina if sprinting and moving
        bool isDrainingStamina = _isSprinting && isMoving;

        bool becameExhausted = playerStamina.TickStamina(isDrainingStamina);

        if (becameExhausted)
            StopSprint();
    }
    private void HandleInteraction()
    {
        if (playerInteraction == null)
            return;

        if (_playerInput == null)
            return;

        if (_playerInput.Player.Interact.WasPressedThisFrame())
            playerInteraction.TryInteract();
    }
    private void Jump()
    {
        if (_controller == null) return; //will throw errors without this, but still works regardless???
        if (isFrozen) return;

        if (_coyoteTimer > 0f && !_isCrouching)
        {
            _velocity.y = Mathf.Sqrt(jumpForce * -2f * Gravity);
            _coyoteTimer = 0f;
            footstepSoundSystem.PlayJumpSound();
        }
    }
    private void TryStartSprint()
    {
        if (playerStamina != null && !playerStamina.CanUseStamina)
            return;

        CmdSetSprint(true);
    }
    private void StopSprint()
    {
        CmdSetSprint(false);
    }
    private void HandleMovementAnimation()
    {
        if (animator == null)
            return;

        Vector2 currentInput = _moveInput;

        if (currentInput.magnitude > 1)
            currentInput.Normalize();

        bool isMoving = currentInput.magnitude > 0.1f;
        bool isRunning = _isSprinting && isMoving;

        if (isMoving)
            lastMoveDirection = currentInput;

        float movementAnimStrength = 0f;

        if (isMoving)
            movementAnimStrength = isRunning ? 1f : 0.5f;

        Vector2 animDirection = lastMoveDirection * movementAnimStrength;

        //damped the SetFloat gives the blend tree a smoother transition between anims
        animator.SetFloat("MoveX", animDirection.x, 0.12f, Time.deltaTime); //.12 (smooth ish but still snappy ps1 like)
        animator.SetFloat("MoveY", animDirection.y, 0.12f, Time.deltaTime);
    }

    //network commands (stop speed cheats & let others see crouching effect)
    [Command] 
    private void CmdSetSprint(bool value) 
        => _isSprinting = value;
    
    [Command] 
    private void CmdSetCrouch(bool value) 
        => _isCrouching = value;

    private void OnCrouchChanged(bool oldValue, bool newValue)
    {
        if (isPaused) return;
        
        /*
        var height = newValue ? .6f : 1; //size of crouched player : size of regular player
        playerModel.transform.localScale = new Vector3(1, height, 1);
        
        var yPos = newValue ? -.4f : 0;
        playerModel.transform.localPosition = new Vector3(0, yPos, 0);
        
        _controller.height = newValue ? 1.2f : 2;
        _controller.center = newValue ? new Vector3(0, -.4f, 0) : Vector3.zero;
        
        //handle outer body features otherwise they shrink on crouch (cant be a child of the player)
        //i could just multiply the scale, but thats long
        /*eyesQuad.localPosition = new Vector3(0, newValue ? -.2f  : .5f,  .5f);
        mouthQuad.localPosition = new Vector3(0, newValue ? -.4f : .25f, .5f);
        nametag.localPosition = new Vector3(0, newValue ? .6f : 1.2f,  0);
        hatSlot.localPosition = new Vector3(0, newValue ? 0 : 1.1f,  0);*/
    }
    
    //player will spawn into the hub with an offset, so that all players dont spawn inside each other, causing them to glitch around.
    public void ClientSetHubPosition()
    {
        _controller.enabled = false;
        transform.position = new Vector3(Random.Range(10f, -10f), Random.Range(5f, 1f), Random.Range(10f, -10f));
        _controller.enabled = true;
        //LoadingScreen.Instance?.Hide();
    }
    public override void OnStopLocalPlayer()
        => SceneManager.sceneLoaded -= OnSceneLoaded;
    public override void OnStopClient()
    {
        if(!isOwned) return;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isPaused = false;
    }
    
    public override void OnStopAuthority()
        => _playerInput?.Disable();
}
