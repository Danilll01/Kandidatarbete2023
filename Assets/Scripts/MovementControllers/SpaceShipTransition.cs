using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceShipTransition : MonoBehaviour
{
    // Ship stuff
    [Header("Ship stuff")]
    [SerializeField] private Camera shipCamera;

    // Player stuff
    private PillPlayerController player;
    
    // Transition
    private bool transitioning = false;
    private float transitionProgress = 0;
    
    // Positions / rotations
    private Vector3 transitionFromPos = Vector3.zero;
    private Quaternion transitionFromRot = Quaternion.identity;
    private Vector3 transitionToPos = Vector3.zero;
    private Quaternion transitionToRot = Quaternion.identity;
    private readonly Vector3 dismountedPos = new Vector3(-2.6f, 2, -2f);
    [SerializeField] private Transform mountedPos;
    
    // Landing
    [Header("Landing stuff")]
    [SerializeField] private float maxLandingAngle = 20f;
    [SerializeField] private float landingTime = 1f;
    private bool shouldDisembark = false;
    
    // Audio
    [Header("Audio stuff")]
    private AudioSource audioPlayer;
    [SerializeField] private AudioClip errorSound;
    
    /// <summary>
    /// Initializes ship transition script
    /// </summary>
    public void Initialize()
    {
        player = Universe.player;

        transform.position = player.transform.position;
        transform.rotation = player.transform.rotation;
        EmbarkInShip();
        transitionToPos = transform.position;
        transitionToRot = transform.rotation;
        transitionProgress = float.MaxValue;
        HandleTransition();
    }

    /// <summary>
    /// Returns a bool if the ship is currently in transition face
    /// </summary>
    /// <returns>The transition bool</returns>
    public bool UnderTransition()
    {
        return transitioning;
    }

    // The local up vector of ship compared to current planet
    private Vector3 LocalUp()
    {
        return Universe.player.attractor == null ? Vector3.zero : (transform.position - transform.parent.position).normalized;
    }
    
    // Handles ship transition. Taken from original ship controller
    void Update()
    {

        if (transitioning)
        {
            HandleTransition();
        }
        else
        {
            CheckBoardStatus();
        }
    }

    // Checks if it is time to transition to or from ship
    private void CheckBoardStatus()
    {

        // If transition is not starter, return 
        if (!Input.GetKeyDown(KeyCode.F)) return;
        
        // Transition is started
        if (Universe.player.boarded)
        {
            if (GetLandingSpot(out (Vector3 position, Quaternion rotation) landingTarget))
            {
                //Set up transition to/from
                transitionFromPos = transform.localPosition;
                transitionFromRot = transform.localRotation;
                transitionToPos = landingTarget.position;
                transitionToRot = landingTarget.rotation;
                transitioning = true;
                shouldDisembark = true;
                
                //Transition method handles moving the player out of the ship
            }
        }
        else
        {
            //Get raised spot
            GetTakeoffSpot(out (Vector3 position, Quaternion rotation) takeoffTarget);

            //Set up transition to/from
            transitionFromPos = transform.localPosition;
            transitionFromRot = transform.localRotation;
            transitionToPos = takeoffTarget.position;
            transitionToRot = takeoffTarget.rotation;
            
            //Embark
            //Move player into ship
            EmbarkInShip();
            
            transitioning = true;
        }
    }
    
    private void HandleTransition()
    {
        // For now basic linear interpolation
        transitionProgress += Time.deltaTime;

        transform.position = Vector3.Lerp(transitionFromPos, transitionToPos, transitionProgress / landingTime);
        transform.rotation = Quaternion.Lerp(transitionFromRot, transitionToRot, transitionProgress / landingTime);

        if (transitionProgress / landingTime >= 1)
        {
            transitioning = false;
            if (shouldDisembark)
            {
                DisembarkFromShip();
                shouldDisembark = false;
            }
            transitionProgress = 0;
        }
    }
    
    private void EmbarkInShip()
    {
        Transform playerTransform = player.transform;
        
        playerTransform.SetParent(mountedPos);
        playerTransform.localPosition = Vector3.zero;
        playerTransform.localRotation = Quaternion.identity;

        player.ShipPlayerTransition();
        shipCamera.enabled = true;

    }
    
    private void GetTakeoffSpot(out (Vector3 position, Quaternion rotation) takeoffSpot)
    {
        takeoffSpot.position = transform.localPosition + LocalUp() * 10;
        takeoffSpot.rotation = Gravity.UprightRotation(transform, player.attractor.transform);
    }
    
    private void DisembarkFromShip()
    {
        Transform shipTransform = transform;
        Transform playerTransform = player.transform;
        
        shipTransform.SetParent(player.attractor.transform);
        
        playerTransform.position = shipTransform.position + (shipTransform.rotation * dismountedPos);
        playerTransform.rotation = shipTransform.rotation;

        shipCamera.enabled = false;
        player.ShipPlayerTransition();
    }
    
    private bool GetLandingSpot(out (Vector3 position, Quaternion rotation) landingSpot)
    {
        Vector3[] gearPositions = {
            new Vector3(2.7f, 0f, -4f),   //Right back
            new Vector3(-2.7f, 0f, -4f),  //Left back
            new Vector3(0f, 0.16f, 10f)     //Front
        };
        Vector3 altFrontPosition = new Vector3(0f, 0.16f, 3f);
        LayerMask planetMask = 1 << (LayerMask.NameToLayer("Planet"));

        landingSpot = (Vector3.zero, Quaternion.identity);
        

        //Convert all gearPositions to their landed coordinates
        Vector3[] gearLandingPositions = new Vector3[gearPositions.Length];
        for (int i = 0; i < gearPositions.Length; i++)
        {
            Vector3 gearPosWorld = transform.TransformPoint(gearPositions[i]);
            Physics.Raycast(gearPosWorld, -LocalUp(), out RaycastHit hit, 20, planetMask);

            if (hit.collider != null)
            {
                gearLandingPositions[i] = hit.point;
            }
            else
            {
                audioPlayer.PlayOneShot(errorSound);
                return false;
            }
        }

        //Assumes 3 gearPositions
        //Create plane which the ship will land on
        Plane landingPlane = new Plane(gearLandingPositions[0], gearLandingPositions[1], gearLandingPositions[2]);
        //Makes sure landing is upright
        if (Vector3.Dot(landingPlane.normal, LocalUp()) < 0)
        {
            landingPlane.Flip();
        }
        //Check if middle is too far into ground
        Physics.Raycast(transform.TransformPoint(altFrontPosition), -LocalUp(), out RaycastHit altHit, 20, planetMask);
        if (landingPlane.GetDistanceToPoint(altHit.point) > 0)
        {
            landingPlane = new Plane(gearLandingPositions[0], gearLandingPositions[1], altHit.point);
            //Makes sure new landing is upright
            if (Vector3.Dot(landingPlane.normal, LocalUp()) < 0)
            {
                landingPlane.Flip();
            }
        }
        //Check if landing angle is allowed
        if (Vector3.Angle(landingPlane.normal, LocalUp()) > maxLandingAngle)
        {
            audioPlayer.PlayOneShot(errorSound);
            return false;
        }

        //Land ship and calculate offsets
        landingPlane.Raycast(new Ray(transform.position, -LocalUp()), out float height);
        Vector3 landingPos = transform.localPosition + (-LocalUp() * height);

        //Set up transition to/from
        landingSpot.position = transform.parent.transform.InverseTransformPoint(landingPos);
        landingSpot.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.TransformVector(Vector3.forward), landingPlane.normal), landingPlane.normal);

        return true;
    }
}
