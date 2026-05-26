using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class CameraMovement : MonoBehaviour
{
    public PlayerMovement playerMovement;
    private PlayerInput _playerInput;

    [Header("FOV Settings")]
    private Camera cam;
    [SerializeField] private Transform camHolder;
    [SerializeField] private float defaultFOV = 70;
    [SerializeField] private float sprintFOV = 90;
    [SerializeField] private float fovSpeed = 10;

    private Vector2 _lookInput;
    public float verticalRotation;
    public float horizontalRotation;
    public float mouseSensitivity;
    void Start()
    {
        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _playerInput = new PlayerInput();

        _playerInput.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        HandleFOV();
    }

    public void MouseLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();

        var mouseX = _lookInput.x * mouseSensitivity;
        var mouseY = _lookInput.y * mouseSensitivity;

        verticalRotation -= mouseY;
        horizontalRotation += mouseX;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        camHolder.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    private void HandleFOV()
    {
        var isMovingForward = playerMovement._moveInput.y > 0.1f;
        var targetFOV = playerMovement._isSprinting && isMovingForward ? sprintFOV : defaultFOV;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
    }
    private void OnDisable()
        => _playerInput?.Disable();
}
