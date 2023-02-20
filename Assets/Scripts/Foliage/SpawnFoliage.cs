using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Linq;


/// <summary>
/// Script which spawns given foliage on a planet
/// </summary>

public class SpawnFoliage : MonoBehaviour
{
    const int prefabLimit = 10000;

    [Header("Trees")]
    [SerializeField] private int treeLimit = 1000;
    [SerializeField] private float treeLine = 25;
    [SerializeField] private float treeAngleLimit = 30;
    [SerializeField] private GameObject[] treePrefabs = new GameObject[4];

    [Header("Bushes")]
    [SerializeField] private int bushLimit = 5000;
    [SerializeField] private float bushLine = 35;
    [SerializeField] private float bushAngleLimit = 30;
    [SerializeField] private GameObject[] bushPrefab = new GameObject[4];

    [Header("Rocks")]
    [SerializeField] private int stoneLimit = 400;
    [SerializeField] private float stoneLine = 36;
    [SerializeField] private float stoneAngleLimit = 30;
    [SerializeField] private GameObject[] stonePrefab = new GameObject[4];

    [Header("Misc")]
    [SerializeField] private int seed = 0;
    [SerializeField] private bool DEBUG = false;


    private int treeIndex = 0;
    private int treeSpawnIndex = 0;
    private Vector3[] treePositions = new Vector3[prefabLimit];

    private int bushIndex = 0;
    private int bushSpawnIndex = 0;
    private Vector3[] bushPositions = new Vector3[prefabLimit];

    private int stoneIndex = 0;
    private int stoneSpawnIndex = 0;
    private Vector3[] stonePositions = new Vector3[prefabLimit];


    // Private members
    private GameObject foliageHandler;

    private Vector3 planetCenter;
    private Planet planet;
    private float planetRadius;
    private float waterLevel;

    void Update()
    {

        if(planet != null && foliageHandler != null)
        {
            GameObject[] cameras = GameObject.FindGameObjectsWithTag("MainCamera");
            for(int i = 0; i < cameras.Length; i++)
            {
                // Tries to spawn 100 of each every frame we are near the planet
                if ((cameras[i].transform.position - planet.transform.position).magnitude < 3000)
                {

                    for (int j = 100; j > 0; j--)
                    {
                        if (treeIndex > treeSpawnIndex) plantTree();
                        else break;
                    }
                    for (int j = 100; j > 0; j--)
                    {
                        if (bushIndex > bushSpawnIndex) plantBush();
                        else break;
                    }
                    for (int j = 100; j > 0; j--)
                    {
                        if (stoneIndex > stoneSpawnIndex) plantStone();
                        else break;
                    }

                }
                // Delets all foliage when leaving
                else if((cameras[i].transform.position - planet.transform.position).magnitude > 5000)
                {
                    if (foliageHandler.transform.childCount > 0)
                    {

                        Destroy(foliageHandler);

                        foliageHandler = new GameObject("Foliage");
                        foliageHandler.transform.parent = planet.transform;
                        foliageHandler.transform.localPosition = new Vector3(0, 0, 0);

                        treeSpawnIndex = 0;
                        bushSpawnIndex = 0;
                        stoneSpawnIndex = 0;
                    }
                }
            }
        }

    }


    public void Initialize(Planet planet, float waterLevel)
    {
        // Gets the parameters for the planets
        this.planet = planet;
        planetRadius = Mathf.Abs(planet.radius / 2);
        planetCenter = planet.transform.position;
        this.waterLevel = Mathf.Abs(waterLevel / 2);

        // Makes the script seedable
        Random.InitState(seed);

        // Creates a game object to hold foilage objects
        foliageHandler = new GameObject("Foliage");
        foliageHandler.transform.parent = planet.transform;
        foliageHandler.transform.localPosition = new Vector3(0, 0, 0);

        generateSpawnPoints();
    }

    /*
    /// <summary>
    /// Plants a specified prefab on a planet
    /// </summary>
    /// <param name="spawnList"> An array with prefabs </param>
    /// <param name="spawningLimit"> Maximum limit to spawn </param>
    /// <param name="maxHeight"> Maximum spawning altitude </param>
    /// <param name="angleLimit"> Maximum spawning angle </param>
    /// <param name="normalVectorSpawning"> If the prefabs should with the normal of the ground as the spawning angle </param>

    private void plant(GameObject[] spawnList, int spawningLimit, float maxHeight, float angleLimit, bool normalVectorSpawning = false)
    {
        Vector3 rayOrigin;
        Vector3 pos;
        RaycastHit hit;
        int arrayIndex;
        float maxRayDistance = 5000;

        for (int i = 0; i < spawningLimit; i++)
        {
            // Creates and sends a ray
            rayOrigin = planetCenter + Random.onUnitSphere * (planetRadius * 0.7f);
            Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);

            if (Physics.Raycast(ray, out hit, maxRayDistance))
            {
                // Checks if the ray hit and if the angle isn't too steep
                if (hit.transform.tag == "Foliage" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) > angleLimit)// || hit.distance < maxRayDistance - maxHeight) Detta funkar inte just nu pga planet radius är fel
                {
                    // Invalid spawn spot
                    if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.yellow, 10);
                    continue;
                }
                else
                {
                    // Valid spawn spot
                    if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.cyan, 10);
                    pos = hit.point;
                }
            }
            else
            {
                // Missed the planet
                if (DEBUG) Debug.DrawLine(rayOrigin, rayOrigin + (planetCenter - rayOrigin).normalized * 200, Color.red, 10);
                continue;
            }


            // Sets the corret rotation for the prefabs
            Quaternion rotation = Quaternion.LookRotation((normalVectorSpawning) ? hit.normal : rayOrigin - planetCenter) * Quaternion.Euler(90, 0, 0);
            // Sets a random rotation for more variation
            rotation *= Quaternion.Euler(0, Random.value * 360, 0);

            // Currently the only way to distinguish bioms right now, which is height
            // Checks the hight of the ground and sets the correct prefab
            if (hit.distance < maxRayDistance - maxHeight / 3) arrayIndex = Random.Range(0, 1);
            else arrayIndex = Random.Range(2, 3);
            Instantiate(spawnList[arrayIndex], pos, rotation, foliageHandler.transform);
        }

    }

    /// <summary>
    /// Plants a specified stone prefab on a planet
    /// </summary>
    /// <param name="spawnList"> An array with stones </param>
    /// <param name="spawningLimit"> Maximum limit to spawn </param>
    /// <param name="maxHeight"> Maximum spawning altitude </param>
    /// <param name="angleLimit"> Minimum spawning angle </param>
    /// <param name="normalVectorSpawning"> If the prefabs should with the normal of the ground as the spawning angle </param>
    private void setStones(GameObject[] spawnList, int spawningLimit, float maxHeight, float angleLimit, bool normalVectorSpawning = false)
    {
        Vector3 rayOrigin;
        Vector3 pos;
        RaycastHit hit;
        int arrayIndex;
        float maxRayDistance = 500;

        for (int i = 0; i < spawningLimit; i++)
        {
            // Creates and sends a ray
            rayOrigin = planetCenter + Random.onUnitSphere * (planetRadius * 0.7f);
            Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);

            if (Physics.Raycast(ray, out hit, maxRayDistance))
            {
                // Checks if the ray hit and if the angle isn't too steep
                if (hit.transform.tag == "Foliage" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) < angleLimit) // || hit.distance < maxRayDistance - maxHeight) Detta funkar inte just nu pga planet radius är fel
                {
                    // Invalid spawn spot
                    if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.red, 10);
                    continue;
                }
                else
                {
                    // Valid spawn spot
                    if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.cyan, 10);
                    pos = hit.point;
                }
            }
            else
            {
                // Missed the planet
                if (DEBUG) Debug.DrawLine(rayOrigin, rayOrigin + (planetCenter - rayOrigin).normalized * 200, Color.yellow, 10);
                continue;
            }

            // Sets the corret rotation for the prefabs
            Quaternion rotation = Quaternion.LookRotation((normalVectorSpawning) ? hit.normal : rayOrigin - planetCenter) * Quaternion.Euler(90, 0, 0);
            // Sets a random rotation for more variation
            rotation *= Quaternion.Euler(0, Random.value * 360, 0);

            // randoms which prefab to place
            arrayIndex = Random.Range(0, 3);
            Instantiate(spawnList[arrayIndex], pos, rotation, foliageHandler.transform);
        }
    }

    */
    private void generateSpawnPoints()
    {
        // Generates positions for all ray origins. TODO make them all care about planet rotation
        generateSpots(treePositions, ref treeIndex, treeLimit, treeLine);
        generateSpots(bushPositions, ref bushIndex, bushLimit, bushLine);
        generateSpots(stonePositions, ref stoneIndex, stoneLimit, stoneLine);
    }

    /// <summary>
    /// Adds ray origin positions to a vector3 array
    /// </summary>
    /// <param name="arr"> Vector3 array </param>
    /// <param name="index"> Index for the kind that should be calculated </param>
    /// <param name="limit"> Limit of spot</param>
    /// <param name="angleLimit"> Checks angle to the ground and that can't be bigger than the limit </param>
    private void generateSpots(Vector3[] arr, ref int index, float limit,float angleLimit)
    {
        Vector3 rayOrigin;
        RaycastHit hit;
        float maxRayDistance = (planetRadius - waterLevel);

        for (int i = 0; i < limit && i < prefabLimit; i++)
        {
            // Creates and sends a ray
            rayOrigin = planetCenter + Random.onUnitSphere * planetRadius;
            Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);

            if (Physics.Raycast(ray, out hit, maxRayDistance))
            {
                if (hit.distance < maxRayDistance - angleLimit)
                {
                    if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.cyan, 10);
                    arr[index] = rayOrigin - planetCenter;
                    index++;
                }
                else
                {
                    if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.yellow, 10);
                }
            }
            else
            {
                if (DEBUG) Debug.DrawLine(rayOrigin, rayOrigin + (planetCenter - rayOrigin).normalized * maxRayDistance, Color.red, 10);
            }
        }
    }
    private void plantTree()
    {

        planetCenter = planet.transform.position;
        RaycastHit hit;
        Vector3 rayOrigin = planetCenter + treePositions[treeSpawnIndex++];
        Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);
        Physics.Raycast(ray, out hit, 10000);
  
        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) > treeAngleLimit)
        {
            return;
        }
        // Sets the corret rotation for the prefabs
        Quaternion rotation = Quaternion.LookRotation(rayOrigin - planetCenter) * Quaternion.Euler(90, 0, 0);
        // Sets a random rotation for more variation
        rotation *= Quaternion.Euler(0, Random.value * 360, 0);

        Instantiate(treePrefabs[Random.Range(0, 1)], hit.point, rotation, foliageHandler.transform);
    }
    private void plantBush()
    {

        planetCenter = planet.transform.position;
        RaycastHit hit;
        Vector3 rayOrigin = planetCenter + bushPositions[bushSpawnIndex++];
        Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);
        Physics.Raycast(ray, out hit, 10000);

        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) > bushAngleLimit)
        {
            return;
        }

        // Sets the corret rotation for the prefabs
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        // Sets a random rotation for more variation
        rotation *= Quaternion.Euler(0, Random.value * 360, 0);

        Instantiate(bushPrefab[Random.Range(0, 1)], hit.point, rotation, foliageHandler.transform);
    }
    private void plantStone()
    {

        planetCenter = planet.transform.position;

        RaycastHit hit;
        Vector3 rayOrigin = planetCenter + stonePositions[stoneSpawnIndex++];
        Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);
        Physics.Raycast(ray, out hit, 10000);

        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) < stoneAngleLimit)
        {
            return;
        }

        // Sets the corret rotation for the prefabs
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        // Sets a random rotation for more variation
        rotation *= Quaternion.Euler(0, Random.value * 360, 0);

        Instantiate(stonePrefab[Random.Range(0, 1)], hit.point, rotation, foliageHandler.transform);
    }
}