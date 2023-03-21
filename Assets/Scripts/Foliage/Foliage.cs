using UnityEngine;
using Random = System.Random;
using Noise;

public class Foliage : MonoBehaviour
{
    // Might wanna move this to the FoliageHandler cuz it might differ depending on biome
    private int maxAngle = 35;

    // Stats for debug
    private int treeNr = 0;
    private int bushNr = 0;
    private int waterPlantNr = 0;
    private int stoneNr = 0;
    private int foragableNr = 0;

    // Spawning spots
    private Vector3[] plantSpots = null;

    // FoliageHandler, the controller of the operation
    private FoliageHandler foliageHandler;

    // This should probably be the Manfred random
    private Random random;

    [SerializeField] private float frequency = 0.1f;

    private Vector3 chunkPosition;
    private int positionArrayLength;

    [HideInInspector] public bool initialized = false;

    // Updates the debug screen
    private void OnDisable()
    {
        if(foliageHandler != null)
        {
            foliageHandler.treeNr -= treeNr;
            foliageHandler.bushNr -= bushNr;
            foliageHandler.waterPlantNr -= waterPlantNr;
            foliageHandler.stoneNr -= stoneNr;
            foliageHandler.foragableNr -= foragableNr;
            foliageHandler.UpdateDebug();
        }
    }

    // Updates the debug screen
    private void OnEnable()
    {
        if (foliageHandler != null)
        {
            foliageHandler.treeNr += treeNr;
            foliageHandler.bushNr += bushNr;
            foliageHandler.waterPlantNr += waterPlantNr;
            foliageHandler.stoneNr += stoneNr;
            foliageHandler.foragableNr += foragableNr;
            foliageHandler.UpdateDebug();
        }
    }

    /// <summary>
    /// Initialize foliage in a specific chunk. In order for a chunk to spawn foliage, run Initialize() then SpawnFoliageOnChunk();
    /// </summary>
    /// <param name="meshVerticesLength"></param>
    /// <param name="position"></param>
    public void Initialize(int meshVerticesLength, Vector3 position)
    {
        // Epic foliageHandler getter :O
        foliageHandler = transform.parent.parent.parent.GetComponent<Planet>().foliageHandler;
        if (foliageHandler == null) return;
        
        // Seedar en random f�r denna chunken // H�r vill vi ha bra random :)
        random = new Random(Universe.seed);

        // Determines how much foliage there should be on this chunk
        positionArrayLength = (int)(meshVerticesLength * foliageHandler.Density);

        // Where to start shooting rays from
        chunkPosition = position;

        // Generates all spawn points for this chunk
        InitFoliage();

        // This is for angle debugging
        // TestSize();

        initialized = true;
    }
    
    // Test fucntion to measure the angle required to hit the whole chunk
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
                    Vector3 localpos = Quaternion.Euler(i, j, k) * chunkPosition.normalized * foliageHandler.PlanetRadius;
                    //Quaternion rotation = Quaternion.LookRotation(-localpos) * Quaternion.Euler(90, 0, 0);
                    GameObject testObject = Instantiate(foliageHandler.debugObject, transform);
                    testObject.transform.localPosition = localpos;
                    //Debug.DrawLine(pog.transform.position, foliageHandler.PlanetPosition, Color.green, 10f);
                }
        

    }

    // Generates spawn spots for this chunk
    private void InitFoliage()
    {
        // Generates arrays with viable spawning positions
        plantSpots = new Vector3[positionArrayLength + foliageHandler.MISS_COMPLIMENT];
        Vector3 pos = foliageHandler.PlanetRadius * chunkPosition.normalized;

        // I would say, let Manfred change this if needed (This generates all spawn spots)
        // Check the debug function above if interested in how it works
        for (int i = 0; i < positionArrayLength + foliageHandler.MISS_COMPLIMENT; i++)
        {
            float x = (float)random.NextDouble() * 18 - 9;
            float y = (float)random.NextDouble() * 18 - 9;
            float z = (float)random.NextDouble() * 18 - 9;
            Vector3 localpos = Quaternion.Euler(x, y, z) * pos;
            plantSpots[i] = localpos;
        }
    }

    /// <summary>
    /// This function spawns foliage on a chunk if possible
    /// </summary>
    public void SpawnFoliageOnChunk()
    {

        // Not initialized or already spawned
        if (plantSpots == null) return;

        // Checks when to exit
        int hits = 0;

        // Constant variables
        Vector3 planetPos = foliageHandler.PlanetPosition;
        float radius = foliageHandler.PlanetRadius;
        float waterRadius = foliageHandler.WaterRadius;

        // Loops though all spots for this chunk
        foreach(Vector3 spot in plantSpots)
        {
            // Shots a ray towards the center of the planet 
            Vector3 rayOrigin = spot + planetPos;
            Ray ray = new Ray(rayOrigin, planetPos - rayOrigin);
            Physics.Raycast(ray, out RaycastHit hit);

            if (foliageHandler.debug)
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
            
            // Exits early if max nr of things has spawned
            if (hits == positionArrayLength)
            {
                if (foliageHandler.debug) Debug.Log("Foliage break");
                break;
            }
                
        }
        if(foliageHandler.debug) Debug.Log("Hits: " + hits + " %: " + hits / (float)positionArrayLength * 100f);
        
        // Removes spots making the chunk unable to spawn new trees
        plantSpots = null;
        // Update debug menu
        OnEnable();
    }

    // On land
    private void SpawnOnLand(RaycastHit hit, Vector3 rayOrigin, Vector3 planetPosition, float heightAboveSea)
    {

        // Checks how steep the terrain is
        if(Mathf.Abs(Vector3.Angle(rayOrigin - planetPosition, hit.normal)) > maxAngle)
        {
            AboveAngle(hit, rayOrigin, heightAboveSea);
        }
        else
        {
            BelowAngle(hit, rayOrigin, heightAboveSea);
        }
    }

    // Water spawning function
    private void SpawnInWater(RaycastHit hit, Vector3 rayOrigin, float depth)
    {
        if (depth < 3)
        {
            Quaternion rotation = Quaternion.LookRotation(rayOrigin) * Quaternion.Euler(90, 0, 0);
            rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
            GameObject waterObject = Instantiate(foliageHandler.GetWaterPlantType(), hit.point, rotation, transform);
            waterObject.transform.localScale = new Vector3(2, depth + 6, 2);
            waterPlantNr++;
        }
    }

    // Steep terrain
    private void AboveAngle(RaycastHit hit, Vector3 rayOrigin, float heightAboveSea)
    {
        // 1/10 chance of spawning a bush in a steep area
        if(random.Next(10) == 0)
            SpawnBushes(hit, rayOrigin);
        else
            SpawnStones(hit, rayOrigin);
    }

    // Flat terrain
    private void BelowAngle(RaycastHit hit, Vector3 rayOrigin, float heightAboveSea)
    {

        
        // 1 in 10 to spawn a forgable
        if (random.Next(10) == 0)
            SpawnForgables(hit, rayOrigin);
        // ~ 1 in 5 is a tree (18%)
        else if (random.Next(5) == 0)
            SpawnTrees(hit, rayOrigin);
        else
            SpawnBushes(hit, rayOrigin);
    }

    // Tree spawning function
    private void SpawnTrees(RaycastHit hit, Vector3 rayOrigin)
    {
        
        int treeType = foliageHandler.CheckForestSpawn(hit.point);

        if (treeType != 0)
        {
            SpawnTreesInForest(treeType, rayOrigin);
        }
        else
        {
            if (random.Next(10) == 0)
            {
                // 1 in 10 to spawn a fallen tree
                Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
                rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
                Instantiate(foliageHandler.fallenTree, hit.point + hit.point.normalized * 0.18f, rotation, transform);
            }
            else
            {
                // Spawns a random tree
                Quaternion rotation = Quaternion.LookRotation(rayOrigin) * Quaternion.Euler(90, 0, 0);
                rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
                Instantiate(foliageHandler.GetTreeType(), hit.point - hit.point.normalized * 0.2f, rotation, transform);
            }
            treeNr++;
        }
    }

    // Forest spawning function
    private void SpawnTreesInForest(int treeType, Vector3 rayOrigin)
    {
        
        // Not sure if it is faster to only do this once or to just use a getter each loop
        float radius = foliageHandler.PlanetRadius;
        float waterRadius = foliageHandler.WaterRadius;

        GameObject treeObject = foliageHandler.GetForstetTree(treeType);


        // Spawns 5 trees around a found forest spot! Bigger number = denser forest
        for (int i = 0; i < 5; i++)
        {
            float x = (float)random.NextDouble()*2 - 1;
            float y = (float)random.NextDouble()*2 - 1;
            float z = (float)random.NextDouble()*2 - 1;
            Vector3 localpos = Quaternion.Euler(x, y, z) * rayOrigin;

            // Assumes we are spawning trees on a planet located in origin!
            // If shit is bugged might have to change this ray
            Physics.Raycast(localpos, -localpos, out RaycastHit hit);
            if(hit.transform == transform.parent && hit.distance < radius - waterRadius)
            {
                Quaternion rotation = Quaternion.LookRotation(rayOrigin) * Quaternion.Euler(90, 0, 0);
                rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
                Instantiate(treeObject, hit.point - (hit.point.normalized * 0.2f), rotation, transform);
                if (foliageHandler.debug) Debug.DrawLine(localpos, hit.point, Color.yellow, 10f);
                treeNr++;
            }
        }
    }

    // Bush spawning function
    private void SpawnBushes(RaycastHit hit, Vector3 rayOrigin)
    {
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
        Instantiate(foliageHandler.GetBushType(), hit.point, rotation, transform);
        bushNr++;
    }

    // Stone spawning fucntion
    private void SpawnStones(RaycastHit hit, Vector3 rayOrigin)
    {
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
        Instantiate(foliageHandler.GetStoneType(), hit.point, rotation, transform);
        stoneNr++;
    }

    // Forgables spawning function
    private void SpawnForgables(RaycastHit hit, Vector3 rayOrigin)
    {
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
        rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
        Instantiate(foliageHandler.GetForagableType(), hit.point, rotation, transform);
        foragableNr++;
        
    }

    
}
