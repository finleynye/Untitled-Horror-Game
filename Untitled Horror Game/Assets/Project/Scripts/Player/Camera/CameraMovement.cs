using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [Header("Refs")]
    public PlayerMovement playerMovement;
    [SerializeField] private Transform camHolder;
    [SerializeField] private Transform playerBody;

    [Header("FOV Settings")]
    private Camera cam;
    [SerializeField] private float defaultFOV = 70;
    [SerializeField] private float sprintFOV = 90;
    [SerializeField] private float fovSpeed = 10;

    public Vector2 _lookInput;
    public float verticalRotation;
    public float horizontalRotation;
    public float mouseSensitivity;
    void Start()
    {
        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        HandleFOV();
        HandleLook();
    }

    public void MouseLook(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
    }


    private void HandleFOV()
    {
        var isMovingForward = playerMovement._moveInput.y > 0.1f;
        var targetFOV = playerMovement._isSprinting && isMovingForward ? sprintFOV : defaultFOV;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);
    }

    private void HandleLook()
    {
        var mouseX = _lookInput.x * mouseSensitivity;
        var mouseY = _lookInput.y * mouseSensitivity;

        verticalRotation -= mouseY;
        horizontalRotation += mouseX;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        camHolder.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}
