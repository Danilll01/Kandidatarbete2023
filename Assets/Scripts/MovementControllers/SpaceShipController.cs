using System;
using System.Collections;
using System.Collections.Generic;
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
    private float rotationZ = 0;
    private float mouseXSmooth = 0;
    private float mouseYSmooth = 0;
    private Vector3 defaultShipRotation;
    private Vector3 oldMovementVector = new();

    /// <summary>
    /// Initializes ship controller script
    /// </summary>
    public void Initialize()
    {
        physicsBody = GetComponent<Rigidbody>();
        physicsBody.useGravity = false;
        lookRotation = transform.rotation;
        defaultShipRotation = spaceshipRoot.localEulerAngles;
        rotationZ = defaultShipRotation.z;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        shipTransitionScript.Initialize();
        Universe.spaceShip = transform;
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
        oldMovementVector = Vector3.Lerp(oldMovementVector, newMovementVector, Time.fixedDeltaTime * movementDampening);

        if (Math.Abs(Input.GetAxisRaw("Sprint") - 1) > 0.001)
        {
            speed = Mathf.Lerp(speed, normalSpeed, Time.fixedDeltaTime);
        }
        else
        {
            speed = Mathf.Lerp(speed, maxSpeed, Time.fixedDeltaTime * 2);
        }

        //Set moveDirection to the vertical axis (up and down keys) * speed
        Vector3 moveDirection = oldMovementVector * speed;

        //Transform the vector3 to local space
        moveDirection = transform.TransformDirection(moveDirection);
        //Set the velocity, so you can move
        physicsBody.velocity = new Vector3(moveDirection.x, moveDirection.y, moveDirection.z);

        //Camera follow
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraPosition.position, Time.fixedDeltaTime * cameraSmooth);
        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraPosition.rotation, Time.fixedDeltaTime * cameraSmooth);

        //Rotation
        float rotationZTmp = Input.GetAxis("Spaceship Roll");

        mouseXSmooth = Mathf.Lerp(mouseXSmooth, Input.GetAxis("Horizontal Look") * rotationSpeed, Time.fixedDeltaTime * cameraSmooth);
        mouseYSmooth = Mathf.Lerp(mouseYSmooth, Input.GetAxis("Vertical Look") * rotationSpeed, Time.fixedDeltaTime * cameraSmooth);
        
        Quaternion localRotationY = Quaternion.Euler(mouseYSmooth, 0, rotationZTmp * rotationSpeed);
        Quaternion localRotationX = Quaternion.Euler(0, mouseXSmooth, 0);

        Debug.Log(Gravity.UprightRotation(transform, transform.parent.transform));
        lookRotation = lookRotation * localRotationY;

        /*Quaternion rotationBefore = transform.rotation;
        Quaternion rotationAfter = Gravity.UprightRotation(transform, transform.parent.transform);
        Quaternion rotationDifference = Quaternion.Inverse(rotationBefore) * rotationAfter;*/
        

        transform.rotation = Gravity.UprightRotation(transform, transform.parent.transform) * (localRotationX * lookRotation);
        //lookRotation = transform.rotation * Quaternion.Inverse(Gravity.UprightRotation(transform, transform.parent.transform));
    
        
        
        Quaternion Upright(Transform entity, Transform centerOfGravity)
        {
            Vector3 directionFromCenter = entity.position - centerOfGravity.transform.position;
            directionFromCenter = directionFromCenter.normalized;
            return Quaternion.FromToRotation(entity.up, directionFromCenter);
        }
        
      

        if (Input.GetKeyDown(KeyCode.H))
        {
            transform.rotation = Gravity.UprightRotation(transform, transform.parent.transform);
        }
        
        rotationZ -= mouseXSmooth;
        rotationZ = Mathf.Clamp(rotationZ, -45, 45);
        spaceshipRoot.transform.localEulerAngles = new Vector3(defaultShipRotation.x, defaultShipRotation.y, rotationZ);
        rotationZ = Mathf.Lerp(rotationZ, defaultShipRotation.z, Time.fixedDeltaTime * cameraSmooth);

        //Update crosshair texture
        if (crosshairTexture)
        {
            crosshairTexture.position = mainCamera.WorldToScreenPoint(transform.position + transform.forward * 100);
        }
    }
}
