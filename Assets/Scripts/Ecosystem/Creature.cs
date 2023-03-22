using System.Collections;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Animator))]
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
    [SerializeField] private float reproductionChance = 0.5f;
    [SerializeField] private int maxChildren = 2;
    [SerializeField] private int childrenCount = 0;
    
    [SerializeField] private bool isChild = false;
    [SerializeField] private float growUpTime = 60f;

    [SerializeField] private GameObject breedingParticle;

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

    private Creature breedingPartner;

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

        // Teleport the creature 1 meter up in correct direction based on position on planet
        transform.position += -(planet.transform.position - transform.position).normalized * 0.3f;

        // Randomize stats
        if (randomizeStats)
        {
            hunger = Random.Range(30, maxHunger);
            thirst = Random.Range(30, maxThirst);
        }

        animator.SetFloat("Speed", speed);
        animator.keepAnimatorStateOnDisable = true;

        if (isChild) canReproduce = false;
    }

    // Update is called once per frame
    void Update()
    {
        // If creature is not visible, dont perform physics update
        if (!renderer.isVisible)
        {
            if (!isSleeping) rigidbody.Sleep();
            isSleeping = true;
            return;
        } else
        {
            isSleeping = false;
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
                foreach (ResourceType resource in resourceTypes)
                {
                    if (resource == ResourceType.Water) continue;
                    
                    LookingForResource(resource);
                }
                
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
            // Spawn a poof particle when the creature dies
            Instantiate(breedingParticle, transform.position, transform.rotation, transform.parent);

            Destroy(gameObject);
        }

        // Decrese timer for reproduction
        if (canReproduce && reproductionTimer > 0)
        {
            reproductionTimer -= Time.deltaTime;
        }

        
        if (isChild)
        {
            // Decrease grow-up timer
            if (growUpTime > 0)
            {
                growUpTime -= Time.deltaTime;
            } else
            {
                isChild = false;
                GameObject newObject = Instantiate(parentPrefab, transform.position, transform.rotation, transform.parent);
                newObject.name = newObject.name.Replace("(Clone)", "").Trim(); 
                Destroy(gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        if (!isSleeping)
        {
            AttractToPlanet();
            Rotate();
        }
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
        GameObject nearestResource = GetNearestGameobject(resource);
        Vector3 resourcePos = Vector3.zero;

        // Get position of nearest resource
        if (resource != ResourceType.Water)
        {
            if (nearestResource != null)
                resourcePos = nearestResource.transform.position;
        } else
        {
            resourcePos = GetNearestWaterSource();        
        }

        // If there is no resource, walk around randomly
        if (nearestResource != null && resourcePos != Vector3.zero)
        {
            // If the resource is within consume radius, consume it
            if (IsCloseToDestination(resourcePos))
            {
                if (DEBUG) Debug.Log("Found it " + Vector3.Distance(transform.position, nearestResource.transform.position) + " away");
                atDestination = true;
                
                bool disable = false;
                
                if (resource == ResourceType.Water)
                {
                    thirst = Mathf.Min(maxThirst, thirst + thirstIncrease);
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
            else if (Vector3.Distance(transform.position, resourcePos) > consumeRadius)
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
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        GameObject nearestObject = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider coll in hitColliders)
        {
            if (coll != collider && coll.gameObject.CompareTag("Creature")) 
            {
                bool foundPartner = coll.GetComponent<Creature>().wantToReproduce && coll.name.Contains(gameObject.name);
                if (!foundPartner) continue;

                float distanceToGameObject = Vector3.Distance(transform.position, coll.transform.position);

                if (nearestDistance > distanceToGameObject)
                {
                    nearestDistance = distanceToGameObject;
                    nearestObject = coll.gameObject;
                }
            }
        }

        if (nearestObject != null)
        {
            if (IsCloseToDestination(nearestObject.transform.position))
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

            childrenCount++;
            hunger -= reproductionCost;

            if (childrenCount >= maxChildren) canReproduce = false;

            reproductionTimer = reproductionCooldown;
        }
        
        currentState = CreatureState.Walking;
    }

    private GameObject GetNearestGameobject(ResourceType type)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        GameObject nearestObject = null;
        float nearestDistance = Mathf.Infinity;

        // Find nearest object with tag
        foreach (Collider coll in hitColliders)
        {
            if (coll != collider && coll.gameObject.CompareTag(type.ToString()))
            {
                if (type == ResourceType.Creature)
                {
                    CreatureType creatureType = coll.gameObject.GetComponent<Creature>().GetCreatureType;
                    if (creatureDiet != creatureType) continue;
                    if (SameSpecies(coll.gameObject.name)) continue;
                }
                
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

    private Vector3 GetNearestWaterSource()
    {
        Vector3 closestWaterSource = Vector3.zero;
        float closestDistance = Mathf.Infinity;

        // We can implement caching here to get faster results. Ex save the 40 closest sources and only update when we are searching for water again
        
        foreach (Vector3 pos in planet.waterPoints)
        {
            // Calculate distance from creature to pos
            float distance = Vector3.Distance(transform.position, pos);
            if (distance < closestDistance && distance < detectionRadius * 4)
            {
                closestDistance = distance;
                closestWaterSource = pos;
            }
        }

        return closestWaterSource;
    }

    private void GotoPosition(Vector3 pos)
    {
        if (!pos.Equals(Vector2.zero) && !IsCloseToDestination(pos))
        {
            // Move the ridgidbody based on velocity
            rigidbody.MovePosition(transform.position + speed * Time.deltaTime * (pos - transform.position).normalized);
        }
        else
        {
            atDestination = true;
        }
    }

    private bool IsCloseToDestination(Vector3 pos)
    {
        Vector3 creatureToPlanetCenter = (planet.transform.position - transform.position);
        Vector3 posToPlanetCenter = planet.transform.position - pos;

        float angle = Vector3.Angle(creatureToPlanetCenter, posToPlanetCenter);
        return angle < consumeRadius;
    }

    private void Rotate()
    {
        Vector3 direction = destination - transform.position;

        RaycastHit hit;
        
        // Cast a ray down to get the normal of the terrain
        if (Physics.Linecast(transform.position, planet.transform.position, out hit))
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

            GameObject hitChunk = hit.transform.gameObject;
            GameObject currentChunk = transform.parent.parent.gameObject;

            if (hitChunk != null && currentChunk != null)
            {
                // Switches chunk if entered into new chunk
                if (hitChunk != currentChunk)
                {
                    Chunk newChunk = hitChunk.transform.GetComponent<Chunk>();
                    if (newChunk != null)
                    {
                        transform.parent = newChunk.creatures;
                    }
                }
            }
            

        }
    }
    private void AttractToPlanet()
    {
        float gravity = -9.82f; // May want different gravity in the future
        Vector3 targetDirection = (transform.position - planet.transform.position).normalized;

        rigidbody.AddForce(targetDirection * gravity);
    }

    private Vector3 GetRandomPoint()
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, transform.position);
        Vector3 randomPoint;
        int tries = 0;

        // Get a random point on the planet, if it fails try again
        do
        {
            randomPoint = transform.position + rotation * Random.insideUnitCircle * detectionRadius * 2;
            tries++;

            // At 100 tries, just walk back the way you came
            if (tries > 100)
            {
                return transform.position - transform.forward * 4f;
            }
        } while (Vector3.Distance(randomPoint, planet.transform.position) < Mathf.Abs(planet.waterDiameter) / 2);

        return randomPoint;
    }

    // Interacts with a resource and plays eat animation
    private void InteractWithResourceAction(GameObject resource, bool disable, ResourceType type)
    {
        currentState = CreatureState.PerformingAction;
        animator.SetBool("Eat", true);
        
        StartCoroutine(InteractWithResource(resource, disable, type));
    }

    private IEnumerator InteractWithResource(GameObject resource, bool disable, ResourceType type)
    {
        // Wait until walk animation is done
        yield return new WaitForSeconds(0.1f);
        animator.SetBool("Walk", false);
        
        // Animation clip length
        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;

        // Wait for duration of eat animation
        yield return new WaitForSeconds(clipLength + 0.1f);

        // Destroy / consume resource
        if (disable)
        {
            if (type == ResourceType.Creature)
            {
                Destroy(resource);
            } else
            {
                resource.GetComponent<Resource>().ConsumeResource();
            }
        }

        // Set the state to idle
        animator.SetBool("Eat", false);
        animator.SetBool("Walk", true);

        yield return new WaitForSeconds(1);

        currentState = CreatureState.Walking;
    }

    private bool SameSpecies(string creatureName)
    {
        bool sameAsParent = parentPrefab.name == creatureName;
        bool sameAsChild = childPrefab.name == creatureName;

        return sameAsParent || sameAsChild;
    }

    public CreatureType GetCreatureType
    {
        get { return creatureType; }
    }
    
    public CreatureType GetCreatureDiet
    {
        get { return creatureDiet; }
    }
}
