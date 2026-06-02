using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemSway : MonoBehaviour
{

    [Header("Sway Settings")]
    [SerializeField] private float rotationSmooth;
    [SerializeField] private float rotationMultiplier;

    [SerializeField] private float positionSmooth;
    [SerializeField] private float positionMultiplier;

    [SerializeField] CameraMovement camMove;

    private Quaternion startRotation;
    private Vector3 startPosition;


    private void Start()
    {

    }
    private void Update()
    {
        if (camMove == null)
            return;


        Vector2 _lookInput = camMove._lookInput;

        UpdateRotationSway(_lookInput);



    }

    private void UpdateRotationSway(Vector2 lookInput)
    {
        //get mouse input
        var mouseX = lookInput.x;
        var mouseY = lookInput.y;

        //calculate target rotation
        Quaternion rotationX = Quaternion.AngleAxis(-mouseY * rotationMultiplier, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX * rotationMultiplier, Vector3.up);

        Quaternion targetRotation = rotationX * rotationY;

        //rotate 
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotationSmooth * Time.deltaTime);
    }


    private void UpdatePositionSway(Vector2 lookInput)
    {
        float mouseX = lookInput.x;
        float mouseY = lookInput.y;

        Vector3 targetPosition = startPosition + new Vector3(-mouseX * positionMultiplier, -mouseY * positionMultiplier, 0f);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, positionSmooth * Time.deltaTime);
    }
}