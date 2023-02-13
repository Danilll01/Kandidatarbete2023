using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using Unity.VisualScripting;
using static UnityEditor.PlayerSettings;

public class GenerateCreatures : MonoBehaviour
{
    PlanetBody planet;
    Vector3 planetCenter;

    [SerializeField] GameObject creature;
    [SerializeField] int seed = 1234;

    [Header("Creature Generation")]
    [SerializeField] int maxCreatureCount = 100;
    [SerializeField] int minPackSize = 1;
    [SerializeField] int maxPackSize = 10;


    // Start is called before the first frame update
    void Start()
    {
        planet = GetComponent<PlanetBody>();

        planetCenter = planet.transform.position;

        Random.InitState(seed);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            // How far do we raycast
            float distance = planet.radius;

            // The ray that will be cast
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Registered a hit
            if (Physics.Raycast(ray, out hit, distance))
            {
                Debug.Log("Hit!");

                
                for (int i = 0; i < 30; i++)
                {
                    Vector3 randPoint = Random.onUnitSphere * (planet.radius * 0.7f);
                    //Debug.Log(randV);
                    Debug.DrawLine(planetCenter, planetCenter + randPoint, Color.red, 10f);

                    Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                    CreateRandomPack(randPoint, rotation);
                }
                
            }

            // This is how system random works where we dont share Random instances
            //System.Random rand1 = new System.Random(1234);
            
        }
    }

    private void CreateRandomPack(Vector3 centerPoint, Quaternion rotation)
    {
        
        // How many creatures in this pack
        int packSize = Random.Range(minPackSize, maxPackSize);
        

        // Create the pack
        for (int i = 0; i < packSize; i++)
        {
            Vector3 randomOrigin = centerPoint + rotation * Random.onUnitSphere * 50f;

            Ray ray = new Ray(randomOrigin, -centerPoint);
            RaycastHit hit;
            
            // Registered a hit
            if (Physics.Raycast(ray, out hit, planet.radius))
            {
                Debug.Log("Spawning object");

                // Creates a rotation for the new object that always is rotated towards the planet
                //Quaternion rotation2 = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                Quaternion rotation2 = Quaternion.LookRotation(hit.point) * Quaternion.Euler(90, 0, 0);
                GameObject newObject = Instantiate(creature, hit.point, rotation2, gameObject.transform);


                Debug.DrawLine(randomOrigin, hit.point, Color.cyan, 10f);

            }

            // Draw casted ray
            //Debug.DrawRay(ray.origin, -centerPoint, Color.blue, 20f, false);
            
        }
    }
}
