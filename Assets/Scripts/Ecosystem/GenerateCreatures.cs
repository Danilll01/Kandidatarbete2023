using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Linq;
using System.Collections.Generic;

public class GenerateCreatures : MonoBehaviour
{
    [SerializeField] GameObject creature;

    [Header("Creature Generation")]
    [SerializeField] private int maxPackCount = 100;
    [SerializeField] private int minPackSize = 1;
    [SerializeField] private int maxPackSize = 10;
    [SerializeField] private float packRadius = 50f;
    [SerializeField] private float terrainSteepnesAngle = 30f;

    [Header("Misc")]
    [SerializeField] private bool DEBUG = false;

    private int seed;
    private Planet planet;
    private Vector3 planetCenter;

    private float creatureSize = 1.5f; // Make so it fit creature size
    private GameObject creatureParent;

    private List<Vector3> waterPoints = new List<Vector3>();


    /// <summary>
    /// Initializes creature generation
    /// </summary>
    /// <param name="planet">The planet script found on a planet</param>
    /// <param name="randomSeed">The random seed to spawn things with</param>
    public void Initialize(Planet planet, int randomSeed)
    {
        this.planet = planet;
        seed = randomSeed;
        planetCenter = planet.transform.position;

        // Create a gameobject to hold all creatures
        creatureParent = new GameObject("Creatures");
        creatureParent.transform.parent = planet.transform;
        creatureParent.transform.localPosition = new Vector3(0,0,0);

        // This is how system random works where we dont share Random instances
        //System.Random rand1 = new System.Random(1234);
        Random.InitState(seed);

        GenerateCreaturesOnPlanet();
        if (DEBUG) Debug.Log("Spawning");

        GatherWaterPoints();
    }

    // Raycasts where all the packs should be created and calls CreateRandomPack to create the packs
    private void GenerateCreaturesOnPlanet()
    {
        // How far do we raycast
        float distance = planet.diameter;

        Vector3[] packPositions = new Vector3[maxPackCount];

        for (int i = 0; i < maxPackCount; i++)
        {

            Vector3 randPoint = planetCenter + Random.onUnitSphere * (planet.diameter * 0.7f);

            // The ray that will be cast
            Ray ray = new Ray(randPoint, planetCenter - randPoint);
            RaycastHit hit;

            // Registered a hit
            if (Physics.Raycast(ray, out hit, distance)) // (Physics.Linecast(randPoint, planetCenter, out hit))
            {
                // Check if the hit is close to a already existing pack
                if (CloseToListOfPoints(packPositions, hit.point, packRadius * 2))
                {
                    if (DEBUG) Debug.Log("Too close to another pack!");
                    continue;
                }

                // Draw a line from pack center
                if (DEBUG) Debug.DrawLine(planetCenter, randPoint, Color.red, 10f);

                // Get correct rotation from the normal of the hit point
                Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                CreateRandomPack(randPoint, rotation);

                packPositions[i] = hit.point;
            }
        }
    }

    // Raycasts around the center point of a pack and creates a random amount of creatures around that point
    private void CreateRandomPack(Vector3 centerPoint, Quaternion rotation)
    {
        
        // How many creatures in this pack
        int packSize = Random.Range(minPackSize, maxPackSize);

        // Used to keep track of all creature positions in a pack
        Vector3[] positions = new Vector3[packSize];

        // Create the pack
        for (int i = 0; i < packSize; i++)
        {
            Vector3 randomOrigin = centerPoint + rotation * Random.insideUnitCircle * packRadius;

            Ray ray = new(randomOrigin, planetCenter - randomOrigin);
            RaycastHit hit;
            
            // Registered a hit
            if (Physics.Raycast(ray, out hit, planet.diameter))
            {
                // Check if the hit colliding with a creature
                if (hit.transform.CompareTag("Creature"))
                {
                    if (DEBUG) Debug.Log("Hit creature");
                    continue;
                }

                // Check if the hit is coliiding with water
                if (Mathf.Abs(planet.waterDiameter)/2 > Vector3.Distance(hit.point, planetCenter))
                {
                    if (DEBUG) Debug.Log("Hit water");
                    continue;
                }

                // Check if "hit.point" is close to a point in positions
                if (CloseToListOfPoints(positions, hit.point, creatureSize))
                {
                    if (DEBUG) Debug.Log("Too close to another creature in pack");
                    continue;
                }

                // Check if the terrain is too steep to place a creature on 
                if (AngleTooSteep(randomOrigin, hit.point, hit.normal))
                {
                    if (DEBUG) Debug.Log("Angle too steep");
                    continue;
                }

                // Creates a rotation for the new object that always is rotated towards the planet
                Quaternion rotation2 = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(90, 0, 0);
                //Quaternion rotation2 = Quaternion.LookRotation(hit.point) * Quaternion.Euler(90, 0, 0);
                GameObject newObject = Instantiate(creature, hit.point, rotation2, hit.transform.GetComponent<Chunk>().creatures);
                newObject.transform.rotation = rotation2;

                bool isSpawnPlanet = planet.gameObject == planet.transform.parent.GetChild(1).gameObject;

                newObject.SetActive(isSpawnPlanet);

                if (DEBUG) Debug.DrawLine(randomOrigin, hit.point, Color.cyan, 10f);

                positions[i] = hit.point;
            }

        }

        creatureParent.SetActive(false);
    }

    private void GatherWaterPoints()
    {
        float rayOffset = 1f;
        float minRayDist = 1.3f;
        float maxRayDist = 2f;

        float maxRayDistance = (planet.radius - Mathf.Abs(planet.waterDiameter) / 2) + rayOffset;
        
        Vector3 rayOrigin;
        Vector3 planetCenter = planet.transform.position;
        RaycastHit hit;

        for (int i = 0; i < 150000; i++)
        {
            rayOrigin = planetCenter + Random.onUnitSphere * planet.radius;
            Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);

            if (Physics.Raycast(ray, out hit, maxRayDistance + maxRayDist))
            {
                if (hit.distance > maxRayDistance + minRayDist)
                {
                    if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.blue, 10);
                    waterPoints.Add(hit.point - planetCenter);
                }
            }
        }

        planet.waterPoints = waterPoints;
    }
    

    // Helper methods

    // Check if a point is near other points in an array
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

    // Calculates if the angle between three points is too steep
    private bool AngleTooSteep(Vector3 spawnPos, Vector3 groundPos, Vector3 groundNormal)
    {
        // Calculate the angle between the normal and the vector from the spawn point to the ground point
        float angle = Vector3.Angle(groundNormal, spawnPos - groundPos);
        if (DEBUG) Debug.Log("Angle: " + angle);
        // If the angle is too steep, return false
        if (angle > terrainSteepnesAngle)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Enable or disable the creature parent
    /// </summary>
    /// <param name="show"></param>
    public void ShowCreatures(bool show)
    {
        creatureParent.SetActive(show);
    }
}
