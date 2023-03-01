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
    [HideInInspector] public bool paused;

    [Header("Ship")]
    [SerializeField] private float shipMovespeed;
    [SerializeField] private float shipRotationSpeed;
    private Transform shipTransform;
    private bool boarded = false;
    private bool transitioning = false;
    private bool shipHoldingUprightRotation = false;
    private Vector3 mountedPos = new Vector3(0, 1.6f, -1.4f);
    private Vector3 dismountedPos = new Vector3(-2.6f, 2, -2f);

    [SerializeField] private float landingTime;
    private float transitionProgress = 0;
    private Vector3 transitionFromPos = Vector3.zero;
    private Quaternion transitionFromRot = Quaternion.identity;
    private Vector3 transitionToPos = Vector3.zero;
    private Quaternion transitionToRot = Quaternion.identity;
    
    // Start is called before the first frame update
    public void Initialize(GameObject planetToSpawnOn)
    {
        body = GetComponent<Rigidbody>();

        GameObject ship = GameObject.Find("Spaceship");
        shipTransform = ship.transform;

        //A bit of a hack to give the player a starting planet
        attractor = planetToSpawnOn.GetComponent<Planet>();
        transform.parent = attractor.transform;
        transform.position = planetToSpawnOn.transform.position + new Vector3(0, attractor.diameter, 0);
        Vector3 directionNearestPlanet = attractor.transform.position - transform.position;
        Physics.Raycast(transform.position, directionNearestPlanet, out RaycastHit hit);

        //Put the player above the ground
        transform.position = hit.point - (directionNearestPlanet.normalized) * 5;
        shipTransform.position = transform.position - (directionNearestPlanet.normalized) * 10;
        shipTransform.SetParent(attractor.transform);

        //Lock the mouse inside of the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        attractor.ShowCreatures(true);
        paused = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!paused)
        {
            if (transitioning)
            {
                HandleTransition();
            }
            else
            {
                if (!boarded)
                {
                    HandleMovement();
                    HandleCamera();
                }
                HandleShip();
            }
        }
        DisplayDebug.AddOrSetDebugVariable("Current planet", attractor.bodyName);
        DisplayDebug.AddOrSetDebugVariable("Planet radius", attractor.diameter.ToString());
        DisplayDebug.AddOrSetDebugVariable("Planet mass", attractor.mass.ToString());
        DisplayDebug.AddOrSetDebugVariable("Planet surface gravity", attractor.surfaceGravity.ToString());
    }

    private void HandleTransition()
    {
        // For now basic linear interpolation
        transitionProgress += Time.deltaTime;

        transform.position = Vector3.Lerp(transitionFromPos, transitionToPos, transitionProgress);
        transform.rotation = Quaternion.Lerp(transitionFromRot, transitionToRot, transitionProgress);

        if (transitionProgress >= 1)
        {
            transitioning = false;
            if (boarded)
            {
                //Disembark
                shipTransform.SetParent(attractor.gameObject.transform);
                transform.position = shipTransform.position + (shipTransform.rotation * dismountedPos);
                transform.rotation = shipTransform.rotation;
            }
            transitionProgress = 0;
            boarded = !boarded;
        }
    }

    private void HandleShip()
    {
        //TODO check ability to board
        //Boarding
        if (transitioning)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (boarded)
            {
                //Disembark
                Physics.Raycast(transform.position, -Up, out RaycastHit hit, 20,  1 << (LayerMask.NameToLayer("Planet")));

                if (hit.collider != null)
                {
                    transitionFromPos = transform.position;
                    transitionFromRot = transform.rotation;
                    transitionToPos = hit.point - Quaternion.FromToRotation(Vector3.up, Up) * Vector3.up * shipTransform.localPosition.y;
                    transitionToRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(shipTransform.TransformVector(Vector3.forward), hit.normal), hit.normal);
                    transitioning = true;
                }
            }
            else
            {
                //Embark
                transform.position = shipTransform.position + (shipTransform.rotation * mountedPos);
                transform.rotation = shipTransform.rotation;
                firstPersonCamera.transform.localRotation = Quaternion.identity;
                body.velocity = Vector3.zero;
                shipTransform.SetParent(transform);

                transitionFromPos = transform.position;
                transitionFromRot = transform.rotation;
                transitionToPos = transitionFromPos + Up * 10;
                transitionToRot = Gravity.UprightRotation(transform, attractor.transform);
                transitioning = true;
            }
        }

        //Controling ship
        if (!boarded)
        {
            return;
        }
        //Controls
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shipHoldingUprightRotation = !shipHoldingUprightRotation;
        }
        //Rotation
        float pitch = Input.GetAxis("Mouse Y") * -1;
        float yaw = Input.GetAxis("Mouse X");
        float roll = Input.GetAxis("Spaceship Roll");
        transform.Rotate(new Vector3(pitch, yaw, roll) * Time.deltaTime * shipRotationSpeed);
        if (shipHoldingUprightRotation)
        {
            Gravity.KeepUpright(transform, attractor.transform);
        }
        //Translation
        float strafe = Input.GetAxis("Spaceship Strafe");
        float lift = Input.GetAxis("Spaceship Lift");
        float thrust = Input.GetAxis("Spaceship Thrust");
        body.velocity += shipTransform.rotation * new Vector3(strafe, lift, thrust) * Time.deltaTime * shipMovespeed;
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

    public Vector3 Up
    {
        get { return (transform.position - attractor.transform.position).normalized; }
    }
}
