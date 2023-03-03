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
    private Vector3 mountedPos = new Vector3(0, 1.6f, -1.4f);
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

        player.transform.position = Vector3.Lerp(transitionFromPos, transitionToPos, transitionProgress);
        player.transform.rotation = Quaternion.Lerp(transitionFromRot, transitionToRot, transitionProgress);

        if (transitionProgress >= 1)
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
        //Left planet and should no longer hold altitude
        if (shipHoldingUprightRotation && (holdingOverPlanet != player.Planet))
        {
            shipHoldingUprightRotation = false;
        }

        //TODO check ability to board. Will do this after the ability to lose ship is mitigated
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (boarded)
            {
                //Disembark
                Physics.Raycast(player.transform.position, -player.Up, out RaycastHit hit, 20, 1 << (LayerMask.NameToLayer("Planet")));

                if (hit.collider != null)
                {
                    //Set up transition to/from
                    transitionFromPos = player.transform.position;
                    transitionFromRot = player.transform.rotation;
                    transitionToPos = hit.point - Quaternion.FromToRotation(Vector3.up, player.Up) * Vector3.up * transform.localPosition.y;
                    transitionToRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.TransformVector(Vector3.forward), hit.normal), hit.normal);
                    transitioning = true;
                    //Transition method handles moving the player out of the ship
                }
            }
            else
            {
                //Embark
                //Move player into ship
                EmbarkInShip();

                //Set up transition to/from
                transitionFromPos = player.transform.position;
                transitionFromRot = player.transform.rotation;
                transitionToPos = transitionFromPos + player.Up * 10;
                transitionToRot = Gravity.UprightRotation(player.transform, player.Planet.transform);
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
                shipHoldingAltitude = Vector3.Distance(player.Planet.transform.position, player.transform.position);
            }
        }
        //Rotation
        float pitch = Input.GetAxis("Controller Vertical Look");
        float yaw = Input.GetAxis("Controller Horizontal Look");
        float roll = Input.GetAxis("Spaceship Roll");
        player.transform.Rotate(new Vector3(pitch, yaw, roll) * Time.deltaTime * shipRotationSpeed);
        if (shipHoldingUprightRotation)
        {
            Gravity.KeepUpright(player.transform, player.Planet.transform);
            player.transform.position = player.transform.position / (player.Altitude / shipHoldingAltitude);

            //This may lead to slowly slipping away from planet. Hasn't noticed so maybe so minute that it may be ignored :)
            Vector3 velocity = player.transform.InverseTransformDirection(body.velocity);
            velocity.y = 0;
            body.velocity = player.transform.TransformDirection(velocity);

            //Not moving up/down. Hold altitude
            if (Input.GetAxisRaw("Spaceship Lift") != 0)
            {
                shipHoldingAltitude += Input.GetAxis("Spaceship Lift") * shipMovespeed * Time.deltaTime;
            }
            player.transform.rotation *= Quaternion.Euler(30, 0, 0);

            holdingOverPlanet = player.Planet;
        }
        //Translation
        float strafe = Input.GetAxis("Spaceship Strafe");
        float lift = Input.GetAxis("Spaceship Lift");
        float thrust = Input.GetAxis("Spaceship Thrust");
        body.velocity += transform.rotation * new Vector3(strafe, lift, thrust) * Time.deltaTime * shipMovespeed;
        //Slowdown due to being inside of a planet
        //TODO. Maybe integrate with actual air resistance
        if (player.Planet != null)
        {
            float altitudeFactor = player.Altitude / player.Planet.radius;
            float divideFactor = altitudeFactor / (altitudeFactor + 1);
            body.velocity /= divideFactor * Time.deltaTime + 1;
        }
    }

    private void DisembarkFromShip()
    {
        transform.SetParent(player.Planet.gameObject.transform);
        player.transform.position = transform.position + (transform.rotation * dismountedPos);
        player.transform.rotation = transform.rotation;
    }

    private void EmbarkInShip()
    {
        player.transform.position = transform.position + (transform.rotation * mountedPos);
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
