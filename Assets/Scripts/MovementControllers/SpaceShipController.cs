using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI speedGauge;
    [SerializeField] private Material travelModePanel;
    [SerializeField] private Sprite[] travelTypeSprites;
    [SerializeField] private Texture2D[] travelTypeTextures;
    [SerializeField] private SpaceShipTransition shipTransitionScript;
    [SerializeField] private new HandleAudio audio;

    // Ship movement
    private float speed;
    private Rigidbody physicsBody;
    private Quaternion lookRotation;
    private bool? shipGUIActive;
    // Velocity kept from a change of reference frame, in the ship's local space
    private Vector3 frameVelocity;
    // Seconds for the speed kept from a reference frame change to mostly fade into what the controls ask for
    [SerializeField] private float frameVelocityFadeTime = 1.5f;
    private int lastShownSpeed = -1;
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
        audio.Initialize();

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
        if (Input.GetButtonDown("ChangeShipMovementType") && Universe.player.boarded)
        {
            if (orbitPlanetMovement)
            {
                lookRotation = standardShip.transform.rotation * lookRotation;
                travelModeGUIImage.sprite = travelTypeSprites[1];
                travelModePanel.mainTexture = travelTypeTextures[1];
                audio.PlaySimpleSoundEffect(HandleAudio.SoundEffects.Toggle);
            }
            else
            {
                lookRotation = Quaternion.Inverse(standardShip.transform.rotation) * transform.rotation;
                travelModeGUIImage.sprite = travelTypeSprites[0];
                travelModePanel.mainTexture = travelTypeTextures[0];
                audio.PlaySimpleSoundEffect(HandleAudio.SoundEffects.Toggle);
            }
            orbitPlanetMovement = !orbitPlanetMovement;
            
        }
    }

    /// <summary>
    /// Sets the ship's world velocity, keeping the difference to what the controls ask for as frame velocity that fades out.
    /// Used when a planet becomes the centre of the solar system and stops moving.
    /// </summary>
    public float FrameVelocityFadeTime => frameVelocityFadeTime;

    public void MatchReferenceFrame(Vector3 velocity)
    {
        if (physicsBody == null || physicsBody.isKinematic) return;

        AddFrameVelocity(velocity - physicsBody.linearVelocity);
    }

    /// <summary>
    /// Adds a velocity from a change of reference frame (for example a planet's orbit speed when leaving it).
    /// It is kept relative to the ship, so it turns with the ship like the controls do, and fades out over time.
    /// </summary>
    public void AddFrameVelocity(Vector3 worldVelocity)
    {
        if (physicsBody == null || physicsBody.isKinematic) return;

        frameVelocity += Quaternion.Inverse(transform.rotation) * worldVelocity;
        physicsBody.linearVelocity += worldVelocity;
    }

    private void SetShipGUIActive(bool active)
    {
        if (shipGUIActive == active) return;
        shipGUIActive = active;

        crosshairTexture.gameObject.SetActive(active);
        travelModeGUIImage.enabled = active;
        speedGauge.gameObject.SetActive(active);
    }

    // Handles movement of ship
    void FixedUpdate()
    {
        // If player is not boarded, we do not need to do ship movement
        if (!Universe.player.boarded || shipTransitionScript.UnderTransition())
        {
            physicsBody.isKinematic = true;
            SetShipGUIActive(false);

            // Setup for Ship transition handover
            isOutsidePlanet = true;
            lookRotation = transform.rotation;
            oldMovementVector = Vector3.zero;
            frameVelocity = Vector3.zero;
            mouseXSmooth = 0;
            mouseYSmooth = 0;
            return;
        }
        
        // Setup for when ship is active
        physicsBody.isKinematic = false;
        SetShipGUIActive(true);

        // Movement
        float thrust = Input.GetAxis("Spaceship Thrust");
        float strafe = Input.GetAxis("Spaceship Strafe");
        float lift = Input.GetAxis("Spaceship Lift");

        Vector3 newMovementVector = new(strafe, lift, thrust);

        // Slows down if near planet
        if (Universe.player.attractor != null)
        {
            newMovementVector *= planetSlowdownFactor;
        }
        
        oldMovementVector = Vector3.Lerp(oldMovementVector, newMovementVector, Time.deltaTime * movementDampening);

        // Adds extra speed if you boost
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
        
        //Set the velocity, so you can move. The frame velocity keeps the speed relative to a planet that just became the centre.
        frameVelocity *= Mathf.Exp(-Time.fixedDeltaTime / frameVelocityFadeTime);
        physicsBody.linearVelocity = moveDirection + transform.rotation * frameVelocity;
        
        // Plays thruster audio if needed
        PlayThrustAudio(newMovementVector);
        
        // Updates speed GUI, only rebuilding the text when the shown value changes
        int shownSpeed = Mathf.RoundToInt(physicsBody.linearVelocity.magnitude);
        if (shownSpeed != lastShownSpeed)
        {
            lastShownSpeed = shownSpeed;
            speedGauge.SetText("{0}km/h", shownSpeed);
        }

        //Camera follow
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraPosition.position, Time.deltaTime * cameraSmooth);
        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraPosition.rotation, Time.deltaTime * cameraSmooth);

        //Rotation
        Quaternion localRotation = Quaternion.Euler(mouseYSmooth, mouseXSmooth, currentRotationVector.z * rotationSpeed);

        // The mouse local look rotation
        lookRotation = lookRotation * localRotation;
        
        // Rotates the invisible ship transform
        RotateMainShip();
        
        // Rotates the visual ship model
        RotateVisualShipModel();

        //Update crosshair texture
        UpdateCrossHair();
        
        // Turn upright after being inactive
        PlayerInactiveCheck(currentRotationVector, newMovementVector);
        
        // Add hover forces
        CalculateSpringForces();
    }

    // Plays thruster audio if needed 
    private void PlayThrustAudio(Vector3 newMovementVector)
    {
        // Play thrust sound effect if player is accelerating
        if (newMovementVector.magnitude > 0f)
        {
            audio.PlaySoundEffect(HandleAudio.SoundEffects.Thrust, true, false, volume:0.3f);
        }
        else
        {
            audio.TurnOffCurrentSoundEffect(0.2f);
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

    // Updates ship cross-hair
    private void UpdateCrossHair()
    {
        if (crosshairTexture)
        {
            if (!Input.GetButton("ShipFreeLook"))
            {
                crosshairTexture.anchoredPosition =
                    new Vector2(rotationY + defaultShipRotation.y, -(rotationX + defaultShipRotation.x - 20)) *
                    crossHairMovement;
            }
            else
            {
                crosshairTexture.gameObject.SetActive(false);
            }
        }
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
