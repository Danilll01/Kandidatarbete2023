using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class Creature : MonoBehaviour
{

    [SerializeField] private float speed = 1f;
    [SerializeField] private float detectionRadius = 30f;
    [SerializeField] private float consumeRadius = 0.5f;

    [SerializeField] private GameObject meshObj;

    [Header("Creature food and water needs")]
    [SerializeField] private CreatureType creatureType = CreatureType.Small;
    
    [SerializeField] public float hunger = 100f;
    [SerializeField] public float thirst = 100f;
    [SerializeField] private bool randomizeStats = true;

    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float maxThirst = 100f;

    [SerializeField] private float hungerThreshold = 80f;
    [SerializeField] private float thirstThreshold = 80f;

    [SerializeField] private float hungerDecrease = 0.1f;
    [SerializeField] private float thirstDecrease = 0.1f;
    
    [SerializeField] private float hungerIncrease = 20f;
    [SerializeField] private float thirstIncrease = 20f;

    [Header("Diet")]
    [SerializeField] private ResourceType[] resourceTypes;
    [Header("If resource type == creature, select type of creature")]
    [SerializeField] private CreatureType creatureDiet;

    [Header("Reproduction")]
    [SerializeField] private GameObject childPrefab;
    [SerializeField] private GameObject parentPrefab;
    [SerializeField] private bool canReproduce = true;
    [SerializeField] public bool wantToReproduce = false;
    [SerializeField] private float reproductionThreshold = 80f;
    [SerializeField] private float reproductionCooldown = 100f;
    [SerializeField] private float reproductionTimer = 0f;
    [SerializeField] private float reproductionCost = 30f;
    [SerializeField, Range(0f, 1f)] private float reproductionChance = 0.5f;
    [SerializeField] private int maxChildren = 2;
    [SerializeField] private int childrenCount = 0;
    
    [SerializeField] private bool isChild = false;
    [SerializeField] private float growUpTime = 60f;

    [SerializeField] private GameObject breedingParticle;

    [Header("Sound")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.4f;
    [SerializeField] private AudioClip[] idleSounds;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] private AudioClip[] stepSounds;
    [SerializeField] private float timeBetweenIdleSounds = 12f;
    [SerializeField] private float stepsTimer = 0f;
    [SerializeField] private float timeBetweenSteps = 1f/3f;
    private float idleSoundTimer;

    // Genes: speed, size, stat decrease, detection radius
    [Header("Genes")]
    [SerializeField] private Genes genes;
    [SerializeField] private Texture alternativeTexture = null;
    [SerializeField, Range(0f, 1f)] private float alternativeTextureProb = 0.1f;

    [SerializeField, Range(0f, 1f)] private float speedMutationProb = 0.8f;
    [SerializeField, Range(0f, 1f)] private float speedMultiplierRange = 0.1f;

    [SerializeField, Range(0f, 1f)] private float sizeMutationProb = 0.4f;
    [SerializeField, Range(0f, 1f)] private float sizeMultiplierRange = 0.05f;

    [SerializeField, Range(0f, 1f)] private float statDecreaseMutationProb = 0.4f;
    [SerializeField, Range(0f, 1f)] private float statDecreaseMultiplierRange = 0.09f;

    [SerializeField, Range(0f, 1f)] private float detectionRadiusMutationProb = 0.5f;
    [SerializeField, Range(0f, 1f)] private float detectionRadiusMultiplierRange = 0.05f;

    private bool alternativeTextureActive = false;
    private bool genesInitialized = false;
    [Header("Misc")]
    [SerializeField] private bool hasDrinkAnimation = false;

    [Header("Debug")]
    [SerializeField] private CreatureState currentState;
    [SerializeField] private bool DEBUG = false;
    [SerializeField] private bool isSleeping;
    [SerializeField] private bool animate = true;


    private bool atDestination = false;
    [SerializeField] private Vector3 destination = Vector3.zero;

    private Planet planet;

    private new Collider collider;
    private new Rigidbody rigidbody;
    private LODGroup lodGroup;
    private new Renderer renderer;
    private Animator animator;
    private AnimatorParameterAdapter animatorParameters;
    private AudioSource audioSource;

    private Creature breedingPartner;
    public bool isDying = false;
    public GameObject gettingEatenBy = null;

    // Overlap sphere optimizations
    private static int foodLayerMask = (1 << 9);
    private static int creatureLayerMask = (1 << 10);
    private static int resourceLayerMask = foodLayerMask | creatureLayerMask;
    private int getResourceTickSkips = 20;
    private int getResourceTicks = 20;
    private GameObject lastResourceFound;
    private ResourceType lastResourceType;
    private Creature lastPartnerFound;
    private int getPartnerTicks = 0;
    private Vector3 foundWaterAt = Vector3.zero;
    private Collider lastGroundCollider;

    // Pre-allocated buffers for OverlapSphereNonAlloc
    private static readonly Collider[] overlapBuffer = new Collider[64];

    // Cached so that comparing tags and names doesn't allocate a new string every time
    private static readonly string[] resourceTags = System.Enum.GetNames(typeof(ResourceType));
    private static readonly int baseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly WaitForSeconds walkAnimationWait = new WaitForSeconds(0.1f);
    private static readonly WaitForSeconds afterActionWait = new WaitForSeconds(1f);
    private static int planetLayerMask;
    private string speciesName;
    private string parentPrefabName;
    private string childPrefabName;

    // Tries per frame to find a random walkable point before trying again next frame
    private const int randomPointTries = 10;

    // Creatures further from the camera than this are still drawn, but stand still
    private const float simulationDistance = 160f;
    private static int cameraFrame = -1;
    private static Vector3 cameraPosition;

    // Start is called before the first frame update
    void Start()
    {
        currentState = CreatureState.Walking;

        planet = transform.parent.parent.parent.parent.GetComponent<Planet>();

        collider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();
        lodGroup = meshObj.GetComponent<LODGroup>();
        renderer = lodGroup.transform.GetComponent<Renderer>();
        animator = GetComponent<Animator>();
        animatorParameters = new AnimatorParameterAdapter(animator);
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = volume;
        audioSource.spatialBlend = 1.0f;

        speciesName = gameObject.name;
        parentPrefabName = parentPrefab.name;
        childPrefabName = childPrefab.name;
        if (planetLayerMask == 0) planetLayerMask = 1 << LayerMask.NameToLayer("Planet");

        // Teleport the creature 1 meter up in correct direction based on position on planet
        transform.position += -(planet.transform.position - transform.position).normalized * 0.3f;

        // Randomize stats
        if (randomizeStats)
        {
            hunger = Random.Range(30, maxHunger);
            thirst = Random.Range(30, maxThirst);

            if (alternativeTexture != null && Random.value < alternativeTextureProb)
            {
                renderer.material.SetTexture(baseMapId, alternativeTexture);
                alternativeTextureActive = true;
            }
        }

        animatorParameters.SetFloat("Speed", speed);
        CreateGenes();

        animator.keepAnimatorStateOnDisable = true;
        //DONE TO IGNORE EVENTS FROM BOUGHT ASSETS, ACTUALLY JUST THE DAMN HORSE, HACK
        animator.fireEvents = false;
        animatorParameters.SetBool("isWalking", true);

        if (isChild) canReproduce = false;

        idleSoundTimer = timeBetweenIdleSounds + Random.Range(-2f,2f);
    }

    // Update is called once per frame
    void Update()
    {
        // If creature is not visible, or so far away that it's only a few pixels, dont perform physics, AI or animation updates
        if (!renderer.isVisible || IsFarFromCamera())
        {
            if (!isSleeping)
            {
                rigidbody.Sleep();
                animator.enabled = false;
            }
            isSleeping = true;
            return;
        } else if (isSleeping)
        {
            isSleeping = false;
            animator.enabled = true;
        }

        // If the creature is performing animation, dont do anything
        if (currentState == CreatureState.PerformingAction)
            return;

        // Update creatures state if needed
        UpdateCreatureState();

        // Act on current state of the creature
        switch (currentState)
        {
            case CreatureState.Idle:
                //Idle()
                break;
            case CreatureState.Walking:
                RandomWalking();
                break;
            case CreatureState.LookingForFood:
                LookingForFood();
                break;
            case CreatureState.LookingForWater:
                LookingForResource(ResourceType.Water);
                break;
            case CreatureState.LookingForPartner:
                LookingForPartner();
                break;
            case CreatureState.Breeding:
                Bredding();
                break;
        }

        // Decrease hunger and thirst
        hunger -= hungerDecrease * Time.deltaTime;
        thirst -= thirstDecrease * Time.deltaTime;

        // Die if hunger or thirst is 0
        if (hunger <= 0 || thirst <= 0)
        {
            Kill();
        }

        // Update all timers

        UpdateReproductionTimer();

        UpdateGrowthTimer();

        UpdateIdleSoundsTimer();

        UpdateStepSoundsTimer();
    }

    void FixedUpdate()
    {
        if (!isSleeping)
        {
            AttractToPlanet();
            Rotate();
        }
    }

    private bool IsFarFromCamera()
    {
        // Camera.main is looked up once per frame for all creatures
        if (cameraFrame != Time.frameCount)
        {
            cameraFrame = Time.frameCount;
            Camera camera = Camera.main;
            cameraPosition = camera != null ? camera.transform.position : transform.position;
        }

        // A little further to wake up again than to go to sleep, so creatures on the edge don't flicker between states
        float distance = isSleeping ? simulationDistance - 5f : simulationDistance;
        return (transform.position - cameraPosition).sqrMagnitude > distance * distance;
    }

    private void UpdateCreatureState()
    {
        // If hunger is below threshold, look for food
        if (hunger < hungerThreshold)
        {
            wantToReproduce = false;
            currentState = CreatureState.LookingForFood;
        }
        else if (thirst < thirstThreshold) // If thirst is below threshold, look for water
        {
            wantToReproduce = false;
            currentState = CreatureState.LookingForWater;
        }
        else if (canReproduce) 
        {
            // Dont try to find a new partner if ones already found
            if (currentState == CreatureState.Breeding) return;

            if (reproductionTimer <= 0 && hunger > reproductionThreshold && thirst > thirstThreshold)
            {
                wantToReproduce = true;
                currentState = CreatureState.LookingForPartner;
            }
        }
        else
        {
            currentState = CreatureState.Walking;
        }
    }

    private void CreateGenes()
    {
        if (genesInitialized) return;
        genes = new Genes();
        genes.alternaviteColor = alternativeTextureActive;
        genes.speed = speed;
        genes.statDecrease = Mathf.Max(thirstDecrease, hungerDecrease);
        genes.size = transform.localScale.x;
        genes.detectionRadius = detectionRadius;
    }

    public void ApplyGenes(Genes genes)
    {
        this.genes = genes;
        
        // Only apply alternative texture if i exists
        if (alternativeTexture != null && genes.alternaviteColor)
        {
            renderer.material.SetTexture(baseMapId, alternativeTexture);
        }
        speed = genes.speed;

        thirstDecrease = genes.statDecrease;
        hungerDecrease = genes.statDecrease;

        // Only use size property on non children
        if (!isChild)
        {
            transform.localScale = Vector3.one * genes.size;
        }

        detectionRadius = genes.detectionRadius;
        genesInitialized = true;
    }

    private Genes MixGenes(Genes otherGenes)
    {
        Genes newGenes = genes;

        // If breeding creatures have the same color the creature spawned has a higher chance of having that color. 
        if (alternativeTextureActive == otherGenes.alternaviteColor)
        {
            // Only set the color to true if alternative texture also is true. 
            newGenes.alternaviteColor = alternativeTextureActive && Random.value < 0.8f;
        } else
        {
            newGenes.alternaviteColor = alternativeTextureActive && Random.value < 0.5f;
        }
        
        newGenes.speed = genes.speed * GetMutationMultiplier(speedMutationProb, speedMultiplierRange);

        newGenes.statDecrease = genes.statDecrease * GetMutationMultiplier(statDecreaseMutationProb, statDecreaseMultiplierRange);

        newGenes.size = genes.size * GetMutationMultiplier(sizeMutationProb, sizeMultiplierRange);

        newGenes.detectionRadius = genes.detectionRadius * GetMutationMultiplier(detectionRadiusMutationProb, detectionRadiusMultiplierRange);

        return newGenes;
    }

    private void RandomWalking()
    {
        if (atDestination)
        {
            // Randomly walk around
            destination = GetRandomPoint();
            atDestination = false;
        } else
        {
            GotoPosition(destination);
        }
        
    }

    private void LookingForResource(ResourceType resource)
    {
        GameObject nearestResource = null;
        if (getResourceTicks <= 0)
        {
            // Unsubsribe to the last resource to enable other creatures to eat it
            if (lastResourceFound != null && resource == ResourceType.Creature) lastResourceFound.GetComponent<Creature>().gettingEatenBy = null;

            nearestResource = GetNearestGameobject(resource);
            
            // Set this object as the consumer of the nearest creature
            if (nearestResource != null && resource == ResourceType.Creature) nearestResource.GetComponent<Creature>().gettingEatenBy = gameObject;
            lastResourceFound = nearestResource;
            
            getResourceTicks = getResourceTickSkips;
        } else
        {
            nearestResource = lastResourceFound;
            getResourceTicks--;
        }
        
        Vector3 resourcePos = Vector3.zero;

        // Get position of nearest resource
        if (resource != ResourceType.Water)
        {
            if (nearestResource != null)
                resourcePos = nearestResource.transform.position;
        } else
        {
            // Dont try to find new water if water is already found
            resourcePos = foundWaterAt == Vector3.zero ? GetNearestWaterSource() : foundWaterAt;
        }

        ApproachResource(nearestResource, resourcePos, resource);
    }

    // Looks for every kind of food in the diet with a single overlap query
    private void LookingForFood()
    {
        GameObject nearestFood;
        if (getResourceTicks <= 0)
        {
            // Unsubsribe to the last resource to enable other creatures to eat it
            if (lastResourceFound != null && lastResourceType == ResourceType.Creature) lastResourceFound.GetComponent<Creature>().gettingEatenBy = null;

            nearestFood = GetNearestFood(out lastResourceType);

            // Set this object as the consumer of the nearest creature
            if (nearestFood != null && lastResourceType == ResourceType.Creature) nearestFood.GetComponent<Creature>().gettingEatenBy = gameObject;
            lastResourceFound = nearestFood;

            getResourceTicks = getResourceTickSkips;
        }
        else
        {
            nearestFood = lastResourceFound;
            getResourceTicks--;
        }

        ApproachResource(nearestFood, nearestFood != null ? nearestFood.transform.position : Vector3.zero, lastResourceType);
    }

    private void ApproachResource(GameObject nearestResource, Vector3 resourcePos, ResourceType resource)
    {
        // If there is no resource, walk around randomly
        if (nearestResource != null || resourcePos != Vector3.zero)
        {
            // If the resource is within consume radius, consume it
            if (IsCloseToObject(resourcePos) || (resource == ResourceType.Water && Vector3.Distance(transform.position, planet.transform.position) + 0.05 < planet.waterDiameter / 2))
            {
                if (DEBUG) Debug.Log("Found it " + Vector3.Distance(transform.position, nearestResource.transform.position) + " away");
                atDestination = true;
                
                bool disable = false;
                
                if (resource == ResourceType.Water)
                {
                    thirst = Mathf.Min(maxThirst, thirst + thirstIncrease);
                    foundWaterAt = Vector3.zero;
                }
                else
                {
                    hunger = Mathf.Min(maxHunger, hunger + hungerIncrease);
                    disable = true;
                }

                if (animate)
                {
                    InteractWithResourceAction(nearestResource, disable, resource);
                } else 
                {
                    if (disable) nearestResource.GetComponent<Resource>().ConsumeResource();
                }
            }
            else
            {
                atDestination = false;
                destination = resourcePos;
                GotoPosition(destination);
            }
        }
        else
        {
            RandomWalking();
        }
    }

    private void LookingForPartner()
    {
        // Search on the same interval as for food, and follow the last found partner in between
        if (getPartnerTicks <= 0 || lastPartnerFound == null || !lastPartnerFound.wantToReproduce)
        {
            lastPartnerFound = GetNearestPartner();
            getPartnerTicks = getResourceTickSkips;
        }
        else
        {
            getPartnerTicks--;
        }

        GameObject nearestObject = lastPartnerFound != null ? lastPartnerFound.gameObject : null;

        if (nearestObject != null)
        {
            if (IsCloseToObject(nearestObject.transform.position))
            {
                atDestination = true;
                breedingPartner = nearestObject.GetComponent<Creature>();
                currentState = CreatureState.Breeding;

            } else
            {
                atDestination = false;
                destination = nearestObject.transform.position;
                GotoPosition(destination);
            }
            
        } else
        {
            RandomWalking();
        }

    }

    private void Bredding()
    {
        if (reproductionChance > Random.Range(0f,1f) && breedingPartner.thirst < thirst)
        {
            Vector3 childPos = transform.position - transform.forward;
            
            // Spawn a poof particle where the child is spawned
            Instantiate(breedingParticle, childPos, transform.rotation, transform.parent);
            
            GameObject newObject = Instantiate(childPrefab, childPos, transform.rotation, transform.parent);
            newObject.name = newObject.name.Replace("(Clone)", "").Trim();
            newObject.GetComponent<Creature>().ApplyGenes(MixGenes(breedingPartner.genes));

            childrenCount++;
            hunger -= reproductionCost;

            if (childrenCount >= maxChildren) canReproduce = false;

            reproductionTimer = reproductionCooldown;
        }
        
        currentState = CreatureState.Walking;
    }

    private Creature GetNearestPartner()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, overlapBuffer, creatureLayerMask);
        Creature nearestPartner = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider coll = overlapBuffer[i];
            if (coll == collider || !coll.CompareTag("Creature")) continue;
            if (!coll.TryGetComponent(out Creature other) || !other.wantToReproduce) continue;
            if (other.speciesName == null || !other.speciesName.Contains(speciesName)) continue;

            float distanceToGameObject = Vector3.Distance(transform.position, coll.transform.position);
            if (nearestDistance > distanceToGameObject)
            {
                nearestDistance = distanceToGameObject;
                nearestPartner = other;
            }
        }

        return nearestPartner;
    }

    private GameObject GetNearestGameobject(ResourceType type)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, overlapBuffer, resourceLayerMask);
        GameObject nearestObject = null;
        float nearestDistance = Mathf.Infinity;

        // Find nearest object with tag
        for (int i = 0; i < hitCount; i++)
        {
            Collider coll = overlapBuffer[i];
            if (coll != collider && IsEdible(coll, type))
            {
                float distanceToGameObject = Vector3.Distance(transform.position, coll.transform.position);

                if (nearestDistance > distanceToGameObject)
                {
                    nearestDistance = distanceToGameObject;
                    nearestObject = coll.gameObject;
                }
            }

        }

        return nearestObject;
    }

    // Finds the nearest object of any food type in the diet
    private GameObject GetNearestFood(out ResourceType foundType)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, overlapBuffer, resourceLayerMask);
        GameObject nearestObject = null;
        float nearestDistance = Mathf.Infinity;
        foundType = ResourceType.Plant;

        for (int i = 0; i < hitCount; i++)
        {
            Collider coll = overlapBuffer[i];
            if (coll == collider) continue;

            foreach (ResourceType type in resourceTypes)
            {
                if (type == ResourceType.Water || !IsEdible(coll, type)) continue;

                float distanceToGameObject = Vector3.Distance(transform.position, coll.transform.position);
                if (nearestDistance > distanceToGameObject)
                {
                    nearestDistance = distanceToGameObject;
                    nearestObject = coll.gameObject;
                    foundType = type;
                }
                break;
            }
        }

        return nearestObject;
    }

    private bool IsEdible(Collider coll, ResourceType type)
    {
        if (!coll.CompareTag(resourceTags[(int)type])) return false;
        if (type != ResourceType.Creature) return true;

        if (!coll.TryGetComponent(out Creature creature)) return false;
        if (creature.isDying) return false;
        if (creatureDiet != creature.GetCreatureType) return false;
        if (SameSpecies(creature.speciesName)) return false;
        return creature.gettingEatenBy == null;
    }

    private Vector3 GetNearestWaterSource()
    {
        // Get random points around the creature and try to find water
        for (int i = 0; i < 2; i++)
        {
            Vector3 randPos = transform.position + transform.rotation * Random.insideUnitCircle * (detectionRadius * 3);

            Ray ray = new(randPos, planet.transform.position - randPos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, planet.radius))
            {
                // Check if the point hit is below water level
                if (Vector3.Distance(hit.point, planet.transform.position) < planet.waterDiameter / 2)
                {
                    if (DEBUG) Debug.DrawLine(randPos, hit.point, Color.red, 10f);
                    foundWaterAt = hit.point;
                    return hit.point;
                }
                
            }
        }

        return Vector3.zero;
    }

    private void GotoPosition(Vector3 pos)
    {
        if (!pos.Equals(Vector2.zero) && !IsCloseToObject(pos))
        {
            // Move the ridgidbody based on velocity
            rigidbody.MovePosition(transform.position + speed * Time.deltaTime * (pos - transform.position).normalized);
        }
        else
        {
            atDestination = true;
        }
    }
    
    private bool IsCloseToObject(Vector3 pos)
    {
        // Gets the smallest distance from collider to pos
        return collider.bounds.SqrDistance(pos) < 0.15f;
    }

    private void Rotate()
    {
        Vector3 direction = destination - transform.position;

        RaycastHit hit;
        
        // Cast a short ray down against the terrain to get its normal
        Vector3 down = planet.transform.position - transform.position;
        if (Physics.Raycast(transform.position, down, out hit, 10f, planetLayerMask))
        {
            Vector3 newUp = hit.normal;
            Vector3 oldForward = transform.forward;

            Vector3 newRight = Vector3.Cross(newUp, oldForward);
            Vector3 newForward = Vector3.Cross(newRight, newUp);
            
            // Caluculate angle between the creature and destination
            float angle = Vector3.SignedAngle(newForward, direction, newUp);

            // Set rotation of creature to the normal of hit
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(newForward, newUp), Time.fixedDeltaTime * 10f);

            // Rotate the creature to the direction it is walking
            transform.Rotate(new (0, angle * Time.fixedDeltaTime * 2f, 0));

            // Switches chunk if entered into new chunk, only looked up when the ground under the creature changes
            if (hit.collider != lastGroundCollider)
            {
                lastGroundCollider = hit.collider;

                GameObject hitChunk = hit.transform.gameObject;
                GameObject currentChunk = transform.parent.parent.gameObject;
                if (hitChunk != currentChunk && hitChunk.TryGetComponent(out Chunk newChunk))
                {
                    transform.parent = newChunk.creatureGameObject.transform;
                }
            }


        }
    }
    private void AttractToPlanet()
    {
        float gravity = -9.82f; // May want different gravity in the future
        Vector3 targetDirection = (transform.position - planet.transform.position).normalized;

        rigidbody.AddForce(targetDirection * (gravity * (rigidbody.mass / 2)));
    }

    private Vector3 GetRandomPoint()
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, transform.position);
        Vector3 randomRayPoint;

        // Randomize points around the creature, a few per frame
        for (int i = 0; i < randomPointTries; i++)
        {
            randomRayPoint = transform.position + rotation * Random.insideUnitCircle * detectionRadius;
            
            // Offset the ray to start in the sky
            randomRayPoint += (randomRayPoint - planet.transform.position).normalized * (planet.radius / 4);
            
            Ray ray = new(randomRayPoint, planet.transform.position - randomRayPoint);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, planet.radius))
            {
                // Check if the point hit is below water level
                if (Vector3.Distance(hit.point, planet.transform.position) > planet.waterDiameter / 2)
                {
                    return hit.point;
                }

            }
        }
        
        // If no apropriate position is found try to go back. 
        return transform.position - transform.forward * 30f;
    }

    // Interacts with a resource and plays eat animation
    private void InteractWithResourceAction(GameObject resource, bool disable, ResourceType type)
    {
        currentState = CreatureState.PerformingAction;
        string animationType = type == ResourceType.Water && animatorParameters.HasBool("isDrinking") ? "isDrinking" : "isAttacking";
        animatorParameters.SetBool(animationType, true);
        
        StartCoroutine(InteractWithResource(resource, disable, type));
    }

    private IEnumerator InteractWithResource(GameObject resource, bool disable, ResourceType type)
    {
        // Wait until walk animation is done
        yield return walkAnimationWait;
        animatorParameters.SetBool("isWalking", false);
        
        // Animation clip length
        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;

        // Wait for duration of eat animation
        yield return new WaitForSeconds(clipLength + 0.1f);

        // Destroy / consume resource
        if (disable)
        {
            if (type == ResourceType.Creature)
            {
                Creature creature = resource.GetComponent<Creature>();
                if (creature != null)
                {
                    creature.Kill();
                }
                
            } else
            {
                resource.GetComponent<Resource>().ConsumeResource();
            }
        }

        // Set the state to idle
        string animationType = type == ResourceType.Water && animatorParameters.HasBool("isDrinking") ? "isDrinking" : "isAttacking";
        animatorParameters.SetBool(animationType, false);
        animatorParameters.SetBool("isWalking", true);

        yield return afterActionWait;

        currentState = CreatureState.Walking;
    }

    #region Timers

    private void UpdateReproductionTimer()
    {
        if (canReproduce && reproductionTimer > 0)
        {
            reproductionTimer -= Time.deltaTime;
        }
    }

    private void UpdateGrowthTimer()
    {
        if (isChild)
        {
            // Decrease grow-up timer
            if (growUpTime > 0)
            {
                growUpTime -= Time.deltaTime;
            }
            else
            {
                isChild = false;
                GameObject newObject = Instantiate(parentPrefab, transform.position, transform.rotation, transform.parent);
                newObject.name = newObject.name.Replace("(Clone)", "").Trim();
                newObject.GetComponent<Creature>().ApplyGenes(genes);
                Destroy(gameObject);
            }
        }
    }

    private void UpdateIdleSoundsTimer()
    {
        if (idleSounds.Length > 0)
        {
            if (idleSoundTimer > 0)
            {
                idleSoundTimer -= Time.deltaTime;
            }
            else
            {
                audioSource.PlayOneShot(GetRandomClip(idleSounds));
                idleSoundTimer = timeBetweenIdleSounds + Random.Range(-5f, 5f); ;
            }
        }
    }

    private void UpdateStepSoundsTimer()
    {
        if (stepSounds.Length > 0)
        {
            if (stepsTimer > 0)
            {
                stepsTimer -= Time.deltaTime;
            }
            else
            {
                audioSource.PlayOneShot(GetRandomClip(stepSounds)); // SPEED = U/S     U / sek    
                stepsTimer = timeBetweenSteps * speed;
            }
        }
    }

    #endregion

    private IEnumerator SelfDestruct()
    {
        AudioClip deathSound = GetRandomClip(deathSounds);
        if (deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);

            yield return new WaitForSeconds(deathSound.length);
        }

        // Spawn a poof particle when the creature dies
        Instantiate(breedingParticle, transform.position, transform.rotation, transform.parent);

        Destroy(gameObject);
    }

    private float GetMutationMultiplier(float mutationProb, float mutationRange)
    {
        // Only apply mutation if the value if below mutation threshold
        if (Random.value < mutationProb)
        {
            // Randomize a value around 1 with an offset of "mutationRange" to either side
            return Random.Range(1 - mutationRange, 1 + mutationRange);
        } else
        {
            return 1;
        }
        
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips.Length == 0) return null;

        return clips[Random.Range(0, clips.Length)];
    }
        

    private bool SameSpecies(string creatureName)
    {
        bool sameAsParent = parentPrefabName == creatureName;
        bool sameAsChild = childPrefabName == creatureName;

        return sameAsParent || sameAsChild;
    }

    /// <summary>
    /// Kills the creature
    /// </summary>
    public void Kill()
    {
        if (!isDying)
        {
            isDying = true;
            StartCoroutine(SelfDestruct());
        }
    }

    /// <summary>
    /// Returns creature type
    /// </summary>
    public CreatureType GetCreatureType
    {
        get { return creatureType; }
    }
    
    /// <summary>
    /// Returns creature diet
    /// </summary>
    public CreatureType GetCreatureDiet
    {
        get { return creatureDiet; }
    }

    private class AnimatorParameterAdapter
    {
        readonly Animator animator;
        readonly AnimatorControllerParameter[] parameters;

        public AnimatorParameterAdapter(Animator animator)
        {
            this.animator = animator;
            parameters = animator.parameters;
        }

        public bool HasBool(string parameterName)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName)
                {
                    return true;
                }
                //HACK. Replace if more divergences are found
                else if (parameterName == "isWalking" &&  parameters[i].name == "isJumping")
                {
                    return true;
                }
            }
            return false;
        }

        public void SetBool(string name, bool value)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Bool && 
                    parameters[i].name == name)
                {
                    animator.SetBool(name, value);
                    //Debug.Log("SET: " + name);
                }
                //HACK. Replace if more divergences are found
                else if (parameters[i].type == AnimatorControllerParameterType.Bool &&
                    parameters[i].name == "isJumping" && name == "isWalking")
                {
                    animator.SetBool("isJumping", value);
                }
            }
        }

        public void SetFloat(string name, float value)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Float &&
                    parameters[i].name == name)
                {
                    animator.SetFloat(name, value);
                }
            }
        }
    }
}
