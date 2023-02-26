using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Script which spawns given foliage on a planet
/// </summary>

public class SpawnFoliage : MonoBehaviour
{
    const int prefabLimit = 20000;

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
    [SerializeField] private bool DEBUG = false;

    [SerializeField] private int approximateObjectsPerChunk = 100;
    private List<GameObject> foliageObjects = new List<GameObject>();


    private int treeIndex = 0;
    private int treeSpawnIndex = 0;
    private Vector3[] treePositions = new Vector3[prefabLimit];

    private int bushIndex = 0;
    private int bushSpawnIndex = 0;
    private Vector3[] bushPositions = new Vector3[prefabLimit];

    private int stoneIndex = 0;
    private int stoneSpawnIndex = 0;
    private Vector3[] stonePositions = new Vector3[prefabLimit];

    private GameObject foliageHandler;
    private GameObject player;

    private static int seed = Universe.seed;

    private Planet planet;
    private float planetRadius;
    private float waterLevel;
    private Vector3 noiseOffset;

    private bool mergedMeshes = false;
    private bool generatedSpawnPoints = false;

    void Update()
    {
        if(generatedSpawnPoints)
        {
            // Tries to spawn 100 of each every frame we are near the planet
            if ((player.transform.position - planet.transform.position).magnitude < 3000)
            {
                for (int j = 100; j > 0; j--)
                {
                    if (treeIndex > treeSpawnIndex) plantTree();
                    else j = 0;
                }
                for (int j = 100; j > 0; j--)
                {
                    if (bushIndex > bushSpawnIndex) plantBush();
                    else j = 0;
                }
                for (int j = 100; j > 0; j--)
                {
                    if (stoneIndex > stoneSpawnIndex) plantStone();
                    else j = 0;
                }
            }
            // Delets all foliage when leaving
            else if((player.transform.position - planet.transform.position).magnitude > 5000)
            {
                for (int i = 0; i < foliageObjects.Count; i++)
                {
                    Destroy(foliageObjects[i]);
                }

                foliageObjects.Clear();
                treeSpawnIndex = 0;
                bushSpawnIndex = 0;
                stoneSpawnIndex = 0;
                generatedSpawnPoints = false;
            }

            if (treeIndex <= treeSpawnIndex && bushIndex <= bushSpawnIndex && stoneIndex <= stoneSpawnIndex)
            {
                UpdateChunks();

                if (!mergedMeshes)
                {
                    CombineStaticMeshesOfChunks();
                }
            }
        }
    }

    private void UpdateChunks()
    {
        Vector3 playerPos = player.transform.position;
        Vector3 planetCenter = Vector3.zero;
        Vector3 playerToPlanetCenter = playerPos - planetCenter;
        Vector3 halfWayPointNormal = new Vector3(playerToPlanetCenter.x / 1.5f, playerToPlanetCenter.y / 1.5f, playerToPlanetCenter.z / 1.5f);

        for (int i = 0; i < planet.chunks.Count; i++)
        {
            bool isBelowHalfWayPoint = CheckIfPointBIsBelowA(halfWayPointNormal, planet.chunks[i].transform.GetComponent<MeshRenderer>().bounds.center, halfWayPointNormal.normalized);
            if (isBelowHalfWayPoint)
            {
                planet.chunks[i].gameObject.SetActive(false);
            }
            else
            {
                planet.chunks[i].gameObject.SetActive(true);
            }
        }
    }

    private bool CheckIfPointBIsBelowA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0) ? true : false;
    }

    private void CombineStaticMeshesOfChunks()
    {
        List<GameObject> objectsInChunk = new List<GameObject>();

        for (int i = 0; i < planet.chunks.Count; i++)
        {
            GameObject meshParent = new GameObject("Mesh parent");
            meshParent.transform.parent = planet.chunks[i].transform;

            int childCount = planet.chunks[i].transform.childCount;
            for (int j = 0; j < childCount; j++)
            {
                objectsInChunk.Add(planet.chunks[i].transform.GetChild(j).gameObject);
            }

            StaticBatchingUtility.Combine(planet.chunks[i].gameObject);
            objectsInChunk.Clear();

        }
        mergedMeshes = true;
    }

    /// <summary>
    /// Initialize the foliage script.
    /// </summary>
    /// <param name="planet"> A reference to the planet the script should be run on</param>
    /// <param name="waterLevel"> Radius (diameter) of the water level of the planet </param>
    public void Initialize(Planet planet, float waterLevel, int seed)
    {
        // Gets the parameters for the planets
        this.planet = planet;
        planetRadius = Mathf.Abs(planet.diameter / 2);
        this.waterLevel = Mathf.Abs(waterLevel / 2);
        noiseOffset = planet.transform.position;

        GameObject[] cameras = GameObject.FindGameObjectsWithTag("MainCamera");
        player = cameras[0];

        // Makes the script seedable
        Random.InitState(seed);

        generateSpawnPoints();
        generatedSpawnPoints = true;
    }

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
    private void generateSpots(Vector3[] arr, ref int index, float limit, float maxHeight)
    {
        Vector3 rayOrigin;
        Vector3 planetCenter = planet.transform.position;
        RaycastHit hit;
        float maxRayDistance = (planetRadius - waterLevel);

        for (int i = 0; i < limit; i++)
        {
            // Tries 20 times
            for (int j = 0; j < 20; j++)
            {
                rayOrigin = planetCenter + Random.onUnitSphere * planetRadius;
                Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);

                if (Physics.Raycast(ray, out hit, maxRayDistance))
                {
                    //if (hit.distance < maxRayDistance - maxHeight)
                    if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.green, 10);
                    arr[index] = rayOrigin - planetCenter;
                    index++;
                    j = 20;
                }
                else
                {
                    if (DEBUG) Debug.DrawLine(rayOrigin, rayOrigin + (planetCenter - rayOrigin).normalized * maxRayDistance, Color.red, 10);
                }
            }
        }
    }
    
    /// <summary>
    /// Plants a tree from the tree array
    /// </summary>
    private void plantTree()
    {

        Vector3 planetCenter = planet.transform.position;
        RaycastHit hit;
        Vector3 rayOrigin = planetCenter + treePositions[treeSpawnIndex++];
        Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);
        Physics.Raycast(ray, out hit, 10000);

        if (hit.transform == null)
        {
            return;
        }
        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || hit.transform.tag == "Player" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) > treeAngleLimit)
        {
            return;
        }
        // Sets the corret rotation for the prefabs
        Quaternion rotation = Quaternion.LookRotation(rayOrigin - planetCenter) * Quaternion.Euler(90, 0, 0);
        // Sets a random rotation for more variation
        rotation *= Quaternion.Euler(0, Random.value * 360, 0);

        foliageObjects.Add(Instantiate(treePrefabs[getIndex(hit.point + noiseOffset)], hit.point + (ray.direction.normalized * 0.2f), rotation, hit.transform));
    }

    /// <summary>
    /// Plants a bush from the bush array
    /// </summary>
    private void plantBush()
    {

        Vector3 planetCenter = planet.transform.position;
        RaycastHit hit;
        Vector3 rayOrigin = planetCenter + bushPositions[bushSpawnIndex++];
        Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);
        Physics.Raycast(ray, out hit, 10000);

        if (hit.transform == null)
        {
            return;
        }
        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || hit.transform.tag == "Player" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) > bushAngleLimit)
        {
            return;
        }


        // Sets the corret rotation for the prefabs
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        // Sets a random rotation for more variation
        rotation *= Quaternion.Euler(0, Random.value * 360, 0);

        foliageObjects.Add(Instantiate(bushPrefab[getIndex(hit.point + noiseOffset)], hit.point, rotation, hit.transform));
    }

    /// <summary>
    /// Plants a stone :) from the stone array
    /// </summary>
    private void plantStone()
    {

        Vector3 planetCenter = planet.transform.position;
        RaycastHit hit;
        Vector3 rayOrigin = planetCenter + stonePositions[stoneSpawnIndex++];
        Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);
        Physics.Raycast(ray, out hit, 10000);

        if (hit.transform == null)
        {
            return;
        }
        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || hit.transform.tag == "Player" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) < stoneAngleLimit)
        {
            return;
        }

        // Sets the corret rotation for the prefabs
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        // Sets a random rotation for more variation
        rotation *= Quaternion.Euler(0, Random.value * 360, 0);

        foliageObjects.Add(Instantiate(stonePrefab[getIndex(hit.point + noiseOffset)], hit.point, rotation, hit.transform));
    }


    private int getIndex(Vector3 pos)
    {
        float noise = Perlin.Noise(pos);

        if (noise < 0)
        {
            if (noise < -0.14) return 0;
            else return 1;
        }
        else
        {
            if (noise > 0.14) return 2;
            else return 3;
        }
    }

}