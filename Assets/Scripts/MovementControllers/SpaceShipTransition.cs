using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceShipTransition : MonoBehaviour
{
    
    private bool transitioning = false;
    [HideInInspector] public bool boarded = false;
    
    private Vector3 transitionFromPos = Vector3.zero;
    private Quaternion transitionFromRot = Quaternion.identity;
    private Vector3 transitionToPos = Vector3.zero;
    private Quaternion transitionToRot = Quaternion.identity;
    
    private PillPlayerController player;
    [SerializeField] private float maxLandingAngle = 20f;
    private float transitionProgress = 0;
    [SerializeField] private float landingTime = 1f;
    private readonly Vector3 dismountedPos = new Vector3(-2.6f, 2, -2f);
    
    private AudioSource audioPlayer;
    [SerializeField] private AudioClip errorSound;
    
    // Start is called before the first frame update
    void Start()
    {
        player = Universe.player;
    }

    // Handles ship transition. Taken from original ship controller
    
    // Update is called once per frame
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
        // More efficient code
        Transform playerTransform = player.transform;
        
        //Embark / Disembark
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (boarded)
            {
                if (GetLandingSpot(out (Vector3 position, Quaternion rotation) landingTarget))
                {
                    //Set up transition to/from
                    transitionFromPos = playerTransform.localPosition;
                    transitionFromRot = playerTransform.localRotation;
                    transitionToPos = landingTarget.position;
                    transitionToRot = landingTarget.rotation;
                    transitioning = true;
                    //Transition method handles moving the player out of the ship
                }
            }
            else
            {
                //Embark
                //Move player into ship
                EmbarkInShip();

                //Get raised spot
                GetTakeoffSpot(out (Vector3 position, Quaternion rotation) takeoffTarget);

                //Set up transition to/from
                transitionFromPos = playerTransform.localPosition;
                transitionFromRot = playerTransform.localRotation;
                transitionToPos = takeoffTarget.position;
                transitionToRot = takeoffTarget.rotation;
                transitioning = true;
                player.boarded = true;
            }
        }
    }
    
    private void HandleTransition()
    {
        // For now basic linear interpolation
        transitionProgress += Time.deltaTime;

        player.transform.localPosition = Vector3.Lerp(transitionFromPos, transitionToPos, transitionProgress / landingTime);
        player.transform.rotation = Quaternion.Lerp(transitionFromRot, transitionToRot, transitionProgress / landingTime);

        if (transitionProgress / landingTime >= 1)
        {
            transitioning = false;
            if (boarded)
            {
                DisembarkFromShip();
            }
            transitionProgress = 0;
            Boarded = !boarded;
        }
    }
    
    private void EmbarkInShip()
    {
        //player.transform.position = transform.position + (transform.rotation * mountedPos.localPosition);
        player.transform.rotation = transform.rotation;
        //camera.transform.localRotation = Quaternion.identity;
        //body.velocity = Vector3.zero;
        transform.SetParent(player.transform);
    }
    
    private void GetTakeoffSpot(out (Vector3 position, Quaternion rotation) takeoffSpot)
    {
        takeoffSpot.position = player.transform.localPosition + player.Up * 10;
        takeoffSpot.rotation = Gravity.UprightRotation(player.transform, player.Planet.transform);
    }
    
    private void DisembarkFromShip()
    {
        transform.SetParent(player.Planet.gameObject.transform);
        player.transform.position = transform.position + (transform.rotation * dismountedPos);
        player.transform.rotation = transform.rotation;
        //body.velocity = Vector3.zero;
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


        Transform playerTransform = player.transform;

        //Convert all gearPositions to their landed coordinates
        Vector3[] gearLandingPositions = new Vector3[gearPositions.Length];
        for (int i = 0; i < gearPositions.Length; i++)
        {
            Vector3 gearPosWorld = transform.TransformPoint(gearPositions[i]);
            Physics.Raycast(gearPosWorld, -player.Up, out RaycastHit hit, 20, planetMask);

            if (hit.collider != null)
            {
                gearLandingPositions[i] = hit.point;
                continue;
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
        if (Vector3.Dot(landingPlane.normal, player.Up) < 0)
        {
            landingPlane.Flip();
        }
        //Check if middle is too far into ground
        Physics.Raycast(transform.TransformPoint(altFrontPosition), -player.Up, out RaycastHit altHit, 20, planetMask);
        if (landingPlane.GetDistanceToPoint(altHit.point) > 0)
        {
            landingPlane = new Plane(gearLandingPositions[0], gearLandingPositions[1], altHit.point);
            //Makes sure new landing is upright
            if (Vector3.Dot(landingPlane.normal, player.Up) < 0)
            {
                landingPlane.Flip();
            }
        }
        //Check if landing angle is allowed
        if (Vector3.Angle(landingPlane.normal, player.Up) > maxLandingAngle)
        {
            audioPlayer.PlayOneShot(errorSound);
            return false;
        }

        //Land ship and calculate offsets
        landingPlane.Raycast(new Ray(playerTransform.position, -player.Up), out float height);
        Vector3 landingPos = playerTransform.position + (-player.Up * height);
        Vector3 playerPositionOffset = Quaternion.FromToRotation(Vector3.up, player.Up) * (Vector3.up * transform.localPosition.y * transform.parent.localScale.y);

        //Set up transition to/from
        landingSpot.position = player.Planet.transform.InverseTransformPoint(landingPos - playerPositionOffset);
        landingSpot.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.TransformVector(Vector3.forward), landingPlane.normal), landingPlane.normal);

        return true;
    }
    
    private bool Boarded
    {
        set
        {
            player.boarded = value;
            boarded = value;
        }
        get { return boarded; }
    }
}
