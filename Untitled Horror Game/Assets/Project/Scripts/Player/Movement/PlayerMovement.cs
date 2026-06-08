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
    private CharacterController _controller;
    private PlayerInput _playerInput;
    
    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime;
    
    [Header("Stamina")]
    [SerializeField] private float maxStamina;
    [SerializeField] private float staminaRegenRate;
    [SerializeField] private float staminaRegenDelay;
    [SerializeField] private float staminaDrainRate;
    [SerializeField] private Slider staminaSlider;
    
    [SyncVar(hook = nameof(OnCrouchChanged))] private bool _isCrouching;
    [SyncVar] public bool _isSprinting;
    
    private float _currentStamina;
    private float _regenDelayTimer;
    private bool _staminaExhausted; //stops sprint when true

    private Vector3 _velocity;
    private float _verticalRotation;
    [HideInInspector]public Vector2 _moveInput;
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

        _currentStamina = maxStamina;
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
        
        staminaSlider = GameObject.Find("Stamina").GetComponent<Slider>();
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = _currentStamina;
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

        HandleStamina();
        HandleMovement();
        HandleInteraction();
    }

    private void HandleStamina()
    {
        var isMovingForward = _moveInput.y > .1f;
        var isDrainingStamina = _isSprinting && isMovingForward;
        
        if (isDrainingStamina) //decrease stamina
        {
            _currentStamina -= staminaDrainRate * Time.deltaTime;
            _regenDelayTimer = staminaRegenDelay;

            if (_currentStamina <= 0f)
            {
                _currentStamina = 0;
                _staminaExhausted = true;
                StopSprint();
            }
        }
        else //increase stamina
        {
            if(_regenDelayTimer > 0f)
                _regenDelayTimer -= Time.deltaTime;
            else
            {
                _currentStamina += staminaRegenRate * Time.deltaTime;
                if (_currentStamina >= maxStamina)
                {
                    _currentStamina = maxStamina;
                    _staminaExhausted = false;
                }
            }
        }
        //if(staminaSlider is not null)
         //   staminaSlider.value = _currentStamina; 
         //harvey sprint script hook here TODO
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
        if (_isSprinting) 
            currentSpeed = sprintSpeed;
        if (_isCrouching) 
            currentSpeed = crouchSpeed;

        var moveDir = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        _controller.Move(moveDir * (currentSpeed * Time.deltaTime));

        _velocity.y += Gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
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
        }
    }


    private void TryStartSprint()
    {
        if(_staminaExhausted) return;
        CmdSetSprint(true);
    }
    
    private void StopSprint()
        => CmdSetSprint(false);
    
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
        hatSlot.localPosition = new Vector3(0, newValue ? 0 : 1.1f,  0);*/
        nametag.localPosition = new Vector3(0, newValue ? .6f : 1.2f,  0);
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
