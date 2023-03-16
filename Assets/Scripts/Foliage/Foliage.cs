using UnityEngine;
using System.Runtime.CompilerServices;
using Random = System.Random;

public class Foliage : MonoBehaviour
{

    [SerializeField] private bool debug = true;
    [SerializeField] private GameObject fallenTree;
    [SerializeField] private GameObject[] trees;
    private int treeArrSize;
    [SerializeField] private GameObject[] bushes;
    private int bushArrSize;
    [SerializeField] private GameObject[] waterBois;
    private int waterArrSize;
    [SerializeField] private GameObject[] stones;
    private int stoneArrSize;
    [SerializeField] private GameObject[] foragables;
    private int foragablesArrSize;
    [SerializeField]
    private GameObject debugObject;

    private const int MISS_COMPLIMENT = 600;

    private Vector3[] spots = null;

    private FoliageHandler foliageHandler;
    private Random random;

    [SerializeField] private float frequency = 0.1f;

    private Vector3 position;

    private int arrayLength;

    public void Initialize(int meshVerticesLength, Vector3 pos)
    {
        
        foliageHandler = transform.parent.parent.parent.GetComponent<Planet>().foliageHandler;
        if (foliageHandler == null || !foliageHandler.IsPlanet) return;
        
        // Seedar en random för denna chunken
        random = new Random(Universe.seed);

        arrayLength = (int)(meshVerticesLength * foliageHandler.Density);

        // Init array lengths
        treeArrSize = trees.Length;
        bushArrSize = bushes.Length;
        waterArrSize = waterBois.Length;
        stoneArrSize = stones.Length;
        foragablesArrSize = foragables.Length;

        position = pos;

        //TestSize();
        InitFoliage();

        //Debug.Log("Length: " + meshVerticesLength);
    }

    private void TestSize()
    {

        //spots = new Vector3[200];

        //GameObject cube = Instantiate(go, Vector3.zero, rotation, transform);

        //cube.transform.localPosition = position.normalized * foliageHandler.PlanetRadius;

        //cube.transform.localScale = new Vector3(foliageHandler.PlanetRadius * foliageHandler.PlanetRadius / 2000, 1, foliageHandler.PlanetRadius * foliageHandler.PlanetRadius / 2000);

        /*for (int i = 0; i < 200; i++)
        {


            float x = (float)random.NextDouble() * 16 - 8;
            float y = (float)random.NextDouble() * 16 - 8;
            float z = (float)random.NextDouble() * 16 - 8;


            Vector3 localpos = Quaternion.Euler(x, y, z) * position.normalized * foliageHandler.PlanetRadius;
            //Quaternion rotation = Quaternion.LookRotation(-localpos) * Quaternion.Euler(90, 0, 0);
            //GameObject pog = Instantiate(go, transform);
            //pog.transform.localPosition = localpos;
            spots[i] = localpos;
        }*/
        for(int i = -8; i < 10; i += 2)
            for (int j = -8; j < 10; j += 2)
                for(int k = -8; k < 10; k += 2)
                {
                    Vector3 localpos = Quaternion.Euler(i, j, k) * position.normalized * foliageHandler.PlanetRadius;
                    //Quaternion rotation = Quaternion.LookRotation(-localpos) * Quaternion.Euler(90, 0, 0);
                    GameObject pog = Instantiate(debugObject, transform);
                    pog.transform.localPosition = localpos;
                    //Debug.DrawLine(pog.transform.position, foliageHandler.PlanetPosition, Color.green, 10f);
                }
        

    }

    private void InitFoliage()
    {
        

        spots = new Vector3[arrayLength + MISS_COMPLIMENT];

        Vector3 pos = foliageHandler.PlanetRadius * position.normalized;

        for (int i = 0; i < arrayLength; i++)
        {
            float x = (float)random.NextDouble() * 18 - 9;
            float y = (float)random.NextDouble() * 18 - 9;
            float z = (float)random.NextDouble() * 18 - 9;
            Vector3 localpos = Quaternion.Euler(x, y, z) * pos;
            spots[i] = localpos;
        }
    }

    public void SpawnFoliageOnChunk()
    {
        if (spots == null) return;

        int hits = 0;

        Ray ray;
        RaycastHit hit;
        Vector3 planetPos = foliageHandler.PlanetPosition;
        float radius = foliageHandler.PlanetRadius;
        float waterRadius = foliageHandler.WaterRadius;

        foreach(Vector3 spot in spots)
        {

            Vector3 rayOrigin = spot + planetPos;
            ray = new Ray(rayOrigin, planetPos - rayOrigin);
            Physics.Raycast(ray, out hit);

            if (debug)
            {
                if (hit.transform == transform.parent) Debug.DrawLine(rayOrigin, hit.point, Color.green, 10f);
                else Debug.DrawLine(rayOrigin, hit.point, Color.red, 10f);
            }

            // Checks if the ray hit the correct chunk
            if (hit.transform == transform.parent)
            {
                // Checks if the ray hit land or water
                if (hit.distance < radius - waterRadius) 
                {
                    SpawnOnLand(hit, rayOrigin, planetPos, waterRadius - hit.distance);
                }
                else
                {
                    SpawnInWater(hit, rayOrigin, hit.distance - (radius - waterRadius));
                }
                hits++;
            }
            if (hits == arrayLength)
            {
                Debug.Log("Foliage break");
                break;
            }
                
        }
        Debug.Log("Hits: " + hits + " %: " + hits / (float)arrayLength * 100f);
        // Removes spots making the chunk unable to spawn new trees
        spots = null;
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SpawnOnLand(RaycastHit hit, Vector3 rayOrigin, Vector3 planetPosition, float heightAboveSea)
    {
        if(Mathf.Abs(Vector3.Angle(rayOrigin - planetPosition, hit.normal)) > 40)
        {
            AboveAngle(hit, rayOrigin, heightAboveSea);
        }
        else
        {
            BelowAngle(hit, rayOrigin, heightAboveSea);
        }
    }

    // Steep terrain
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AboveAngle(RaycastHit hit, Vector3 rayOrigin, float heightAboveSea)
    {

        // 1/7 chance of spawning a bush in a steep area
        if(random.Next(7) == 0)
            SpawnBushes(hit, rayOrigin);
        else
            SpawnStones(hit, rayOrigin);
    }

    // Flat terrain
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BelowAngle(RaycastHit hit, Vector3 rayOrigin, float heightAboveSea)
    {
        // 1 in 5 is a tree
        if (random.Next(10) == 0)
            SpawnForgables(hit, rayOrigin);
        else if (random.Next(5) == 0)
            SpawnTrees(hit, rayOrigin);
        else
            SpawnBushes(hit, rayOrigin);
    }


    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SpawnTrees(RaycastHit hit, Vector3 rayOrigin)
    {
        // 1/10 of spawning fallen tree
        if(random.Next(10) == 0)
        {
            Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
            rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
            Instantiate(fallenTree, hit.point, rotation, transform);
        }
        else
        {
            Quaternion rotation = Quaternion.LookRotation(rayOrigin) * Quaternion.Euler(90, 0, 0);
            rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
            Instantiate(trees[random.Next(treeArrSize)], hit.point - (hit.point.normalized), rotation, transform);
        }
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SpawnBushes(RaycastHit hit, Vector3 rayOrigin)
    {
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
        Instantiate(bushes[random.Next(bushArrSize)], hit.point, rotation, transform);
    }


    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SpawnStones(RaycastHit hit, Vector3 rayOrigin)
    {
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
        Instantiate(stones[random.Next(stoneArrSize)], hit.point, rotation, transform);
    }

    private void SpawnForgables(RaycastHit hit, Vector3 rayOrigin)
    {
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
        Instantiate(foragables[random.Next(foragablesArrSize)], hit.point, rotation, transform);
    }


    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SpawnInWater(RaycastHit hit, Vector3 rayOrigin, float depth)
    {
        if(depth < 3)
        {
            Quaternion rotation = Quaternion.LookRotation(rayOrigin) * Quaternion.Euler(90, 0, 0);
            rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
            GameObject waterObject = Instantiate(waterBois[random.Next(waterArrSize)], hit.point, rotation, transform);
            waterObject.transform.localScale = new Vector3(2, depth + 6, 2);
        }
    }
}
