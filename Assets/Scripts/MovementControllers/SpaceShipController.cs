using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SpaceShipTransition))]

public class SpaceShipController : MonoBehaviour
{
    [SerializeField] private float normalSpeed = 25f;
    [SerializeField] private float accelerationSpeed = 45f;
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform spaceshipRoot;
    [SerializeField] private float rotationSpeed = 2.0f;
    [SerializeField] private float cameraSmooth = 4f;
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
        if (!Universe.player.boarded) { return; }
        
        float thrust = Input.GetAxis("Spaceship Thrust");
        float strafe = Input.GetAxis("Spaceship Strafe");
        float lift = Input.GetAxis("Spaceship Lift");

        Vector3 newMovementVector = new(strafe, lift, thrust);
        Debug.Log("New: " + newMovementVector);
        oldMovementVector = Vector3.Lerp(oldMovementVector, newMovementVector, Time.deltaTime * accelerationSpeed);
        Debug.Log("Old: " + oldMovementVector);
        
        if (Input.GetMouseButton(1))
        {
            speed = Mathf.Lerp(speed, accelerationSpeed, Time.deltaTime * 3);
        }
        else
        {
            speed = Mathf.Lerp(speed, normalSpeed, Time.deltaTime * 10);
        }

        //Set moveDirection to the vertical axis (up and down keys) * speed
        //Vector3 moveDirection = new Vector3(strafe, lift, thrust) * speed;
        
        Vector3 moveDirection = oldMovementVector * speed;
        Debug.Log("SKUMMA: " + moveDirection);
        
        //Transform the vector3 to local space
        moveDirection = transform.TransformDirection(moveDirection);
        //Set the velocity, so you can move
        physicsBody.velocity = new Vector3(moveDirection.x, moveDirection.y, moveDirection.z);

        //Camera follow
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraPosition.position, Time.deltaTime * cameraSmooth);
        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraPosition.rotation, Time.deltaTime * cameraSmooth);

        //Rotation

        float rotationZTmp = Input.GetAxis("Spaceship Roll");

        
        mouseXSmooth = Mathf.Lerp(mouseXSmooth, Input.GetAxis("Horizontal Look") * rotationSpeed, Time.deltaTime * cameraSmooth);
        mouseYSmooth = Mathf.Lerp(mouseYSmooth, Input.GetAxis("Vertical Look") * rotationSpeed, Time.deltaTime * cameraSmooth);
        
        Quaternion localRotation = Quaternion.Euler(mouseYSmooth, mouseXSmooth, rotationZTmp * rotationSpeed);
        lookRotation = lookRotation * localRotation;
        transform.rotation = lookRotation;
        
        rotationZ -= mouseXSmooth;
        rotationZ = Mathf.Clamp(rotationZ, -45, 45);
        spaceshipRoot.transform.localEulerAngles = new Vector3(defaultShipRotation.x, defaultShipRotation.y, rotationZ);
        rotationZ = Mathf.Lerp(rotationZ, defaultShipRotation.z, Time.deltaTime * cameraSmooth);

        //Update crosshair texture
        if (crosshairTexture)
        {
            crosshairTexture.position = mainCamera.WorldToScreenPoint(transform.position + transform.forward * 100);
        }
    }
}
