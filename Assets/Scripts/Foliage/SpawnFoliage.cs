using UnityEngine;
using System;
using Random = UnityEngine.Random;
using System.Linq;


/// <summary>
/// Script which spawns given foliage on a planet
/// </summary>

public class SpawnFoliage : MonoBehaviour
{
    [Header("Trees")]
    [SerializeField] private int treeLimit = 10000;
    [SerializeField] private float treeLine = 25;
    [SerializeField] private float treeAngleLimit = 30;
    [SerializeField] private GameObject[] trees = new GameObject[4];

    [Header("Bushes")]
    [SerializeField] private int bushLimit = 50000;
    [SerializeField] private float bushLine = 35;
    [SerializeField] private float bushAngleLimit = 30;
    [SerializeField] private GameObject[] bushes = new GameObject[4];

    [Header("Rocks")]
    [SerializeField] private int stoneLimit = 4000;
    [SerializeField] private float stoneLine = 36;
    [SerializeField] private float stoneAngleLimit = 30;
    [SerializeField] private GameObject[] stones = new GameObject[4];

    [Header("Misc")]
    [SerializeField] private int seed = 0;
    [SerializeField] private bool DEBUG = false;



    // Private members
    private GameObject foliageHandler;
    private float planetRadius;
    private Vector3 planetCenter;
    private Planet planet;

    void Update()
    {

        if (planet != null )
        {
            GameObject[] cameras = GameObject.FindGameObjectsWithTag("MainCamera");

            for (int i = 0; i < cameras.Length; i++)
            {
                if ((cameras[i].transform.position - planet.transform.position).magnitude < 2000)
                {
                    if (foliageHandler == null)
                    {
                        spawnFoliage();
                        break;
                    }
                    
                }
                else
                {
                    deleteFoliage();
                }

            }

        }
    }

    /// <summary>
    /// Destroys all the foliage on the current planet
    /// </summary>
    public void deleteFoliage()
    {
        if(foliageHandler!= null)
        {
            Destroy(foliageHandler);
        }
    }
    /// <summary>
    /// Takes a planet and spawns foliage on it.
    /// </summary>
    private void spawnFoliage()
    {
        // Makes the script seedable
        Random.InitState(seed);

        // Creates a game object to hold foilage objects
        foliageHandler = new GameObject("Foliage");
        foliageHandler.transform.parent = planet.transform;
        foliageHandler.transform.localPosition = new     Vector3(0, 0, 0);

        // Gets the parameters for the planets
        planetRadius = planet.radius;
        planetCenter = planet.transform.position;

        // Places foliage
        plant(trees, treeLimit, treeLine, treeAngleLimit);
        plant(bushes, bushLimit, bushLine, bushAngleLimit, true);
        setStones(stones, stoneLimit, stoneLine, stoneAngleLimit, true);

    }


    public void Initialize(Planet planet)
    {
        this.planet = planet;
    }

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
}