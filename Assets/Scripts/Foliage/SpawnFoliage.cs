using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// Script which spawns given foliage on a planet
/// </summary>

public class SpawnFoliage : MonoBehaviour
{ 

    // Temporary while testing
    [SerializeField] private float radius;
    [SerializeField] private float planetRadius;
    [SerializeField] private float rayLenght;
    [SerializeField] private float spawningAngleLimit;
    [SerializeField] private int stoneLimit;
    [SerializeField] private int bushLimit;
    [SerializeField] private int treeLimit;
    [SerializeField] private int seed;
    [SerializeField] private float treeLine;

    [SerializeField] private Object[] trees = new Object[4];
    [SerializeField] private Object[] bushes = new Object[2];
    //[SerializeField] private Object[] grass = new Object[2];
    [SerializeField] private Object[] stones = new Object[2];

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(seed);

        plantStones();
        plantBushes();
        plantTrees();
    }

    /// <summary>
    /// Plants trees
    /// </summary>
    private void plantTrees()
    {
        Vector3 position;
        RaycastHit hit;

        // Loops thought all tree locations
        for (int i = 0; i < treeLimit; i++)
        {
            // Creates a ball around the planet which shoots rays to check if it's legal to spawn foliage
            position = Random.onUnitSphere * radius;

            if (Physics.Raycast(position, -position, out hit, rayLenght))
            {
                if (hit.transform.tag == "Foliage" ||
                    Mathf.Abs(Vector3.Angle(position, hit.normal)) > 30 ||
                    hit.distance < (radius - planetRadius) - treeLine)
                {
                    // Illegal spawn point
                    i--;
                    continue;
                }
                position = hit.point;
            }
            else {
                // Illegal spawn point
                i--;
                continue;
            }

            // Sets spawning placement, rotation and prefab
            Quaternion rotation = Quaternion.LookRotation(position) * Quaternion.Euler(90, 0, 0);
            int use;
            if(hit.distance < (radius - planetRadius) - treeLine / 3) use = (Random.value > 0.5f) ? 0 : 1;
            else use = (Random.value > 0.5f) ? 2 : 3;
            Object newTree = Instantiate(trees[use], position, rotation);
            newTree.GetComponent<Transform>().parent = this.transform;
        }
    }
    /// <summary>
    /// Plants bushes
    /// </summary>
    private void plantBushes()
    {
        Vector3 position;
        RaycastHit hit;

        for (int i = 0; i < bushLimit; i++)
        {
            // Creates a ball around the planet which shoots rays to check if it's legal to spawn foliage
            position = Random.onUnitSphere * radius;
            if (Physics.Raycast(position, -position, out hit, rayLenght))
            {
                if (hit.transform.tag == "Foliage" || 
                    Mathf.Abs(Vector3.Angle(position, hit.normal)) > spawningAngleLimit + 10 ||
                    hit.distance < (radius - planetRadius) - treeLine - 10)
                {
                    // Illegal spawn point
                    i--;
                    continue;
                }
                position = hit.point;
            }
            else
            {
                // Illegal spawn point
                i--;
                continue;
            }

            // Sets spawning placement, rotation and prefab
            Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
            int use = (Random.value > 0.5f) ? 1 : 0;
            Object newBush = Instantiate(bushes[use], position, rotation);
            newBush.GetComponent<Transform>().parent = this.transform;
        }
    }


    /// <summary>
    /// Plants* stones
    /// </summary>
    private void plantStones()
    {
        Vector3 position;
        RaycastHit hit;

        for (int i = 0; i < stoneLimit; i++)
        {
            // Creates a ball around the planet which shoots rays to check if it's legal to spawn foliage
            position = Random.onUnitSphere * radius;
            if (Physics.Raycast(position, -position, out hit, rayLenght))
            {
                if (hit.transform.tag == "Foliage" || 
                    Mathf.Abs(Vector3.Angle(position, hit.normal)) < spawningAngleLimit)
                {
                    // Illegal spawn point
                    i--;
                    continue;
                }
                position = hit.point;
            }
            else
            {
                // Illegal spawn point
                i--;
                continue;
            }

            Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
            int use = (Random.value > 0.5f) ? 1 : 0;
            Object newBush = Instantiate(stones[use], position, rotation);
            newBush.GetComponent<Transform>().parent = this.transform;
        }
    }
}