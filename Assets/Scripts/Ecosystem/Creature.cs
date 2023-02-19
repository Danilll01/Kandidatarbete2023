using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour
{
    
    [SerializeField] private float speed = 1f;
    [SerializeField] private float detectionRadius = 30f;
    [SerializeField] private float consumeRadius = 1f;

    [Header("Creature food and water needs")]
    [SerializeField] private float hunger = 100f;
    [SerializeField] private float thirst = 100f;

    [SerializeField] private float hungerDecrease = 0.1f;
    [SerializeField] private float thirstDecrease = 0.1f;
    
    [SerializeField] private float hungerIncrease = 20f;
    [SerializeField] private float thirstIncrease = 20f;

    
    [SerializeField] private CreatureState currentState;

    private bool foundFoodOrWater;
    private Vector3 foodOrWaterDestination = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        currentState = CreatureState.LookingForWater;
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
            //Walking();
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

    private void LookingForResource(ResourceType resource)
    {
        /*
        if (!foundFoodOrWater && !foodOrWaterDestination.Equals(Vector3.zero))
        {
            GotoPosition(foodOrWaterDestination);
        }*/
        
        GameObject nearestResource = GetNearestGameobject(resource.ToString());
        
        if (nearestResource != null && Vector3.Distance(transform.position, nearestResource.transform.position) < consumeRadius)
        {
            Debug.Log("Found it");
            foundFoodOrWater = true;
            InteractWithResourceAction();

            if (resource == ResourceType.Water)
            {
                thirst += thirstIncrease;
            }
            else if (resource == ResourceType.Food)
            {
                hunger += hungerIncrease;
            }
            
            Destroy(nearestResource);
        }
        else if (nearestResource != null && Vector3.Distance(transform.position, nearestResource.transform.position) > consumeRadius)
        {
            Debug.Log("Going to it");
            foundFoodOrWater = true;
            foodOrWaterDestination = nearestResource.transform.position;
            GotoPosition(foodOrWaterDestination);
            
        }
        else
        {
            foundFoodOrWater = false;
            foodOrWaterDestination = Vector3.zero;
        }


    }

    private GameObject GetNearestGameobject(string tagname)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);

        GameObject nearestObject = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider coll in hitColliders)
        {
            if (coll.gameObject.CompareTag(tagname))
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
        Vector3 direction = pos - transform.position;
        transform.position += direction.normalized * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void InteractWithResourceAction()
    {
        currentState = CreatureState.PerformingAction;
    }
}
