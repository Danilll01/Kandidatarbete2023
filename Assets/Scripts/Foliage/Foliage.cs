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

    private Vector3[] spots = null;

    private FoliageHandler foliageHandler;
    private Random random;

    [SerializeField] private float frequency = 0.1f;

    public void Initialize(Vector3[] meshVertices, Vector3 pos)
    {
        
        foliageHandler = transform.parent.parent.parent.GetComponent<Planet>().foliageHandler;
        if (foliageHandler == null || !foliageHandler.IsPlanet) return;
        
        // Seedar en random för denna chunken
        random = new Random(Universe.seed);

        // Init array lengths
        treeArrSize = trees.Length;
        bushArrSize = bushes.Length;
        waterArrSize = waterBois.Length;
        stoneArrSize = stones.Length;
        foragablesArrSize = foragables.Length;

        // Checks if the chunk is on the surface
        Vector3 rayOrigin = pos.normalized * foliageHandler.PlanetRadius + foliageHandler.PlanetPosition;
        Ray ray = new (rayOrigin, foliageHandler.PlanetPosition - rayOrigin);
        Physics.Raycast(ray, out RaycastHit hit);

        if(hit.transform == transform.parent)
        {
            InitFoliage(meshVertices);
        }
    }

    private void InitFoliage(Vector3[] meshVertices)
    {
        int max = meshVertices.Length;

        spots = new Vector3[(int)(max * foliageHandler.Density)];

        float radius = foliageHandler.PlanetRadius;

        for (int i = 0; i < spots.Length; i++)
        {
            spots[i] = (meshVertices[random.Next(0, max)] + new Vector3(random.Next(-5, 5), random.Next(-5, 5), random.Next(5, 5))).normalized * radius;
        }
    }

    public void SpawnFoliageOnChunk()
    {
        if (spots == null) return;

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
            }
        }
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
        // 1 in 6 is a tree
        if (random.Next(10) == 0)
            SpawnForgables(hit, rayOrigin);
        else if (random.Next(6) == 0)
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
