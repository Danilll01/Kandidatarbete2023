using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpaceShipTransition))]

public class SpaceShipController : MonoBehaviour
{
    [Header("Movement stuff")]
    [SerializeField] private float movementDampening = 25f;
    [SerializeField] private float normalSpeed = 25f;
    [SerializeField] private float maxSpeed = 45f;
    [SerializeField] [Range(0,1)] private float planetSlowdownFactor = 0.6f;
    
    [Header("Hovering stuff")]
    [SerializeField] private float strength = 10f;
    [SerializeField] private float hoverDampening = 100f;
    
    [Header("Ship setting stuff")] 
    [SerializeField] private float inactiveTime = 10f;
    [SerializeField] private float crossHairMovement = 30f;
    
    [Header("Camera stuff")]
    [SerializeField] private float rotationSpeed = 2.0f;
    [SerializeField] private float cameraSmooth = 4f;
    
    [Header("Spaceship setup stuff")]
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform spaceshipRoot;
    [SerializeField] private RectTransform crosshairTexture;
    [SerializeField] private SpaceShipTransition shipTransitionScript;

    // Ship movement
    private float speed;
    private Rigidbody physicsBody;
    private Quaternion lookRotation;
    private float rotationX = 0;
    private float rotationY = 0;
    private float rotationZ = 0;
    private float mouseXSmooth = 0;
    private float mouseYSmooth = 0;
    private Vector3 defaultShipRotation;
    private Vector3 oldMovementVector = new();
    private GameObject standardShip;
    private bool isOutsidePlanet = false;
    private bool canMove = true;
    private Vector3 enterGroundPosition;
   
    
    // Backend stuff
    private float inactiveTimer = 0;

    /// <summary>
    /// Initializes ship controller script
    /// </summary>
    public void Initialize()
    {
        physicsBody = GetComponent<Rigidbody>();
        physicsBody.useGravity = false;
        lookRotation = transform.rotation;
        defaultShipRotation = spaceshipRoot.localEulerAngles;
        rotationY = defaultShipRotation.z;
        inactiveTimer = inactiveTime;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        shipTransitionScript.Initialize();
        Universe.spaceShip = transform;
        standardShip = new GameObject { hideFlags = HideFlags.HideInHierarchy };
    }

    // Handles movement of ship
    void FixedUpdate()
    {
        // If player is not boarded, we do not need to do ship movement
        if (!Universe.player.boarded || shipTransitionScript.UnderTransition())
        {
            physicsBody.isKinematic = true;
            lookRotation = transform.rotation;
            return;
        }

        physicsBody.isKinematic = false;
        
        float thrust = Input.GetAxis("Spaceship Thrust");
        float strafe = Input.GetAxis("Spaceship Strafe");
        float lift = Input.GetAxis("Spaceship Lift");

        Vector3 newMovementVector = new(strafe, lift, thrust);

        if (Universe.player.attractor != null)
        {
            newMovementVector *= planetSlowdownFactor;
        }
        
        oldMovementVector = Vector3.Lerp(oldMovementVector, newMovementVector, Time.deltaTime * movementDampening);

        if (Math.Abs(Input.GetAxisRaw("Sprint") - 1) > 0.001)
        {
            speed = Mathf.Lerp(speed, normalSpeed, Time.fixedDeltaTime);
        }
        else
        {
            speed = Mathf.Lerp(speed, maxSpeed, Time.fixedDeltaTime);
        }

        //Set moveDirection to the vertical axis (up and down keys) * speed
        Vector3 moveDirection = oldMovementVector * speed;

        //Transform the vector3 to local space
        moveDirection = transform.TransformDirection(moveDirection);
        //Set the velocity, so you can move
        if (canMove)
        {
            physicsBody.velocity = new Vector3(moveDirection.x, moveDirection.y, moveDirection.z);
        }
        else
        {
            physicsBody.velocity += new Vector3(moveDirection.x, moveDirection.y, moveDirection.z);
        }

        //Camera follow
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraPosition.position, Time.deltaTime * cameraSmooth);
        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraPosition.rotation, Time.deltaTime * cameraSmooth);

        //Rotation
        float rotationZTmp = Input.GetAxis("Spaceship Roll");
        float currentMouseXMovement = Input.GetAxis("Horizontal Look");
        float currentMouseYMovement = Input.GetAxis("Vertical Look");
        Vector3 currentRotationVector = new Vector3(currentMouseXMovement, currentMouseYMovement, rotationZTmp);
        
        mouseXSmooth = Mathf.Lerp(mouseXSmooth, currentMouseXMovement * rotationSpeed,  0.01f * cameraSmooth);
        mouseYSmooth = Mathf.Lerp(mouseYSmooth, currentMouseYMovement * rotationSpeed, 0.01f * cameraSmooth);
        Quaternion localRotation = Quaternion.Euler(mouseYSmooth, mouseXSmooth, rotationZTmp * rotationSpeed);

        // The mouse local look rotation
        lookRotation = lookRotation * localRotation;

        if (Universe.player.attractor == null || currentRotationVector != Vector3.zero || newMovementVector != Vector3.zero)
        {
            inactiveTimer = inactiveTime;
        }
        else
        {
            if (inactiveTimer < 0 && Universe.player.attractor != null)
            {
                lookRotation = Quaternion.Lerp(lookRotation, Quaternion.identity, Time.fixedDeltaTime / 3f);
            }
            else
            {
                inactiveTimer -= Time.deltaTime;
            }
        }
        
        
        RotateMainShip();
        
        RotateVisualShipModel();

        //Update crosshair texture
        if (crosshairTexture)
        {
            crosshairTexture.anchoredPosition = new Vector2(rotationY + defaultShipRotation.y, -(rotationX + defaultShipRotation.x - 20)) * crossHairMovement;
        }
    }

  
    // Rotates the main ship body
    private void RotateMainShip()
    {
        // Ship rotation
        if (Universe.player.attractor == null)
        {
            // If the ship goes away from planet, rotate ship correctly
            if (!isOutsidePlanet)
            {
                // The inverse is only needed when automatic orbiting is turned on
                lookRotation = Quaternion.Inverse(standardShip.transform.rotation) * transform.rotation;
                isOutsidePlanet = true;
            }
        }
        else
        {
            // Model the standard ship around the planet
            standardShip.transform.position = transform.position;
            standardShip.transform.rotation = Gravity.UprightRotation(standardShip.transform, transform.parent.transform);

            // If the ship comes into orbit, match the look rotation with the standard ship rotation
            if (isOutsidePlanet)
            {
                // Camera fix when entering planets if automatic planet following is turned on
                lookRotation = Quaternion.Inverse(standardShip.transform.rotation) * transform.rotation;
                isOutsidePlanet = false;
            }
        }

        // Rotate the main ship (remove standardShip if automatic planet following is turned of)
        transform.rotation = standardShip.transform.rotation * lookRotation;
    }
    
    
    
    // Rotates visual model
    private void RotateVisualShipModel()
    {
        // Visual rotation
        rotationY += mouseXSmooth/10f;
        rotationX += mouseYSmooth/10f;
        rotationY = Mathf.Clamp(rotationY, -15, 15);
        rotationX = Mathf.Clamp(rotationX, -15, 15);
        spaceshipRoot.transform.localEulerAngles = new Vector3(rotationX, rotationY, rotationZ);
        rotationY = Mathf.Lerp(rotationY, defaultShipRotation.y, Time.deltaTime * cameraSmooth);
        rotationX = Mathf.Lerp(rotationX, defaultShipRotation.x, Time.deltaTime * cameraSmooth);
        rotationZ = Mathf.Lerp(rotationZ, Input.GetAxis("Spaceship Roll") * 2f, Time.deltaTime * cameraSmooth);
    }

    private void OnCollisionEnter(Collision other)
    {
        canMove = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        enterGroundPosition = transform.position;
    }
    
    private void OnTriggerStay(Collider other)
    {
        
        float force = GetHoverForce(other.contactOffset);
        physicsBody.AddForceAtPosition(Vector3.Normalize(transform.position - Universe.player.attractor.transform.position) * force, transform.position);
        Debug.Log("INNE: " + other.contactOffset);
    }

    /*private void OnCollisionStay(Collision other)
    {
        ContactPoint contact = other.GetContact(0);
        float force = GetHoverForce(contact.separation);
        physicsBody.AddForceAtPosition(contact.normal * force, physicsBody.worldCenterOfMass);
        Debug.Log("INNE: " + contact.separation);
    }*/

    private float GetHoverForce(float distance)
    {
        float force = strength * distance;// + (hoverDampening * physicsBody.velocity.magnitude);
        return Mathf.Max(0f, force);
    }
    
    private void OnTriggerExit(Collider other)
    {
        //enterGroundPosition = null;
    }
    
    private void OnCollisionExit(Collision other)
    {
        canMove = true;
    }
    
}
