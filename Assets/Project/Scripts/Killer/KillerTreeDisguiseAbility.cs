using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class KillerTreeDisguiseAbility : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LocalPlayerMeshVisibility localMeshVisibility;

    [Header("Camera Settings")]
    [SerializeField] private float disguisedNearClipPlane = 0.8f;
    [SerializeField] private float normalNearClipPlane = 0.01f;

    [Header("Visuals")]
    [SerializeField] private GameObject killerVisualModel;
    [SerializeField] private GameObject treeDisguiseModel;

    [Header("Audio")]
    [SerializeField] private AudioSource disguiseAudioSource;
    [SerializeField] private AudioClip rustleSound;

    [Header("Disguise Settings")]
    [SerializeField] private float timeNeededToDisguise = 3f;
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Ground Snap Settings")]
    [SerializeField] private float groundRayHeight = 3f;
    [SerializeField] private float groundRayDistance = 8f;
    [SerializeField] private LayerMask floorLayers;

    [Header("Tree Rotation")]
    [SerializeField] private bool faceSameDirectionAsPlayer = true;
    [SerializeField] private Vector3 treeRotationOffset = new Vector3(-90f, 0f, 0f);

    [Header("State")]
    [SerializeField] private float stillTimer;

    [SyncVar(hook = nameof(OnDisguiseChanged))]
    [SerializeField] private bool isDisguised;

    private PlayerInput _playerInput;

    public bool IsDisguised => isDisguised;

    public override void OnStartAuthority()
    {
        if (!isOwned)
            return;

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (localMeshVisibility == null)
            localMeshVisibility = GetComponent<LocalPlayerMeshVisibility>();

        if (disguiseAudioSource == null)
            disguiseAudioSource = GetComponent<AudioSource>();

        _playerInput = new PlayerInput();
         
        _playerInput.Player.KillerExitDisguise.performed += OnToggleDisguisePressed;

        _playerInput.Enable();
    }

    private void Start()
    {
        ApplyDisguiseVisuals();
    }

    private void Update()
    {
        if (!isOwned)
            return;

        if (isDisguised)
            return;

        CheckStillness();
    }

    private void CheckStillness()
    {
        if (playerMovement == null)
            return;

        bool isMoving = playerMovement._moveInput.magnitude > movementThreshold;

        if (isMoving)
        {
            stillTimer = 0f;
            return;
        }

        stillTimer += Time.deltaTime;

        if (stillTimer >= timeNeededToDisguise)
            TryEnterDisguise();
    }

    private void TryEnterDisguise()
    {
        CmdSetDisguise(true);
    }

    private void OnToggleDisguisePressed(InputAction.CallbackContext context)
    {
        CmdSetDisguise(!isDisguised);
    }

    [Command]
    private void CmdSetDisguise(bool value)
    {
        if (isDisguised == value)
            return;

        isDisguised = value;

        RpcPlayDisguiseSound();
    }

    [ClientRpc]
    private void RpcPlayDisguiseSound()
    {
        if (disguiseAudioSource == null || rustleSound == null)
            return;

        disguiseAudioSource.PlayOneShot(rustleSound);
    }

    private void OnDisguiseChanged(bool oldValue, bool newValue)
    {
        ApplyDisguiseVisuals();
    }

    private void ApplyDisguiseVisuals()
    {
        if (killerVisualModel != null)
            killerVisualModel.SetActive(!isDisguised);

        if (treeDisguiseModel != null)
        {
            treeDisguiseModel.SetActive(isDisguised);

            if (isDisguised)
            {
                treeDisguiseModel.transform.localPosition = Vector3.zero;
                treeDisguiseModel.transform.localRotation = Quaternion.Euler(treeRotationOffset);
            }
        }

        if (isOwned)
        {
            if (playerMovement != null)
                playerMovement.isFrozen = isDisguised;

            if (playerCamera != null)
                playerCamera.nearClipPlane = isDisguised ? disguisedNearClipPlane : normalNearClipPlane;

            if (localMeshVisibility != null)
            {
                //disguised = hide local body.
                //not disguised = return to normal first person visibility
                localMeshVisibility.SetForcedLocalVisible(false);
                localMeshVisibility.RefreshVisibility();
            }
        }

        if (!isDisguised)
            stillTimer = 0f;
    }

    public override void OnStopAuthority()
    {
        if (_playerInput != null)
        {
            _playerInput.Player.KillerExitDisguise.performed -= OnToggleDisguisePressed;

            _playerInput.Disable();
            _playerInput = null;
        }

        if (playerMovement != null)
            playerMovement.isFrozen = false;

        if (playerCamera != null)
            playerCamera.nearClipPlane = normalNearClipPlane;

        if (localMeshVisibility != null)
        {
            localMeshVisibility.SetForcedLocalVisible(false);
            localMeshVisibility.RefreshVisibility();
        }
    }
}