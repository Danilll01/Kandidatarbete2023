using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Creature : MonoBehaviour
{
    
    [SerializeField] private float speed = 1f;
    [SerializeField] private float detectionRadius = 30f;
    [SerializeField] private float consumeRadius = 0.5f;

    [SerializeField] private GameObject meshObj;

    [Header("Creature food and water needs")]
    [SerializeField] private float hunger = 100f;
    [SerializeField] private float thirst = 100f;

    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float maxThirst = 100f;

    [SerializeField] private float hungerThreshold = 80f;
    [SerializeField] private float thirstThreshold = 80f;

    [SerializeField] private float hungerDecrease = 0.1f;
    [SerializeField] private float thirstDecrease = 0.1f;
    
    [SerializeField] private float hungerIncrease = 20f;
    [SerializeField] private float thirstIncrease = 20f;

    [Header("Debug")]
    [SerializeField] private CreatureState currentState;
    [SerializeField] private bool DEBUG = false;
    [SerializeField] private bool isSleeping;
    

    // Normal destination for every state
    private bool atDestination = false;
    [SerializeField] private Vector3 destination = Vector3.zero;

    private Planet planet;

    private Collider collider;
    private Rigidbody rigidbody;
    private LODGroup lodGroup;
    private Renderer renderer;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        currentState = CreatureState.Walking;

        planet = transform.parent.parent.GetComponent<Planet>();

        collider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();
        lodGroup = meshObj.GetComponent<LODGroup>();
        renderer = lodGroup.transform.GetComponent<Renderer>();
        animator = GetComponent<Animator>();

        // Teleport the creature 1 meter up in correct direction based on position on planet
        transform.position += -(planet.transform.position - transform.position).normalized * 0.3f;

    }

    // Update is called once per frame
    void Update()
    {

        if (!renderer.isVisible)
        {
            if (!isSleeping) rigidbody.Sleep();
            isSleeping = true;
            return;
        } else
        {
            isSleeping = false;
        }

        if (currentState == CreatureState.PerformingAction)
            return;
            
        if (hunger < hungerThreshold)
        {
            currentState = CreatureState.LookingForFood;
            
        } else if (thirst < thirstThreshold)
        {
            currentState = CreatureState.LookingForWater;
        } else
        {
            currentState = CreatureState.Walking;
        }


        switch (currentState)
        {
            case CreatureState.Idle:
                //Idle()
                break;
            case CreatureState.Walking:
                RandomWalking();
                break;
            case CreatureState.LookingForFood:
                LookingForResource(ResourceType.Food);
                break;
            case CreatureState.LookingForWater:
                LookingForResource(ResourceType.Water);
                break;
            case CreatureState.PerformingAction:
                //InteractWithResourceAction();
                break;
            case CreatureState.LookingForPartner:
                //LookingForPartner();
                break;
            case CreatureState.Breeding:
                // Bredding();
                break;
        }

        hunger -= hungerDecrease * Time.deltaTime;
        thirst -= thirstDecrease * Time.deltaTime;

        // Die if hunger or thirst is 0
        if (hunger <= 0 || thirst <= 0)
        {
            Destroy(gameObject);
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
        GameObject nearestResource = GetNearestGameobject(resource.ToString());
        Vector3 resourcePos = Vector3.zero;
        
        if (resource == ResourceType.Food)
        {
            if (nearestResource != null)
                resourcePos = nearestResource.transform.position;
        } else
        {
            Vector3 nearestWater = GetNearestWaterSource();
            resourcePos = nearestWater;
        }

        if (nearestResource != null && resourcePos != Vector3.zero)
        {
            if (IsCloseToDestination(resourcePos))
            {
                //Debug.Log("Found it " + Vector3.Distance(transform.position, nearestResource.transform.position) + " away");
                atDestination = true;
                InteractWithResourceAction();

                if (resource == ResourceType.Water)
                {
                    thirst = Mathf.Min(maxThirst, thirst + thirstIncrease);
                }
                else if (resource == ResourceType.Food)
                {
                    hunger = Mathf.Min(maxHunger, hunger + hungerIncrease);
                    Destroy(nearestResource);
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

    private GameObject GetNearestGameobject(string tagname)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        GameObject nearestObject = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider coll in hitColliders)
        {
            if (coll != collider && coll.gameObject.CompareTag(tagname))
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

    private Vector3 GetNearestWaterSource()
    {
        Vector3 closestWaterSource = Vector3.zero;
        float closestDistance = Mathf.Infinity;

        // We can implement caching here to get faster results. Ex save the 40 closest sources and only update when we are searching for water again
        
        foreach (Vector3 pos in planet.waterPoints)
        {
            // Calculate distance from creature to pos
            float distance = Vector3.Distance(transform.position, pos);
            if (distance < closestDistance)
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
        // transform.position += -(planet.meshObj.transform.position - transform.position).normalized;
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
            transform.Rotate(new (0, angle * Time.fixedDeltaTime, 0));
            
        }
    }
    private void AttractToPlanet()
    {
        float gravity = -9.82f;
        Vector3 targetDirection = (transform.position - planet.transform.position).normalized;
        Vector3 bodyUp = transform.up;

        rigidbody.AddForce(targetDirection * gravity);
        //transform.rotation = Quaternion.FromToRotation(bodyUp, targetDirection) * transform.rotation;

        /*
        float attractingBodyMass = planet.mass / 1000;
        Vector3 planetPosition = planet.meshObj.transform.position;

        double r2 = Vector3.Distance(transform.position, planetPosition);
        r2 *= r2;

        if (DEBUG) Debug.Log("Distance" + transform.position + " : " + planetPosition);

        //THE DIVIDED BY TEN IS A HOTFIX TO KEEP GRAVITY DOWN
        Vector3 attractionDirection = (planetPosition - transform.position).normalized / 10;

        rigidbody.velocity += attractionDirection * (float)((attractingBodyMass * Time.deltaTime) / r2);
        */
    }

    private Vector3 GetRandomPoint()
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, transform.position);
        Vector3 randomPoint;
        int tries = 0;
        do
        {
            randomPoint = transform.position + rotation * Random.insideUnitCircle * detectionRadius;
            tries++;
            
            if (DEBUG) Debug.Log("Distance: " + Vector3.Distance(randomPoint, planet.transform.position));
            if (DEBUG) Debug.Log("Water: " + Mathf.Abs(planet.waterRadius) / 2);

            if (tries > 100)
            {
                return transform.position - transform.forward * 4f;
            }
        } while (Vector3.Distance(randomPoint, planet.transform.position) < Mathf.Abs(planet.waterRadius) / 2);

        return randomPoint;
    }

    private void InteractWithResourceAction()
    {
        currentState = CreatureState.PerformingAction;
        animator.SetBool("Walk", false);
        animator.SetBool("Eat", true);
        StartCoroutine(InteractWithResource());
    }

    // Create a coroutine
    IEnumerator InteractWithResource()
    {
        // Wait for 3 seconds
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length * 2);

        // Set the state to idle
        animator.SetBool("Eat", false);
        animator.SetBool("Walk", true);

        yield return new WaitForSeconds(1);

        currentState = CreatureState.Walking;
    }
}
