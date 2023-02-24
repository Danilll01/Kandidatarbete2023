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

    private bool atDestination = false;
    private Vector3 destination = Vector3.zero;
    private Planet planet;

    private Collider collider;
    private Rigidbody rigidbody;
    private LODGroup lodGroup;
    private Renderer renderer;

    // Start is called before the first frame update
    void Start()
    {
        currentState = CreatureState.Walking;

        planet = transform.parent.parent.GetComponent<Planet>();

        //collider = meshObj.GetComponent<Collider>();
        //rigidbody = meshObj.GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();
        lodGroup = meshObj.GetComponent<LODGroup>();
        renderer = lodGroup.transform.GetComponent<Renderer>();

        // Teleport the creature 2 meters up in correct direction based on position on planet
        transform.position += -(planet.transform.position - transform.position).normalized;

    }

    /*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, consumeRadius);
    }
    */

    // Update is called once per frame
    void Update()
    {
        //KeepUpRight();
        //AttractToPlanet();
        //print(rigidbody.IsSleeping());

        if (!renderer.isVisible)
        {
            if (!isSleeping) rigidbody.Sleep();
            isSleeping = true;
            return;
            //Debug.Log("Name:");
        } else
        {
            isSleeping = false;
        }

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
                InteractWithResourceAction();
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
            //print("Rendering");
            AttractToPlanet();
            KeepUpRight();
        }
    }

    private void RandomWalking()
    {
        if (false)
        {
            Debug.Log("dest: " + destination);
            Debug.Log("pos: " + transform.position);
            Debug.Log("Is at dest: " + atDestination);
        }

        if (atDestination)
        {
            // Randomly walk around
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, transform.position);

            destination = transform.position + rotation * Random.insideUnitCircle * detectionRadius;
            atDestination = false;
        } else
        {
            GotoPosition(destination);
        }
        
    }

    private void LookingForResource(ResourceType resource)
    {
        
        GameObject nearestResource = GetNearestGameobject(resource.ToString());
        
        if (nearestResource != null && IsCloseToDestination(nearestResource.transform.position))
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
            }
            
            currentState = CreatureState.Idle;
            Destroy(nearestResource);
        }
        else if (nearestResource != null && Vector3.Distance(transform.position, nearestResource.transform.position) > consumeRadius)
        {
            atDestination = true;
            destination = nearestResource.transform.position;
            GotoPosition(destination);
        }
        else
        {
            atDestination = false;
            destination = Vector3.zero;
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
        if (DEBUG) Debug.Log("Angle:" + angle);
        return angle < consumeRadius;
    }

    private void KeepUpRight()
    {
        // Look at the walking direction and have the create follow the terrain

        Vector3 direction = destination - transform.position;

        Quaternion rotation;

        if (direction != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(direction);
        } else
        {
            rotation = Quaternion.FromToRotation(Vector3.forward, transform.position);
        }
        
        
        Vector3 directionFromCenter = transform.position - planet.transform.position;

        //directionFromCenter = directionFromCenter.normalized;
        //transform.rotation = Quaternion.FromToRotation(transform.up, directionFromCenter) * transform.rotation;

        // Make the caracter also look a the direction it is walking
        //transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.fixedDeltaTime * 0.2f);

        RaycastHit hit;
        
        // Cast a ray down to get the normal of the terrain
        if (Physics.Linecast(transform.position, planet.transform.position, out hit))
        {
            if (DEBUG) Debug.Log("Hit: " + hit.collider.gameObject.name);
            //Quaternion grndTilt = 
            // Set rotation of creature to the normal of hit
            //transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * rotation; // Typ works
            //Quaternion.AngleAxis(Quaternion.Angle(transform.rotation, rotation), hit.normal);

            
            Vector3 newUp = hit.normal;
            Vector3 oldForward = transform.forward;

            Vector3 newRight = Vector3.Cross(newUp, oldForward);
            Vector3 newForward = Vector3.Cross(newRight, newUp);

            transform.rotation = Quaternion.LookRotation(newForward, newUp);
            /*
            
            Vector3 terrainNormal = hit.normal;

            // Calculate the forward direction towards the target point
            Vector3 targetPoint = destination;
            Vector3 forwardDirection = (targetPoint - transform.position).normalized;

            // Calculate the right direction using the cross product of the forward direction and the terrain normal
            Vector3 rightDirection = Vector3.Cross(forwardDirection, terrainNormal);

            // Calculate the new up direction using the cross product of the right and forward directions
            Vector3 upDirection = Vector3.Cross(rightDirection, forwardDirection);

            // Create a new rotation using the forward and up directions
            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection, upDirection);

            // Combine the new rotation with the rotation that keeps the object aligned with the terrain normal
            Quaternion finalRotation = Quaternion.FromToRotation(transform.up, terrainNormal) * targetRotation;

            // Apply the final rotation to the object
            transform.rotation = finalRotation;
            */
        }

        // https://stackoverflow.com/questions/61852558/unity-rotating-object-around-y-axis-while-constantly-setting-up-different-rot

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

    private void InteractWithResourceAction()
    {
        currentState = CreatureState.PerformingAction;
    }
}
