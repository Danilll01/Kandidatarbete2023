using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PillPlayerController : MonoBehaviour
{
    public Planet attractor = null;
    public Camera firstPersonCamera;
    public float movementSpeed;
    public float airControlFactor;
    public float jumpForce;
    [SerializeField] private float swimForce;
    [SerializeField] private float maxSwimSpeed = 10;
    public float maxSpeed;

    [SerializeField] private PlayerWater playerWater;

    private Rigidbody body;
    [HideInInspector] public bool paused;
    private ShipController ship;
    [HideInInspector] public bool boarded = false;

    private void Awake()
    {
        Universe.player = this;
    }

    // Start is called before the first frame update
    public void Initialize(GameObject planetToSpawnOn)
    {
        Universe.player = this;

        if (attractor == null)
        {
            attractor = planetToSpawnOn.GetComponent<Planet>();
            if (attractor == null)
            {
                Debug.LogError("Planet player spawned on has no Planet script");
            }
        }

        body = GetComponent<Rigidbody>();
        ship = GameObject.Find("Spaceship").GetComponent<ShipController>();

        //A bit of a hack to give the player a starting planet
        transform.position = planetToSpawnOn.transform.position + new Vector3(0, attractor.diameter, 0);
        Vector3 directionNearestPlanet = attractor.transform.position - transform.position;
        Physics.Raycast(transform.position, directionNearestPlanet, out RaycastHit hit);

        //Put the player above the ground
        transform.position = hit.point - (directionNearestPlanet.normalized) * 5;
        ship.Initialize(body, firstPersonCamera);

        //Lock the mouse inside of the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        paused = false;

        playerWater.Initialize(attractor);
    }

    // Update is called once per frame
    void Update()
    {
        if (!paused && !boarded)
        {
            HandleMovement();
            HandleCamera();
            if(!ReferenceEquals(attractor, playerWater.planet)) playerWater.UpdatePlanet(attractor);
            playerWater.UpdateWater(transform.position);
        }
        if (attractor != null)
        {
            DisplayDebug.AddOrSetDebugVariable("Current planet", attractor.bodyName);
            DisplayDebug.AddOrSetDebugVariable("Planet radius", attractor.diameter.ToString());
            DisplayDebug.AddOrSetDebugVariable("Planet mass", attractor.mass.ToString());
            DisplayDebug.AddOrSetDebugVariable("Planet surface gravity", attractor.surfaceGravity.ToString());
        }
        else
        {
            DisplayDebug.AddOrSetDebugVariable("Current planet", "Sun");
            DisplayDebug.AddOrSetDebugVariable("Planet radius", "N/A");
            DisplayDebug.AddOrSetDebugVariable("Planet mass", "N/A");
            DisplayDebug.AddOrSetDebugVariable("Planet surface gravity", "N/A");
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
        
        //Swiming
        if(Swimming)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                movementVector.y += swimForce * 10 * Time.deltaTime;
            }
            else
            {
                movementVector.y += 0.0001f;
            }
            
        }
        //Jumping
        else if (Input.GetKeyDown(KeyCode.Space) && Grounded)
        {
            movementVector.y += jumpForce;
        }
        
        //Input recieved
        if (movementVector.magnitude != 0)
        {
            //Ground controls + swim controls
            if(Swimming)
            {
                float currentUppSpeed = (Quaternion.Inverse(transform.rotation) * body.velocity).y + movementVector.y;

                if (Mathf.Abs(currentUppSpeed) > maxSwimSpeed)
                {
                    movementVector.y = maxSwimSpeed * Mathf.Sign(currentUppSpeed);
                }
                else
                {
                    movementVector.y = currentUppSpeed;
                }
                body.velocity = transform.rotation * movementVector;


            }
            else if (Grounded)
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

    public Planet Planet
    {
        get { return attractor; }
        set { attractor = value; }
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
        get { return Physics.Raycast(transform.position, attractor.transform.position - transform.position, 2f); }
    }

    public Vector3 Up
    {
        get { return (transform.position - attractor.transform.position).normalized; }
    }

    private bool Swimming
    {
        get { return playerWater.underWater; }
    }

}
