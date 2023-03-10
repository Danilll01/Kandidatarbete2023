using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using Noise;

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

    private Transform player;

    private static int seed = Universe.seed;

    private Planet planet;
    private float planetRadius;
    private float waterLevel;
    private Vector3 noiseOffset;

    private bool generatedSpawnPoints = false;
    [HideInInspector] public bool foliageSpawned;

    void Update()
    { 
        if (generatedSpawnPoints)
        {
            // Tries to spawn 100 of each every frame we are near the planet
            if (ReferenceEquals(player.parent, planet.transform) && planet.chunksHandler.chunksGenerated)
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
            else if (!ReferenceEquals(player.parent, planet.transform))
            {
                for (int i = 0; i < foliageObjects.Count; i++)
                {
                    Destroy(foliageObjects[i]);
                }
                foliageObjects.Clear();
                treeSpawnIndex = 0;
                bushSpawnIndex = 0;
                stoneSpawnIndex = 0;
            }

            foliageSpawned = treeIndex <= treeSpawnIndex && bushIndex <= bushSpawnIndex && stoneIndex <= stoneSpawnIndex;
        }
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

        player = planet.player;

        generateSpawnPoints();
        generatedSpawnPoints = true;

        // Makes the script seedable
        Random.InitState(seed);
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
        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || hit.transform.tag == "Player" || hit.transform.tag == "Food" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) > treeAngleLimit)
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
        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || hit.transform.tag == "Player" || hit.transform.tag == "Food" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) > bushAngleLimit)
        {
            return;
        }


        // Sets the corret rotation for the prefabs
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        // Sets a random rotation for more variation
        rotation *= Quaternion.Euler(0, Random.value * 360, 0);
        GameObject bushObj = Instantiate(bushPrefab[getIndex(hit.point + noiseOffset)], hit.point, rotation, hit.transform);
        foliageObjects.Add(bushObj);
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
        if (hit.transform.tag == "Foliage" || hit.transform.tag == "Creature" || hit.transform.tag == "Player" || hit.transform.tag == "Food" || Mathf.Abs(Vector3.Angle(rayOrigin - planetCenter, hit.normal)) < stoneAngleLimit)
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