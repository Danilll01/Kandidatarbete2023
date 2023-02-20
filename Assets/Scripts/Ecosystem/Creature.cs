using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

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
    private GameObject planet;

    private Collider collider;
    private Rigidbody rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        currentState = CreatureState.Walking;

        planet = transform.parent.parent.gameObject;

        //collider = meshObj.GetComponent<Collider>();
        //rigidbody = meshObj.GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();
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
        if (!pos.Equals(Vector2.zero) && Vector3.Distance(transform.position, pos) > 0.1f)
        {
            Vector3 direction = pos - transform.position;
            transform.position += direction.normalized * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);

            AttractToPlanet();
        }
        else
        {
            atDestination = true;
        }

        
    }

    private void AttractToPlanet()
    {
        float attractingBodyMass = 100000000;

        double r2 = Vector3.Distance(transform.position, planet.transform.position);
        r2 *= r2;

        //THE DIVIDED BY TEN IS A HOTFIX TO KEEP GRAVITY DOWN
        Vector3 attractionDirection = (planet.transform.position - transform.position).normalized / 10;

        rigidbody.velocity += attractionDirection * (float)((attractingBodyMass * Time.deltaTime) / r2);
    }

    private void InteractWithResourceAction()
    {
        currentState = CreatureState.PerformingAction;
    }
}
