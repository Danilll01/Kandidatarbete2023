using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using Unity.VisualScripting;
using static UnityEditor.PlayerSettings;
using System.Linq;
using Unity.Burst.CompilerServices;
using UnityEngine.UIElements;

public class GenerateCreatures : MonoBehaviour
{
    PlanetBody planet;
    Vector3 planetCenter;

    [SerializeField] GameObject creature;
    [SerializeField] int seed = 1234;

    [Header("Creature Generation")]
    [SerializeField] int maxPackCount = 100;
    [SerializeField] int minPackSize = 1;
    [SerializeField] int maxPackSize = 10;


    private bool DEBUG = true;
    private float creatureSize = 20f; // Make so it fit creature size
    private float packRadius = 50f;
    private GameObject creatureParent;

    // Start is called before the first frame update
    void Start()
    {
        planet = GetComponent<PlanetBody>();
        
        planetCenter = planet.transform.position;

        // Create a gameobject to hold all creatures
        creatureParent = new GameObject("Creatures");
        creatureParent.transform.parent = planet.transform;

        // This is how system random works where we dont share Random instances
        //System.Random rand1 = new System.Random(1234);
        Random.InitState(seed);

        GenerateCreaturesOnPlanet();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GenerateCreaturesOnPlanet()
    {
        // How far do we raycast
        float distance = planet.radius;

        Vector3[] packPositions = new Vector3[maxPackCount];

        for (int i = 0; i < maxPackCount; i++)
        {

            Vector3 randPoint = Random.onUnitSphere * (planet.radius * 0.7f);

            // The ray that will be cast
            Ray ray = new Ray(randPoint, planetCenter - randPoint);
            RaycastHit hit;

            // Registered a hit
            if (Physics.Raycast(ray, out hit, distance))
            {
                // Check if the hit is close to a already existing pack
                if (CloseToListOfPoints(packPositions, hit.point, packRadius * 2))
                {
                    if (DEBUG) Debug.Log("Too close to another pack!");
                    continue;
                }


                // Draw a line from pack center
                if (DEBUG) Debug.DrawLine(planetCenter, planetCenter + randPoint, Color.red, 10f);

                // Get correct rotation from the normal of the hit point
                Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                CreateRandomPack(randPoint, rotation);

                packPositions[i] = hit.point;
            }
        }
    }

    private void CreateRandomPack(Vector3 centerPoint, Quaternion rotation)
    {
        
        // How many creatures in this pack
        int packSize = Random.Range(minPackSize, maxPackSize);


        Vector3[] positions = new Vector3[packSize];

        // Create the pack
        for (int i = 0; i < packSize; i++)
        {
            Vector3 randomOrigin = centerPoint + rotation * Random.insideUnitCircle * packRadius;

            Ray ray = new Ray(randomOrigin, -centerPoint);
            RaycastHit hit;
            
            // Registered a hit
            if (Physics.Raycast(ray, out hit, planet.radius))
            {
                
                if (hit.transform.CompareTag("Creature"))
                {
                    if (DEBUG) Debug.Log("Hit creature");
                    continue;
                }

                // Check if "hit.point" is close to a point in positions
                if (CloseToListOfPoints(positions, hit.point, creatureSize))
                {
                    if (DEBUG) Debug.Log("Too close to another creature in pack");
                    continue;
                }

                // Creates a rotation for the new object that always is rotated towards the planet
                //Quaternion rotation2 = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                Quaternion rotation2 = Quaternion.LookRotation(hit.point) * Quaternion.Euler(90, 0, 0);
                GameObject newObject = Instantiate(creature, hit.point, rotation2, creatureParent.transform);


                if (DEBUG) Debug.DrawLine(randomOrigin, hit.point, Color.cyan, 10f);

                positions[i] = hit.point;
            }

            // Draw casted ray
            //Debug.DrawRay(ray.origin, -centerPoint, Color.blue, 20f, false);

        }
    }

    private bool CloseToListOfPoints(Vector3[] positions, Vector3 newPoint, float minDistance)
    {
        for (int j = 0; j < positions.Count(); j++)
        {
            if (Vector3.Distance(newPoint, positions[j]) < minDistance)
            {
                return true;
            }
        }
        return false;
    }
}
