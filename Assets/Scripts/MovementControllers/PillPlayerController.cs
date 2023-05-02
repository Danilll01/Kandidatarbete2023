using UnityEngine;
using ExtendedRandom;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class PillPlayerController : MonoBehaviour
{
    public Planet attractor = null;
    public Camera firstPersonCamera;
    [SerializeField] private PlayerWater playerWater;
    private Rigidbody body;
    [HideInInspector] public bool paused;

    [Header("Movement")]
    public float movementSpeed;
    [SerializeField] private float sprintFactor;
    public float airControlFactor;
    public float jumpForce;
    private int coyoteTimer = 0;
    [SerializeField] private int coyoteMax;
    [SerializeField] private float swimForce;
    [SerializeField] private float maxSwimSpeed = 10;
    public float maxSpeed;
    private bool jump = false; // Used for creating a rising trigger for jump
    private bool isSprinting = false;

    [Header("Ship")]
    private ShipController ship;
    [HideInInspector] public bool boarded = false;

    // Animations
    private Animator animator;
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Direction = Animator.StringToHash("Direction");
    private Transform animationRig;
    
    [Header("Camera")]
    [SerializeField] [Range(0.2f, 5f)] private float mouseSensitivity = 1f;
    [SerializeField] private float lookLimitAngle = 80f;
    private float pitch = 0f;
    private static readonly int Swim = Animator.StringToHash("Swim");
    private static readonly int Jump = Animator.StringToHash("Jump");

    private void Awake()
    {
        Universe.player = this;
        animator = transform.GetChild(0).GetComponent<Animator>();
        animationRig = transform.GetChild(0);
    }

    // Start is called before the first frame update
    public void Initialize(Planet planetToSpawnOn, int seed)
    {
        Universe.player = this;

        if (attractor == null)
        {
            attractor = planetToSpawnOn;
            if (attractor == null)
            {
                Debug.LogError("Player spawned without a planet");
            }
        }

        body = GetComponent<Rigidbody>();
        ship = GameObject.Find("Spaceship").GetComponent<ShipController>();
        Spawn(planetToSpawnOn, seed);
        //Lock the mouse inside of the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        paused = false;

        playerWater.Initialize(attractor);
    }

    // Update is called once per frame
    void Update()
    {       
        if (!paused)
        {
            if (!boarded)
            {
                HandleMovement();
                HandleCamera();
            }
            if (attractor != null)
            {
                if (!ReferenceEquals(attractor, playerWater.planet)) playerWater.UpdatePlanet(attractor);
                playerWater.UpdateWater(transform.position);
            }
        }
        
        #if DEBUG || UNITY_EDITOR
        if (attractor != null)
        {
            DisplayDebug.AddOrSetDebugVariable("Current planet", attractor.bodyName);
            DisplayDebug.AddOrSetDebugVariable("Planet radius", attractor.radius.ToString());
            DisplayDebug.AddOrSetDebugVariable("Planet mass", attractor.mass.ToString());
            DisplayDebug.AddOrSetDebugVariable("Planet surface gravity", attractor.surfaceGravity.ToString());
            BiomeValue currentBiome = Biomes.EvaluteBiomeMap(attractor.Biome, transform.position, attractor.DistanceToSun);
            DisplayDebug.AddOrSetDebugVariable("Biome: Mountain", currentBiome.mountains.ToString());
            DisplayDebug.AddOrSetDebugVariable("Biome: Temperature", currentBiome.temperature.ToString());
            DisplayDebug.AddOrSetDebugVariable("Biome: Trees", currentBiome.trees.ToString());
        }
        else
        {
            DisplayDebug.AddOrSetDebugVariable("Current planet", "Sun");
            DisplayDebug.AddOrSetDebugVariable("Planet radius", "N/A");
            DisplayDebug.AddOrSetDebugVariable("Planet mass", "N/A");
            DisplayDebug.AddOrSetDebugVariable("Planet surface gravity", "N/A");
            DisplayDebug.AddOrSetDebugVariable("Biome: Mountain", "N/A");
            DisplayDebug.AddOrSetDebugVariable("Biome: Temperature", "N/A");
            DisplayDebug.AddOrSetDebugVariable("Biome: Trees", "N/A");
        }
        #endif
    }

    private void FixedUpdate()
    {
        coyoteTimer = Grounded ? 0 : coyoteTimer + 1;
    }
    private void HandleCamera()
    {
        //Rotate player and camera
        //Vector3 cameraRotationVector = new Vector3(Input.GetAxis("Vertical Look") + Input.GetAxisRaw("Controller Vertical Look") * 3, 0);
        Vector3 playerRotationVector = new Vector3(0, (mouseSensitivity * Input.GetAxis("Horizontal Look")) + Input.GetAxisRaw("Controller Horizontal Look") * 4);
        pitch += (mouseSensitivity * Input.GetAxis("Vertical Look")) + (Input.GetAxisRaw("Controller Vertical Look") * 3);
        
        // Clamp pitch between lookAngle
        pitch = Mathf.Clamp(pitch, -lookLimitAngle, lookLimitAngle);
        
        transform.Rotate(playerRotationVector);
        firstPersonCamera.transform.localEulerAngles = new Vector3(pitch, 0 ,0);
    }

    // Moves model back or forward depending on player speed
    private void MoveModelWhileSprint(float speed)
    {
        if(speed > 0.5f)
        {
            if (isSprinting) return;
            isSprinting = true;
            animationRig.localPosition += new Vector3(0,0,-0.015f);
        }
        else
        {
            if (!isSprinting) return;
            isSprinting = false;
            animationRig.localPosition += new Vector3(0,0,0.015f);
        }
        
    }
    private void HandleMovement()
    {
        //Keep old Y velocity. Rotates to world space, grabs y velocity and rotates back to planet orientation
        Vector3 yGround = Grounded ? GroundNormal : transform.rotation * transform.up;
        Vector3 oldY = Vector3.Project(body.velocity, yGround);
        
        //New movement
        float inputDirection = Input.GetAxisRaw("Horizontal");
        float inputSpeed = Input.GetAxisRaw("Vertical");
        Vector3 movementVector = new Vector3(inputDirection, 0, inputSpeed) * movementSpeed;
        
        // Used for animations
        float direction = Mathf.Min(Mathf.Abs(inputDirection), 0.5f);
        float speed = Mathf.Min(Mathf.Abs(inputSpeed), 0.5f);   
        
        if (Input.GetAxisRaw("Sprint") == 1) // Running
        {
            movementVector *= sprintFactor;
            
            // Animations
            speed = Mathf.Min(Mathf.Abs(inputSpeed),1);
            direction = Mathf.Min(Mathf.Abs(inputDirection),1); 
        }
        else if (Input.GetAxisRaw("Sprint") == -1) // Walking slow
        {
            movementVector /= sprintFactor;
            
            // Animations
            direction = Mathf.Min(Mathf.Abs(inputDirection), 0.3f);
            speed = Mathf.Min(Mathf.Abs(inputSpeed), 0.3f);
        }

        // Decides if the model should be moved back because of sprinting
        MoveModelWhileSprint(speed);
        
        //Swiming
        if (Swimming)
        {
            if (Input.GetAxisRaw("Jump") == 1)
            {
                movementVector.y += swimForce * 10 * Time.deltaTime;
            }
            else
            {
                movementVector.y += 0.0001f;
            }
        }
        else if (Input.GetAxisRaw("Jump") == 1 && coyoteTimer <= coyoteMax) //Jumping
        {
            jump = true;
        }
        else if (!Grounded || Input.GetAxisRaw("Jump") == 0) //Resets the jump when jump is released or left the ground
        {
            jump = false;
        }
        if (jump)
        {
            movementVector.y = jumpForce;
        }

        //Input received
        if (movementVector.magnitude != 0)
        {
            //Ground controls + swim controls
            if (Swimming)
            {
                float currentUppSpeed = (Quaternion.Inverse(transform.rotation) * body.velocity).y + movementVector.y;

                if (Mathf.Abs(currentUppSpeed) > maxSwimSpeed)
                {
                    movementVector.y = maxSwimSpeed * Mathf.Sign(currentUppSpeed);
                }
                else
                {
                    movementVector.y = currentUppSpeed;
                }
                body.velocity = transform.rotation * movementVector;


            }
            else if (Grounded)
            {
                movementVector = transform.rotation * movementVector;

                //Remove any movement that would make the player leave the ground unless the player is jumping
                if (!jump)
                {
                    movementVector -= Vector3.Project(movementVector, yGround);
                }
                body.velocity = movementVector;
            }
            //Air controls
            else
            {
                //Add movement
                body.velocity += transform.rotation * movementVector * (Time.deltaTime * airControlFactor);
                //Normalize to maxSpeed if necessary
                Vector3 oldVelocity = (Quaternion.Inverse(transform.rotation) * body.velocity);
                Vector3 oldHorizontalVelocity = new Vector3(oldVelocity.x, 0, oldVelocity.z);
                if (oldHorizontalVelocity.magnitude > maxSpeed)
                {
                    Vector3 newHorizontalVelocity = oldHorizontalVelocity.normalized * maxSpeed;
                    Vector3 newVelocity = oldVelocity - oldHorizontalVelocity + newHorizontalVelocity;
                    body.velocity = transform.rotation * newVelocity;
                }
            }
        }
        //No input
        else if (Grounded)
        {
            Vector3 velocity = Vector3.zero;
            velocity += oldY;
            body.velocity = velocity;
        }

        // Sets animation state
        animator.SetFloat(Speed, Mathf.Sign(inputSpeed) * speed);
        animator.SetFloat(Direction, Mathf.Sign(inputDirection) * direction);  
        
        if (boarded) return;
        Gravity.KeepUpright(transform, attractor.transform);
        Gravity.Attract(transform.position, body, attractor.transform.position, attractor.mass);
    }

    private void Spawn(Planet planet, int seed)
    {
        RandomX rand = new RandomX(seed);
        Vector3 spawnLocationAbovePlanet = planet.transform.position + (rand.OnUnitSphere() * planet.radius * 1.15f);
        transform.position = spawnLocationAbovePlanet;
        transform.LookAt(planet.transform);
        ship.Initialize(body, firstPersonCamera);
    }

    public Planet Planet
    {
        get { return attractor; }
        set { attractor = value; }
    }

    /// <summary>
    /// The altitude of the player from the currently attracting planet.
    /// </summary>
    public float Altitude
    {
        get { return (attractor.transform.position - transform.position).magnitude; }
    }


    /// <summary>
    /// The Vector3 of the normal ground the player is standing on. Returns Vector3.Zero if not on ground.
    /// </summary>
    public Vector3 GroundNormal
    {
        get
        {
            if (attractor.Equals(null))
            {
                return Vector3.zero;
            }


            Physics.Raycast(transform.position, attractor.transform.position - transform.position, out RaycastHit hit, 0.75f);
            return hit.collider == null ? Vector3.zero : hit.normal;
        }
    }


    public bool Grounded
    {
   
        get 
        { 
            if (attractor == null)
            {
                return false;
            }
            
            bool isGrounded = Physics.Raycast(transform.position, attractor.transform.position - transform.position, 0.75f);
            animator.SetBool(Jump, !isGrounded); // Sets animation
            return isGrounded;      
        }
    }

    public Vector3 Up
    {
        get { return (transform.position - attractor.transform.position).normalized; }
    }

    private bool Swimming
    {
        get
        {
            animator.SetBool(Swim, playerWater.underWater); // Sets animation
            return playerWater.underWater;
        }
    }

}
