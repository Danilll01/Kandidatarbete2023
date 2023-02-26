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
    public float maxSpeed;

    private Rigidbody body;
    public bool paused;

    private Transform shipTransform;
    private Rigidbody shipBody;
    private bool boarded = false;
    Vector3 mountedPos = new Vector3(0, 1.6f, -1.4f);
    Vector3 dismountedPos = new Vector3(-2.6f, 2, -2f);
    // Start is called before the first frame update
    public void Initialize(GameObject planetToSpawnOn)
    {
        body = GetComponent<Rigidbody>();

        GameObject ship = GameObject.Find("Spaceship");
        shipTransform = ship.transform;
        shipBody = ship.GetComponent<Rigidbody>();

        //A bit of a hack to give the player a starting planet
        attractor = planetToSpawnOn.GetComponent<Planet>();
        Debug.Log(attractor);
        Debug.Log(planetToSpawnOn);
        transform.parent = attractor.transform;
        transform.position = planetToSpawnOn.transform.position + new Vector3(0, attractor.radius, 0);
        Vector3 directionNearestPlanet = attractor.transform.position - transform.position;
        Physics.Raycast(transform.position, directionNearestPlanet, out RaycastHit hit);

        //Put the player above the ground
        transform.position = hit.point - (directionNearestPlanet.normalized) * 5;
        shipTransform.position = transform.position - (directionNearestPlanet.normalized) * 10;
        shipTransform.SetParent(attractor.transform);

        //Lock the mouse inside of the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        paused = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!paused)
        {
            HandleMovement();
        }
        if (!boarded)
        {
            HandleCamera();
        }
        HandleShip();
        DisplayDebug.AddOrSetDebugVariable("Current planet", attractor.bodyName);
        DisplayDebug.AddOrSetDebugVariable("Planet radius", attractor.radius.ToString());
        DisplayDebug.AddOrSetDebugVariable("Planet mass", attractor.mass.ToString());
        DisplayDebug.AddOrSetDebugVariable("Planet surface gravity", attractor.surfaceGravity.ToString());
    }

    private void HandleShip()
    {
        //TODO check ability to board
        //Boarding
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (boarded)
            {
                //Disembark
                gameObject.transform.SetParent(attractor.gameObject.transform);
                transform.position = shipTransform.position + dismountedPos;
                transform.rotation = shipTransform.rotation;
                boarded = false;
            }
            else
            {
                //Embark
                gameObject.transform.SetParent(shipTransform);
                transform.position = shipTransform.position + mountedPos;
                transform.rotation = shipTransform.rotation;
                body.velocity = Vector3.zero;
                boarded = true;
            }
        }
    }

    private void HandleCamera()
    {
        //Rotate player and camera
        Vector3 cameraRotationVector = new Vector3(Input.GetAxis("Mouse Y") * -1, 0);
        Vector3 playerRotationVector = new Vector3(0, Input.GetAxis("Mouse X"));
        firstPersonCamera.transform.Rotate(cameraRotationVector);
        transform.Rotate(playerRotationVector);
    }

    private void HandleMovement()
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
                //Add movement
                body.velocity += transform.rotation * movementVector * Time.deltaTime * airControlFactor;
                //Normalize to maxSpeed if necessary
                Vector3 oldVelocity = (Quaternion.Inverse(transform.rotation) * body.velocity);
                Vector3 oldHorizontalVelocity = new Vector3(oldVelocity.x, 0, oldVelocity.z);
                if (oldHorizontalVelocity.magnitude > maxSpeed)
                {
                    Vector3 newHorizontalVelocity = oldHorizontalVelocity.normalized * maxSpeed;
                    Vector3 newVelocity = oldVelocity - oldHorizontalVelocity + newHorizontalVelocity;
                    body.velocity = transform.rotation * newVelocity;
                }
            }
        }
        //No input
        else if (Grounded)
        {
            body.velocity = Vector3.zero;
            body.velocity += oldY;
        }

        if (!boarded)
        {
            Gravity.KeepUpright(transform, attractor.transform);
            Gravity.Attract(transform.position, body, attractor.transform.position, attractor.mass);
        }
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
