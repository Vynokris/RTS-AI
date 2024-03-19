using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Rendering.FilterWindow;

public class CameraControls : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private float yMin = 2.0f;
    [SerializeField] private float yMax = 20.0f;

    [SerializeField] private float movementSpeed = 10.0f;
    [SerializeField] private float movementTime = 2.0f;

    private Vector3 newPosition;
    private Vector3 lastCorrectZoom;
    private Vector3 newZoom;

    private void Start()
    {
        newPosition = transform.position;
        newZoom = cameraTransform.localPosition;
    }

    void Update()
    {
        HandleMovementInput();
    }

    private void HandleMovementInput()
    {
        if (Input.GetKey(KeyCode.W))
        {
            newPosition += (transform.forward * movementSpeed);
        }

        if (Input.GetKey(KeyCode.A))
        {
            newPosition += (transform.right * -movementSpeed);
        }

        if (Input.GetKey(KeyCode.S))
        {
            newPosition += (transform.forward * -movementSpeed);
        }

        if (Input.GetKey(KeyCode.D))
        {
            newPosition += (transform.right * movementSpeed);
        }

        newZoom += cameraTransform.forward * Input.mouseScrollDelta.y * 2;

        if (newZoom.y < yMin)
        {
            newZoom = lastCorrectZoom;
            newZoom.y = yMin;
        }

        else if (newZoom.y > yMax)
        {
            newZoom = lastCorrectZoom;
            newZoom.y = yMax;
        }

        else
        {
            lastCorrectZoom = newZoom;
        }

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }
}
