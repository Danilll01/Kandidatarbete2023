using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour
{
    
    [SerializeField] private float speed = 1f;
    [SerializeField] private float detectionRadius = 50f;
    [SerializeField] private float consumeRadius = 1f;

    [Header("Creature food and water needs")]
    [SerializeField] private float hunger = 100f;
    [SerializeField] private float thirst = 100f;

    [SerializeField] private float hungerDecrease = 0.1f;
    [SerializeField] private float thirstDecrease = 0.1f;
    
    [SerializeField] private float hungerIncrease = 20f;
    [SerializeField] private float thirstIncrease = 20f;

    
    private CreatureState currentState;

    private bool foundFoodOrWater;
    private Vector3 foodOrWaterDestination;

    // Start is called before the first frame update
    void Start()
    {
        
    }

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
            LookingForFood();
        }
        else if (currentState == CreatureState.LookingForWater)
        {
            //LookingForWater();
        }

        hunger -= hungerDecrease * Time.deltaTime;
        thirst -= thirstDecrease * Time.deltaTime;
        
        
    }

    private void LookingForFood()
    {
        
        if (foundFoodOrWater)
        {
            GotoPosition(foodOrWaterDestination);
        }

        GameObject nearestFood = GetNearestGameobject("Food");

        if (nearestFood != null && Vector3.Distance(transform.position, nearestFood.transform.position) < consumeRadius)
        {
            hunger += hungerIncrease;
            Destroy(nearestFood);
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
    }

    private void PerformAction()
    {
        
    }
}
