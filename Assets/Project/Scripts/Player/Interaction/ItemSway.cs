using UnityEngine;

public class ItemSway : MonoBehaviour
{

    [Header("Sway Settings")]
    [SerializeField] private float rotationSmooth;
    [SerializeField] private float rotationMultiplier;

    [SerializeField] private float positionSmooth;
    [SerializeField] private float positionMultiplier;

    [SerializeField] CameraMovement camMove;

    private Quaternion _startRotation;
    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.localPosition;
        _startRotation = transform.localRotation;
    }
    
    private void Update()
    {
        if (camMove == null)
            return;
        
        var lookInput = camMove._lookInput;
        UpdateRotationSway(lookInput);
        UpdatePositionSway(lookInput);
    }

    private void UpdateRotationSway(Vector2 lookInput)
    {
        var mouseX = lookInput.x;
        var mouseY = lookInput.y;
        
        var rotationX = Quaternion.AngleAxis(-mouseY * rotationMultiplier, Vector3.right);
        var rotationY = Quaternion.AngleAxis(mouseX * rotationMultiplier, Vector3.up);
        var swayOffset = rotationX * rotationY;
        
        var baseRotation = Quaternion.Euler(0f, -90f, 0f); //hardcoded value im so sorry
        var targetRotation = swayOffset * baseRotation;

        // Rotate smoothly
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotationSmooth * Time.deltaTime);
    }
    
    private void UpdatePositionSway(Vector2 lookInput)
    {
        var mouseX = lookInput.x;
        var mouseY = lookInput.y;

        var targetPosition = _startPosition + new Vector3(-mouseX * positionMultiplier, -mouseY * positionMultiplier, 0f);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, positionSmooth * Time.deltaTime);
    }
}