using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PillPlayerController : MonoBehaviour
{
    public GameObject centerOfGravity;
    public Camera firstPersonCamera;
    public float movementSpeed;

    private Rigidbody body;
    private Quaternion playerSpin;
    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        playerSpin = transform.rotation;
        GetComponent<Rigidbody>().rotation = Quaternion.Euler(30, 30, 30);
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        KeepBodyUpright();
    }

    private void HandleInput()
    {
        Vector3 movementVector = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) {
            movementVector += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.A)) {
            movementVector += Vector3.left;
        }
        if (Input.GetKey(KeyCode.S)) {
            movementVector += Vector3.back;
        }
        if (Input.GetKey(KeyCode.D)) {
            movementVector += Vector3.right;
        }
        if (Input.GetKey(KeyCode.Space)) {
            movementVector += Vector3.up;
        }
        body.velocity += transform.rotation * movementVector * Time.deltaTime * movementSpeed;

        Vector3 cameraRotationVector = new Vector3(Input.GetAxis("Mouse Y") * -1, 0);
        Vector3 playerRotationVector = new Vector3(0, Input.GetAxis("Mouse X"));
        firstPersonCamera.transform.Rotate(cameraRotationVector);
        transform.Rotate(playerRotationVector);
    }

    private void KeepBodyUpright()
    {
        //Look at center of gravity
        Vector3 directionFromCenter = transform.position - centerOfGravity.transform.position;
        directionFromCenter = directionFromCenter.normalized;
        transform.rotation = Quaternion.FromToRotation(transform.up, directionFromCenter) * transform.rotation;
    }
}
