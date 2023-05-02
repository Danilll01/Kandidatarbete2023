using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Util;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Cursor = UnityEngine.Cursor;

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
    [SerializeField] private float mouseSensitivity = 9f;
    
    [Header("Camera stuff")]
    [SerializeField] private float rotationSpeed = 2.0f;
    [SerializeField] private float cameraSmooth = 4f;
    
    [Header("Spaceship setup stuff")]
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform spaceshipRoot;
    [SerializeField] private RectTransform crosshairTexture;
    [SerializeField] private Image travelModeGUIImage;
    [SerializeField] private Material travelModePanel;
    [SerializeField] private Sprite[] travelTypeSprites;
    [SerializeField] private Texture2D[] travelTypeTextures;
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
    private Vector3 currentRotationVector = Vector3.zero;
    private GameObject standardShip;
    private bool isOutsidePlanet = false;
    private LayerMask collisionCheckMask;

    // Backend stuff
    private float inactiveTimer = 0;
    private bool orbitPlanetMovement = true;
    private readonly Collider[] activateHoverSpring = new Collider[1];

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
       
        // Setting up optimization mask
        collisionCheckMask = LayerMask.GetMask("Planet");
        collisionCheckMask |= ( 1 << LayerMask.NameToLayer("Foliage"));
        collisionCheckMask |= ( 1 << LayerMask.NameToLayer("Food"));
        
        // Sets up GUI
        travelModeGUIImage.sprite = travelTypeSprites[0];
        travelModePanel.mainTexture = travelTypeTextures[0];
    }

    // Handle mouse input in normal update to handle framerate issues otherwise
    private void Update()
    {
        float rotationZTmp = Input.GetAxis("Spaceship Roll");
        float currentMouseXMovement = Input.GetButton("ShipFreeLook") ? 0 : Input.GetAxis("Horizontal Look");
        float currentMouseYMovement = Input.GetButton("ShipFreeLook") ? 0 : Input.GetAxis("Vertical Look");
        currentRotationVector = new Vector3(currentMouseXMovement, currentMouseYMovement, rotationZTmp);
        
        mouseXSmooth = Mathf.Lerp(mouseXSmooth, currentMouseXMovement * rotationSpeed,  0.02f * mouseSensitivity);
        mouseYSmooth = Mathf.Lerp(mouseYSmooth, currentMouseYMovement * rotationSpeed, 0.02f * mouseSensitivity);

        // Change movement type between orbit follow and straight
        if (Input.GetButtonDown("ChangeShipMovementType"))
        {
            
            if (orbitPlanetMovement)
            {
                lookRotation = standardShip.transform.rotation * lookRotation;
                travelModeGUIImage.sprite = travelTypeSprites[1];
                travelModePanel.mainTexture = travelTypeTextures[1];
            }
            else
            {
                lookRotation = Quaternion.Inverse(standardShip.transform.rotation) * transform.rotation;
                travelModeGUIImage.sprite = travelTypeSprites[0];
                travelModePanel.mainTexture = travelTypeTextures[0];
            }
            orbitPlanetMovement = !orbitPlanetMovement;
            
        }
    }

    // Handles movement of ship
    void FixedUpdate()
    {
        // If player is not boarded, we do not need to do ship movement
        if (!Universe.player.boarded || shipTransitionScript.UnderTransition())
        {
            physicsBody.isKinematic = true;
            crosshairTexture.gameObject.SetActive(false);
            travelModeGUIImage.enabled = false;
            
            // Setup for Ship transition handover
            isOutsidePlanet = true;
            lookRotation = transform.rotation;
            oldMovementVector = Vector3.zero;
            mouseXSmooth = 0;
            mouseYSmooth = 0;
            return;
        }
        
        // Setup for when ship is active
        physicsBody.isKinematic = false;
        crosshairTexture.gameObject.SetActive(true);
        travelModeGUIImage.enabled = true;
        
        // Movement
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
        Quaternion localRotation = Quaternion.Euler(mouseYSmooth, mouseXSmooth, currentRotationVector.z * rotationSpeed);

        // The mouse local look rotation
        lookRotation = lookRotation * localRotation;
        
        RotateMainShip();
        
        RotateVisualShipModel();

        //Update crosshair texture
        if (crosshairTexture)
        {
            if (!Input.GetButton("ShipFreeLook"))
            {
                crosshairTexture.anchoredPosition = new Vector2(rotationY + defaultShipRotation.y, -(rotationX + defaultShipRotation.x - 20)) * crossHairMovement;
            }
            else
            {
                crosshairTexture.gameObject.SetActive(false);
            }
            
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
                lookRotation = orbitPlanetMovement ? Quaternion.Inverse(standardShip.transform.rotation) * transform.rotation : transform.rotation;
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
                if (orbitPlanetMovement) { lookRotation = Quaternion.Inverse(standardShip.transform.rotation) * transform.rotation; }
                isOutsidePlanet = false;
            }
        }
        
        // Rotate the main ship (remove standardShip if automatic planet following is turned of)
        transform.rotation = orbitPlanetMovement ? standardShip.transform.rotation * lookRotation : lookRotation;

    }
    
    
    
    // Rotates visual model
    private void RotateVisualShipModel()
    {
        // Visual rotation
        rotationY += mouseXSmooth/10f;
        rotationX += mouseYSmooth/10f;
        rotationY = Mathf.Clamp(rotationY, -15, 15);
        rotationX = Mathf.Clamp(rotationX, -15, 15);
        spaceshipRoot.localEulerAngles = new Vector3(rotationX, rotationY, rotationZ);
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
                // Flip upright
                Quaternion rotateTowards = orbitPlanetMovement ? Quaternion.identity : Gravity.UprightRotation(transform, transform.parent);
                lookRotation = Quaternion.Lerp(lookRotation, rotateTowards, Time.fixedDeltaTime / 3f);
                
                // Pull towards ground
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
