using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Util;
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
    [SerializeField] private float springLength = 10f;
    [SerializeField] private float springStrength = 1000f;
    [SerializeField] private float springDampening = 1000f;
    [SerializeField] private RaySpring[] springs;
    
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
    private LayerMask collisionCheckMask;
    private Collider[] activateHoverSpring = new Collider[1];

   
    
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
       
        collisionCheckMask = LayerMask.GetMask("Planet");
        collisionCheckMask |= ( 1 << LayerMask.NameToLayer("Foliage"));
        collisionCheckMask |= ( 1 << LayerMask.NameToLayer("Food"));

    }

    // Handles movement of ship
    void FixedUpdate()
    {
        // If player is not boarded, we do not need to do ship movement
        if (!Universe.player.boarded || shipTransitionScript.UnderTransition())
        {
            physicsBody.isKinematic = true;
            
            // Setup for Ship transition handover
            isOutsidePlanet = true;
            lookRotation = transform.rotation;
            oldMovementVector = Vector3.zero;
            mouseXSmooth = 0;
            mouseYSmooth = 0;
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
        physicsBody.velocity = new Vector3(moveDirection.x, moveDirection.y, moveDirection.z);

        //Camera follow
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraPosition.position, Time.deltaTime * cameraSmooth);
        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraPosition.rotation, Time.deltaTime * cameraSmooth);

        //Rotation
        float rotationZTmp = Input.GetAxis("Spaceship Roll");
        float currentMouseXMovement = Input.GetButton("ShipFreeLook") ? 0 : Input.GetAxis("Horizontal Look");
        float currentMouseYMovement = Input.GetButton("ShipFreeLook") ? 0 : Input.GetAxis("Vertical Look");
        Vector3 currentRotationVector = new Vector3(currentMouseXMovement, currentMouseYMovement, rotationZTmp);
        
        mouseXSmooth = Mathf.Lerp(mouseXSmooth, currentMouseXMovement * rotationSpeed,  0.01f * cameraSmooth);
        mouseYSmooth = Mathf.Lerp(mouseYSmooth, currentMouseYMovement * rotationSpeed, 0.01f * cameraSmooth);
        Quaternion localRotation = Quaternion.Euler(mouseYSmooth, mouseXSmooth, rotationZTmp * rotationSpeed);

        // The mouse local look rotation
        lookRotation = lookRotation * localRotation;
        
        RotateMainShip();
        
        RotateVisualShipModel();

        //Update crosshair texture
        if (crosshairTexture)
        {
            crosshairTexture.anchoredPosition = new Vector2(rotationY + defaultShipRotation.y, -(rotationX + defaultShipRotation.x - 20)) * crossHairMovement;
        }
        
        // Turn upright after being inactive
        PlayerInactiveCheck(currentRotationVector, newMovementVector);
        
        // Add hover forces
        CalculateSpringForces();
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

    // Turns the ship upright and pulls it toward the planet when player is inactive
    private void PlayerInactiveCheck(Vector3 currentRotationVector, Vector3 newMovementVector)
    {
        if (Universe.player.attractor == null || currentRotationVector != Vector3.zero || newMovementVector != Vector3.zero)
        {
            inactiveTimer = inactiveTime;
        }
        else
        {
            // Check if inactive timer has run out
            if (inactiveTimer < 0 && Universe.player.attractor != null)
            {
                // Flip upright and pull towards ground
                lookRotation = Quaternion.Lerp(lookRotation, Quaternion.identity, Time.fixedDeltaTime / 3f);
                Vector3 planetCenter = transform.parent.position - transform.position;
                physicsBody.AddForce(planetCenter.normalized * 200);
            }
            else
            {
                inactiveTimer -= Time.deltaTime;
            }
        }
    }
    
    // Calculates spring hover forces for all springs on the ship
    private void CalculateSpringForces()
    {
        // If springs are deactivated, do not do any calls
        int collisions = Physics.OverlapCapsuleNonAlloc(transform.position + transform.forward * 16, transform.position -transform.forward * 14, 20, activateHoverSpring, collisionCheckMask);
        if (collisions <= 0) return;
        
        // Add spring force from springs 
        foreach (RaySpring raySpring in springs)
        {
            raySpring.AddSpringForce(physicsBody, springLength, springStrength, springDampening);
        }
    }


}
