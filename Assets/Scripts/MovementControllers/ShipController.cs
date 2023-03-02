using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private float shipMovespeed;
    [SerializeField] private float shipRotationSpeed;
    [HideInInspector] public bool boarded = false;
    private bool transitioning = false;
    private bool shipHoldingUprightRotation = false;
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

    public void Initialize(PillPlayerController player, Rigidbody body, Camera camera)
    {
        this.player = player;
        this.body = body;
        this.camera = camera;
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
        //TODO check ability to board
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shipHoldingUprightRotation = !shipHoldingUprightRotation;
        }
        //Rotation
        float pitch = Input.GetAxis("Mouse Y") * -1;
        float yaw = Input.GetAxis("Mouse X");
        float roll = Input.GetAxis("Spaceship Roll");
        player.transform.Rotate(new Vector3(pitch, yaw, roll) * Time.deltaTime * shipRotationSpeed);
        if (shipHoldingUprightRotation)
        {
            Gravity.KeepUpright(player.transform, player.Planet.transform);
        }
        //Translation
        float strafe = Input.GetAxis("Spaceship Strafe");
        float lift = Input.GetAxis("Spaceship Lift");
        float thrust = Input.GetAxis("Spaceship Thrust");
        body.velocity += transform.rotation * new Vector3(strafe, lift, thrust) * Time.deltaTime * shipMovespeed;
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
