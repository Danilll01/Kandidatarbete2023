using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PillPlayerController : MonoBehaviour
{
    public Planet attractor;
    public Camera firstPersonCamera;
    public float movementSpeed;
    public float airControlFactor;
    public float jumpForce;

    private Rigidbody body;
    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        //A bit of a hack to give the player a starting planet
        attractor = GameObject.Find("Sun").transform.parent.transform.GetChild(1).GetComponent<Planet>();
        Vector3 directionNearestPlanet = attractor.transform.position - transform.position;
        Physics.Raycast(transform.position, directionNearestPlanet, out RaycastHit hit);
        transform.SetParent(attractor.transform);

        //Put the player above the ground
        transform.position = hit.point - (directionNearestPlanet.normalized) * 5;

        //Lock the mouse inside of the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        Gravity.KeepUpright(transform, attractor.transform);
        Gravity.Attract(transform.position, body, attractor.transform.position, attractor.mass);
    }

    private void HandleInput()
    {
        //Keep old Y velocity. Rotates to world space, grabs y velocity and rotates back to planet orientation
        Vector3 oldY = transform.rotation * new Vector3(0, (Quaternion.Inverse(transform.rotation) * body.velocity).y);
        //New movement
        Vector3 movementVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")) * movementSpeed;
        //Jumping
        if (Input.GetKeyDown(KeyCode.Space) && Grounded)
        {
            movementVector.y += jumpForce;
        }
        //Input recieved
        if (movementVector.magnitude != 0)
        {
            //Ground controls
            if (Grounded)
            {
                body.velocity = transform.rotation * movementVector;
                body.velocity += oldY;
            }
            //Air controls
            else
            {
                body.velocity += transform.rotation * movementVector * Time.deltaTime * airControlFactor;
            }
        }
        //No input
        else if (Grounded)
        {
            body.velocity = Vector3.zero;
            body.velocity += oldY;
        }

        //Rotate player and camera
        Vector3 cameraRotationVector = new Vector3(Input.GetAxis("Mouse Y") * -1, 0);
        Vector3 playerRotationVector = new Vector3(0, Input.GetAxis("Mouse X"));
        firstPersonCamera.transform.Rotate(cameraRotationVector);
        transform.Rotate(playerRotationVector);
    }

    /// <summary>
    /// The altitude of the player from the currently attracting planet.
    /// </summary>
    public float Altitude
    {
        get { return (attractor.transform.position - transform.position).magnitude; }
    }

    public bool Grounded
    {
        get { return Physics.Raycast(transform.position,  attractor.transform.position  - transform.position, 2f); }
    }
}
