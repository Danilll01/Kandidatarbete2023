using System.Linq;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private float shipMovespeed;
    [SerializeField] private float shipRotationSpeed;
    [HideInInspector] public bool boarded = false;
    private bool transitioning = false;
    private bool shipHoldingUprightRotation = false;
    private Planet holdingOverPlanet = null;
    private float shipHoldingAltitude;
    [SerializeField] private Transform mountedPos;
    private Vector3 dismountedPos = new Vector3(-2.6f, 2, -2f);

    [SerializeField] private float landingTime;
    private float transitionProgress = 0;
    private Vector3 transitionFromPos = Vector3.zero;
    private Quaternion transitionFromRot = Quaternion.identity;
    private Vector3 transitionToPos = Vector3.zero;
    private Quaternion transitionToRot = Quaternion.identity;
    private PillPlayerController player;
    private Rigidbody body;
    private new Camera camera;

    public void Initialize(Rigidbody body, Camera camera)
    {
        player = Universe.player;
        this.body = body;
        this.camera = camera;

        //Place ship next to player and make ship child to planets
        Physics.Raycast(player.transform.position, player.attractor.transform.position - player.transform.position, out RaycastHit hit, 20, 1 << (LayerMask.NameToLayer("Planet")));
        transform.position = hit.point;
        transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.TransformVector(Vector3.forward), hit.normal), hit.normal);
        transform.position += transform.TransformDirection(Vector3.right * 5);
        transform.SetParent(player.Planet.transform);

        // If mounted pos transform is not set in editor it will grab the object at least
        if (mountedPos.Equals(null))
        {
            mountedPos = transform.GetChild(0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (transitioning)
        {
            HandleTransition();
        }
        else
        {
            HandleShip();
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

    private void HandleShip()
    {
        // More efficient code
        Transform playerTransform = player.transform;
        
        //Left planet and should no longer hold altitude
        if (shipHoldingUprightRotation && (holdingOverPlanet != player.Planet))
        {
            shipHoldingUprightRotation = false;
        }

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

        //Controling ship
        if (!boarded)
        {
            return;
        }
        //Controls
        if (Input.GetKeyDown(KeyCode.Space) && player.Planet != null)
        {
            holdingOverPlanet = player.Planet;
            shipHoldingUprightRotation = !shipHoldingUprightRotation;
            if (shipHoldingUprightRotation)
            {
                shipHoldingAltitude = Vector3.Distance(player.Planet.transform.position, playerTransform.position);
            }
        }
        //Rotation
        float pitch = Input.GetAxis("Vertical Look");
        float yaw = Input.GetAxis("Horizontal Look");
        float roll = Input.GetAxis("Spaceship Roll");
        playerTransform.Rotate(new Vector3(pitch, yaw, roll) * (Time.deltaTime * shipRotationSpeed));
        if (shipHoldingUprightRotation)
        {
            playerTransform.localPosition = playerTransform.localPosition / (player.Altitude / shipHoldingAltitude);

            //This may lead to slowly slipping away from planet. Hasn't noticed so maybe so minute that it may be ignored :)
            Quaternion rot = playerTransform.rotation;
            Gravity.KeepUpright(playerTransform, player.Planet.transform);
            Vector3 velocity = playerTransform.InverseTransformDirection(body.velocity);
            velocity.y = 0;
            body.velocity = playerTransform.TransformDirection(velocity);
            playerTransform.rotation = rot;

            //Not moving up/down. Hold altitude
            if (Input.GetAxisRaw("Spaceship Lift") != 0)
            {
                shipHoldingAltitude += Input.GetAxis("Spaceship Lift") * shipMovespeed * Time.deltaTime;
            }

            holdingOverPlanet = player.Planet;
        }
        //Translation
        float strafe = Input.GetAxis("Spaceship Strafe");
        float lift = Input.GetAxis("Spaceship Lift");
        float thrust = Input.GetAxis("Spaceship Thrust");
        body.velocity += transform.rotation * new Vector3(strafe, lift, thrust) * (Time.deltaTime * shipMovespeed);
        //Slowdown due to being inside of a planet
        //TODO. Maybe integrate with actual air resistance
        if (player.Planet != null)
        {
            if (body.velocity.magnitude > shipMovespeed * 5)
            {
                body.velocity = body.velocity.normalized * (shipMovespeed * 5);
            }
            float divideFactor = 1.2f;
            if (strafe == 0 && lift == 0 && thrust == 0)
            {
                divideFactor = Mathf.Lerp(2f, 1.2f, body.velocity.magnitude / (shipMovespeed * 5));
            }
            DisplayDebug.AddOrSetDebugVariable("Slowdown factor:", divideFactor.ToString());
            body.velocity /= (divideFactor - 1) * Time.deltaTime + 1;
        }
        else
        {
            DisplayDebug.AddOrSetDebugVariable("Slowdown factor:", "N/A");
        }
    }

    private bool GetLandingSpot(out (Vector3 position, Quaternion rotation) landingSpot)
    {
        //TODO make sure entire beam is above ground
        Vector3[] gearPositions = { 
            new Vector3(2.7f, 0f, -1.8f),   //Right back
            new Vector3(-2.7f, 0f, -1.8f),  //Left back
            new Vector3(0f, 0.16f, 10f)     //Front
        };
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
                return false;
            }
        }

        //Assumes 3 gearPositions
        Plane landingPlane = new Plane(gearLandingPositions[0], gearLandingPositions[1], gearLandingPositions[2]);
        landingPlane.Raycast(new Ray(playerTransform.position, -player.Up), out float height);
        Vector3 landingPos = playerTransform.position + (-player.Up * height);
        Vector3 playerPositionOffset = Quaternion.FromToRotation(Vector3.up, player.Up) * (Vector3.up * transform.localPosition.y);


        //Set up transition to/from
        landingSpot.position = player.Planet.transform.InverseTransformPoint(landingPos - playerPositionOffset);
        landingSpot.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.TransformVector(Vector3.forward), landingPlane.normal), landingPlane.normal);

        //Makes sure landing is upright
        if (Vector3.Dot(landingPlane.normal, player.Up) < 0)
        {
            landingSpot.rotation = Quaternion.Euler(180, 0, 0) * landingSpot.rotation;
        }

        return true;
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
    }

    private void EmbarkInShip()
    {
        player.transform.position = transform.position + (transform.rotation * mountedPos.localPosition);
        player.transform.rotation = transform.rotation;
        camera.transform.localRotation = Quaternion.identity;
        body.velocity = Vector3.zero;
        transform.SetParent(player.transform);
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
