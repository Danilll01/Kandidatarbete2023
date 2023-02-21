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

    [SerializeField] private float hungerDecrease = 0.1f;
    [SerializeField] private float thirstDecrease = 0.1f;
    
    [SerializeField] private float hungerIncrease = 20f;
    [SerializeField] private float thirstIncrease = 20f;

    [Header("Debug")]
    [SerializeField] private CreatureState currentState;
    [SerializeField] private bool DEBUG = false;

    private bool atDestination = false;
    private Vector3 destination = Vector3.zero;
    private Planet planet;

    private Collider collider;
    private Rigidbody rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        currentState = CreatureState.Walking;

        planet = transform.parent.parent.GetComponent<Planet>();

        //collider = meshObj.GetComponent<Collider>();
        //rigidbody = meshObj.GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();

        // Teleport the creature 2 meters up in correct direction based on position on planet
        transform.position += -(planet.meshObj.transform.position - transform.position).normalized;

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

        if (currentState == CreatureState.Idle)
        {
            //Idle();
        }
        else if (currentState == CreatureState.Walking)
        {
            RandomWalking();
        }
        else if (currentState == CreatureState.LookingForFood)
        {
            LookingForResource(ResourceType.Food);
        }
        else if (currentState == CreatureState.LookingForWater)
        {
            LookingForResource(ResourceType.Water);
        }
        else if (currentState == CreatureState.PerformingAction)
        {
            InteractWithResourceAction();
        }
        else if (currentState == CreatureState.LookingForPartner)
        {
            //LookingForPartner();
        }
        else if (currentState == CreatureState.Breeding)
        {
            //Breeding();
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
        AttractToPlanet();
        KeepUpRight();
    }

    private void RandomWalking()
    {
        if (DEBUG)
        {
            Debug.Log("dest: " + destination);
            Debug.Log("pos: " + transform.position);
            Debug.Log("Is at dest: " + atDestination);
        }

        if (atDestination)
        {
            // Randomly walk around
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, transform.position);

            destination = transform.position +  rotation * Random.insideUnitCircle * detectionRadius;
            atDestination = false;
        } else
        {
            GotoPosition(destination);
        }
        
    }

    private void LookingForResource(ResourceType resource)
    {
        
        GameObject nearestResource = GetNearestGameobject(resource.ToString());
        
        if (nearestResource != null && Vector3.Distance(transform.position, nearestResource.transform.position) < consumeRadius)
        {
            Debug.Log("Found it " + Vector3.Distance(transform.position, nearestResource.transform.position) + " away");
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
        if (!pos.Equals(Vector2.zero) && Vector3.Distance(transform.position, pos) > 1.5f)
        {
            //Vector3 direction = pos - transform.position;
            //transform.position += speed * Time.deltaTime * direction.normalized;
            //transform.rotation = Quaternion.LookRotation(direction);

            // Move the ridgidbody based on velocity
            rigidbody.MovePosition(transform.position + speed * Time.deltaTime * (pos - transform.position).normalized);
        }
        else
        {
            atDestination = true;
        }
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
        
        
        Vector3 directionFromCenter = transform.position - planet.meshObj.transform.position;

        directionFromCenter = directionFromCenter.normalized;
        transform.rotation = Quaternion.FromToRotation(transform.up, directionFromCenter) * transform.rotation;

        // Make the caracter also look a the direction it is walking
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.fixedDeltaTime * 1f);

        RaycastHit hit;

        // Cast a ray down to get the normal of the terrain
        if (Physics.Raycast(transform.position, directionFromCenter, out hit))
        {

            // Set rotation of creature to the normal of hit
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(90, 0, 0);
        }

    }
    private void AttractToPlanet()
    {
        float gravity = -9.82f;
        Vector3 targetDirection = (transform.position - planet.meshObj.transform.position).normalized;
        Vector3 bodyUp = transform.up;

        rigidbody.AddForce(targetDirection * gravity);
        transform.rotation = Quaternion.FromToRotation(bodyUp, targetDirection) * transform.rotation;

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
