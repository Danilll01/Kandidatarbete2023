using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PillPlayerController : MonoBehaviour
{
    public Planet attractor;
    public Camera firstPersonCamera;
    public float movementSpeed;

    private Rigidbody body;
    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        CenterOfGravity.KeepUpright(transform, attractor.transform);
        CenterOfGravity.Attract(transform, body, attractor.transform, attractor.mass);
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
        CenterOfGravity.KeepUpright(transform, attractor.transform);
    }
}
