using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    private const float Gravity = -18f;

    [Header("Refs")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerStamina playerStamina;
    private CharacterController _controller;
    private PlayerInput _playerInput;

    [Header("Visuals")]
    [SerializeField] private GameObject characterRenderer; //visible character mesh
    [SerializeField] private GameObject firstPersonView; //camera holder / view stuff

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

    //networked animation parameters for the states
    [SyncVar] private float _networkMoveX;
    [SyncVar] private float _networkMoveY;
    [SyncVar] private bool _networkIsGrounded;
    [SyncVar] private bool _networkIsMoving;

    private Vector3 _velocity;
    private float _verticalRotation;
    [HideInInspector] public Vector2 _moveInput;
    [HideInInspector] public Vector2 lastMoveDirection;
    private Vector2 _lookInput;
    private float _coyoteTimer;

    public bool isPaused;
    public bool isFrozen;
    public bool IsCrouching => _isCrouching;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        //disable player camera/view objects while in lobby
        //do not disable the actual character mesh
        if (firstPersonView != null)
            firstPersonView.SetActive(false);

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

        _playerInput.Enable();
        ClientSetupAfterSceneLoad();
    }
    
    public override void OnStartLocalPlayer()
        => SceneManager.sceneLoaded += OnSceneLoaded;
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClientSetupAfterSceneLoad();
    }

    private void Update()
    {
        ApplySceneVisualState();

        if (SceneManager.GetActiveScene().name == "Lobby")
            return;

        //owner controls movement/input
        if (isOwned)
        {
            if (_playerInput == null)
                return;

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
        else
        {
            //remote clients only display synced animation data
            HandleRemoteAnimation();
        }
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

            if (footstepSoundSystem != null)
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
    private void HandleRemoteAnimation()
    {
        if (animator == null)
            return;

        float animSmoothTime = _networkIsGrounded ? 0.06f : 0.12f;

        animator.SetFloat("MoveX", _networkMoveX, animSmoothTime, Time.deltaTime);
        animator.SetFloat("MoveY", _networkMoveY, animSmoothTime, Time.deltaTime);
        animator.SetBool("IsCrouching", _isCrouching);
        animator.SetBool("IsSprinting", _isSprinting && _networkIsMoving && !_isCrouching);
        animator.SetBool("IsGrounded", _networkIsGrounded);
    }
    private void HandleMovementAnimation()
    {
        if (animator == null)
            return;

        Vector2 currentInput = _moveInput;

        if (currentInput.magnitude > 1)
            currentInput.Normalize();

        //stop tiny input drift from affecting the blend tree
        if (currentInput.magnitude < 0.1f)
            currentInput = Vector2.zero;

        bool isMoving = currentInput.magnitude > 0.1f;
        bool isRunning = _isSprinting && isMoving && !_isCrouching;

        if (isMoving)
            lastMoveDirection = currentInput;

        float movementAnimStrength = 0f;

        if (isMoving)
        {
            if (_isCrouching)
                movementAnimStrength = 1f;
            else
                movementAnimStrength = isRunning ? 1f : 0.5f;
        }

        Vector2 animDirection = lastMoveDirection * movementAnimStrength;

        float animSmoothTime = _controller.isGrounded ? 0.06f : 0.12f; //lower dampen for jump animation because of weird transition timings?

        animator.SetFloat("MoveX", animDirection.x, animSmoothTime, Time.deltaTime);
        animator.SetFloat("MoveY", animDirection.y, animSmoothTime, Time.deltaTime);
        animator.SetBool("IsCrouching", _isCrouching);
        animator.SetBool("IsSprinting", isRunning);
        animator.SetBool("IsGrounded", _controller.isGrounded);

        //send values to server so other clients can see them
        CmdSetAnimationValues(animDirection.x, animDirection.y, _controller.isGrounded, isMoving);
    }
    private void ApplySceneVisualState()
    {
        bool inLobby = SceneManager.GetActiveScene().name == "Lobby";

        //player mesh should stay active in the actual game
        if (characterRenderer != null)
            characterRenderer.SetActive(!inLobby);

        //only the owning player should ever have the first-person camera active
        /*if (firstPersonView != null)
            firstPersonView.SetActive(isOwned && !inLobby);*/

        if (cameraHolder != null)
            cameraHolder.gameObject.SetActive(isOwned && !inLobby);

        if (isOwned)
        {
            Cursor.lockState = inLobby ? CursorLockMode.None : CursorLockMode.Locked;
            /*DiscordManager.Instance?.Presence.SetPresence("Waiting in the hub");*/
            Cursor.visible = inLobby;
        }
    }

    //network commands (stop speed cheats & let others see crouching effect)
    [Command]
    private void CmdSetAnimationValues(float moveX, float moveY, bool isGrounded, bool isMoving)
    {
        _networkMoveX = moveX;
        _networkMoveY = moveY;
        _networkIsGrounded = isGrounded;
        _networkIsMoving = isMoving;
    }

    [Command]
    private void CmdSetSprint(bool value)
        => _isSprinting = value;

    [Command]
    private void CmdSetCrouch(bool value)
    {
        _isCrouching = value;

        if (_isCrouching)
            _isSprinting = false;
    }

    private void OnCrouchChanged(bool oldValue, bool newValue)
    {
        if (isPaused)
            return;

        if (_controller != null)
        {
            _controller.height = newValue ? 1.2f : 2f;
            _controller.center = newValue ? new Vector3(0f, -0.4f, 0f) : Vector3.zero;
        }
    }
    public void ClientSetupAfterSceneLoad()
    {
        if (!isOwned)
            return;

        ApplySceneVisualState();
    }

    //player will spawn into the hub with an offset, so that all players dont spawn inside each other, causing them to glitch around.
    public void ClientSetHubPosition()
    {
        if (!isOwned)
            return;

        if (_controller == null)
            return;
        
        LocalPlayerSpawner.SpawnAtScenePoint(transform, _controller, name);
        //LoadingScreen.Instance?.Hide();
    }
    public override void OnStopLocalPlayer()
        => SceneManager.sceneLoaded -= OnSceneLoaded;
    public override void OnStopClient()
    {
        if (!isOwned) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isPaused = false;
    }

    public override void OnStopAuthority()
        => _playerInput?.Disable();
}
