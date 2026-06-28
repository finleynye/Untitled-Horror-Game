using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class KillerTreePlacementAbility : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Tree Prefabs")]
    [SerializeField] private GameObject previewTreePrefab;
    [SerializeField] private GameObject placedTreePrefab;

    [Header("Preview Material")]
    [SerializeField] private Material transparentPreviewMaterial;

    [Header("Placement Settings")]
    [SerializeField] private float placementDistance = 4f;
    [SerializeField] private float floorRayHeight = 5f;
    [SerializeField] private float floorRayDistance = 15f;
    [SerializeField] private LayerMask floorLayers;

    [Header("Rotation Settings")]
    [SerializeField] private bool faceSameDirectionAsPlayer = true;
    [SerializeField] private Vector3 treeRotationOffset = new Vector3(-90f, 0f, 0f);

    [Header("State")]
    [SerializeField] private bool isPlacingTree;
    [SerializeField] private bool canPlaceTree;

    [Header("Placement Limit")]
    [SerializeField] private int maxPlacedTrees = 5;

    [SyncVar]
    [SerializeField] private int placedTreeCount;

    private PlayerInput _playerInput;

    private GameObject currentPreviewTree;
    private Vector3 currentPlacementPosition;
    private Quaternion currentPlacementRotation;

    public bool IsPlacingTree => isPlacingTree;
    private bool CanPlaceMoreTrees => placedTreeCount < maxPlacedTrees;

    public override void OnStartAuthority()
    {
        if (!isOwned)
            return;

        _playerInput = new PlayerInput();

        //e
        _playerInput.Player.KillerInteract.performed += OnInteractPressed;

        //left click
        _playerInput.Player.KillerPlaceTree.performed += OnPlaceTreePressed;

        //right click
        _playerInput.Player.KillerCancelTree.performed += OnCancelTreePressed;

        _playerInput.Enable();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }
    private void Update()
    {
        if (!isOwned)
            return;

        //keep moving the preview while placement mode is active
        if (isPlacingTree)
            UpdateTreePreview();
    }

    private void OnInteractPressed(InputAction.CallbackContext context) => ToggleTreePlacement();
    private void OnPlaceTreePressed(InputAction.CallbackContext context) => TryPlaceTree();
    private void OnCancelTreePressed(InputAction.CallbackContext context) => CancelTreePlacement();

    private void ToggleTreePlacement()
    {
        //if already placing, pressing e cancels it
        if (isPlacingTree)
        {
            CancelTreePlacement();
            return;
        }

        StartTreePlacement();
    }
    private void StartTreePlacement()
    {
        //stop placing if the killer has reached the limit
        if (!CanPlaceMoreTrees)
            return;

        if (previewTreePrefab == null)
            return;

        if (playerCamera == null)
            return;

        isPlacingTree = true;
        canPlaceTree = false;

        //spawn the local ghost tree
        currentPreviewTree = Instantiate(previewTreePrefab);
        //make the preview use the transparent material
        ApplyPreviewMaterial(currentPreviewTree);

        UpdateTreePreview();
    }
    private void UpdateTreePreview()
    {
        if (currentPreviewTree == null)
            return;

        Vector3 forwardPosition = playerCamera.transform.position + playerCamera.transform.forward * placementDistance;

        //start the ray above that point so it can find the floor
        Vector3 rayStartPosition = forwardPosition + Vector3.up * floorRayHeight;

        //raycast down to snap the tree to the ground
        if (Physics.Raycast(rayStartPosition, Vector3.down, out RaycastHit hit, floorRayDistance, floorLayers))
        {
            canPlaceTree = true;

            currentPlacementPosition = hit.point;
            currentPlacementRotation = GetPlacementRotation();

            currentPreviewTree.SetActive(true);
            currentPreviewTree.transform.position = currentPlacementPosition;
            currentPreviewTree.transform.rotation = currentPlacementRotation;
        }
        else
        { 
            //hide the preview if there is no valid floor
            canPlaceTree = false;
            currentPreviewTree.SetActive(false);
        }
    }
    private Quaternion GetPlacementRotation()
    {
        Quaternion baseRotation;

        //make the tree face the same flat direction as the player
        if (faceSameDirectionAsPlayer)
        {
            Vector3 flatForward = playerCamera.transform.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude < 0.01f)
                baseRotation = transform.rotation;
            else
                baseRotation = Quaternion.LookRotation(flatForward.normalized);
        }
        else
            baseRotation = Quaternion.identity;

        //fix the model import rotation (tree model is sideways when spawned)
        Quaternion offsetRotation = Quaternion.Euler(treeRotationOffset);

        return baseRotation * offsetRotation;
    }
    private void TryPlaceTree()
    {
        if (!isPlacingTree)
            return;

        if (!canPlaceTree)
            return;

        if (!CanPlaceMoreTrees)
        {
            CancelTreePlacement();
            return;
        }

        //ask server to spawn the real tree
        CmdPlaceTree(currentPlacementPosition, currentPlacementRotation);

        CancelTreePlacement();
    }
    private void CancelTreePlacement()
    {
        if (!isPlacingTree && currentPreviewTree == null)
            return;

        isPlacingTree = false;
        canPlaceTree = false;

        //destroy the local preview tree
        if (currentPreviewTree != null)
            Destroy(currentPreviewTree);

        currentPreviewTree = null;
    }
    private void ApplyPreviewMaterial(GameObject previewObject)
    {
        if (transparentPreviewMaterial == null)
            return;

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer treeRenderer in renderers)
        {
            Material[] materials = treeRenderer.materials;

            //replace every material slot with the transparent material
            for (int i = 0; i < materials.Length; i++)
                materials[i] = transparentPreviewMaterial;
            
            treeRenderer.materials = materials;
        }
    }
    [Command]
    private void CmdPlaceTree(Vector3 position, Quaternion rotation)
    {
        if (placedTreePrefab == null)
            return;

        //server side limit so killer cant place 10,000 trees
        if (placedTreeCount >= maxPlacedTrees)
            return;

        GameObject placedTree = Instantiate(placedTreePrefab, position, rotation);

        NetworkServer.Spawn(placedTree);

        placedTreeCount++;
    }
    public override void OnStopAuthority()
    {
        if (_playerInput != null)
        {
            _playerInput.Player.Interact.performed -= OnInteractPressed;
            _playerInput.Player.KillerPlaceTree.performed -= OnPlaceTreePressed;
            _playerInput.Player.KillerCancelTree.performed -= OnCancelTreePressed;

            _playerInput.Disable();
            _playerInput = null;
        }

        CancelTreePlacement();
    }
}