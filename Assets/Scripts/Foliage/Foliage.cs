using UnityEngine;
using ExtendedRandom;
using Noise;
using System.Collections.Generic;

public class Foliage : MonoBehaviour
{
    // Might wanna move this to the FoliageHandler cuz it might differ depending on biome
    private int maxAngle = 35;

    // Stats for debug
    private int objectsNr = 0;

    // Spawning spots
    private Vector3[] plantSpots = null;

    private PriorityQueue<FoliageSpawnData> objectsToSpawn = new PriorityQueue<FoliageSpawnData>();
    private FoliageSpawnData objectToSpawn;
    private int objectsPerBatchedSpawn = 80;

    // FoliageHandler, the controller of the operation
    private FoliageHandler foliageHandler;

    private Planet planet;

    // This should probably be the Manfred random
    private RandomX random;

    [SerializeField] private float frequency = 0.1f;

    private Vector3 chunkPosition;
    private int positionArrayLength;

    private float planetMaxHeight;
    private int objectsInForest;

    [HideInInspector] public bool initialized = false;

    // Updates the debug screen
    private void OnDisable()
    {
        if(foliageHandler != null)
        {
            foliageHandler.objectsNr -= objectsNr;
            foliageHandler.UpdateDebug();
        }
    }

    // Updates the debug screen
    private void OnEnable()
    {
        if (foliageHandler != null)
        {
            foliageHandler.objectsNr += objectsNr;
            foliageHandler.UpdateDebug();
        }
    }

    /// <summary>
    /// Initialize foliage in a specific chunk. In order for a chunk to spawn foliage, run Initialize() then SpawnFoliageOnChunk();
    /// </summary>
    /// <param name="meshVerticesLength"></param>
    /// <param name="position"></param>
    public void Initialize(int meshVerticesLength, Vector3 position, int seed, Planet planet)
    {
        this.planet = planet;

        planetMaxHeight = planet.terrainLevel.GetMax();

        // Epic foliageHandler getter :O
        foliageHandler = planet.foliageHandler;
        if (foliageHandler == null && foliageHandler.isInstantiated) return;
        
        // Seedar en random f�r denna chunken // H�r vill vi ha bra random :)
        random = new RandomX(seed);

        // Determines how much foliage there should be on this chunk
        positionArrayLength = (int)(meshVerticesLength * foliageHandler.Density);

        // Used to change forest density on planets depending on its radius
        objectsInForest = Mathf.Clamp((int) (0.03f * planet.radius - 5f), 5, 1000);

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
        Vector3 pos = foliageHandler.transform.rotation * (foliageHandler.PlanetRadius * chunkPosition.normalized);
        // I would say, let Manfred change this if needed (This generates all spawn spots)
        // Check the debug function above if interested in how it works
        for (int i = 0; i < positionArrayLength + foliageHandler.MISS_COMPLIMENT; i++)
        {
            float x = (float)random.Value() * 18 - 9;
            float y = (float)random.Value() * 18 - 9;
            float z = (float)random.Value() * 18 - 9;
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
            if (heightAboveSea < planetMaxHeight * 0.8 && planet.willGeneratePlanetLife)
            {
                BelowAngle(hit, rayOrigin, heightAboveSea);
            } else
            {
                if (random.Value() < 0.5f)
                    SpawnStones(hit, rayOrigin);
            }
            
        }
    }

    // Water spawning function
    private void SpawnInWater(RaycastHit hit, Vector3 rayOrigin, float depth)
    {
        if (depth < 3)
        {
            GameObject waterObject = InstantiateObject(foliageHandler.GetWaterPlantType(), hit, rayOrigin);
            int index = waterObject.name.Contains("Type") ? 1 : 0;

            Material materialForFoliageObject = foliageHandler.foliageCollections[0].biomeMaterials[index];
            waterObject.GetComponent<MeshRenderer>().material = materialForFoliageObject;

            waterObject.transform.localScale = new Vector3(2, depth + 6, 2);
            objectsNr++;
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
        //Remove packs based on local biome
        BiomeValue localBiome = Biomes.EvaluteBiomeMap(planet.Biome, hit.point);
        int[] acceptableIndexes = new int[foliageHandler.foliageCollections.Length];

        int j = 0;

        for (int i = 0; i < foliageHandler.foliageCollections.Length; i++)
        {
            if (localBiome.IsInsideRangeCelcius(foliageHandler.foliageCollections[i].biomeRange))
            {
                // Add indicies
                acceptableIndexes[j] = i;
                j++;
            }
        }

        int chosenIndex = acceptableIndexes[random.Next(j)];

        // Only run if we found apropriate collections to spawn
        if (j > 0)
        {
            FoliageCollection chosenCollection = foliageHandler.foliageCollections[chosenIndex];
            

            if (chosenCollection.probabilityToSkip < random.Value())
            {
                GameObject foliageObj = chosenCollection.gameObjects[random.Next(chosenCollection.gameObjects.Length)];

                int index = foliageObj.name.Contains("Type") ? 1 : 0;

                Material materialForFoliageObject = chosenCollection.biomeMaterials[index];

                SpawnTreesInForest(foliageObj, rayOrigin, hit.point, chosenCollection.name, chosenCollection.probabilityToSkip, materialForFoliageObject);
            }

        }
    }

    /// <summary>
    /// Spawns "objectsPerBatchedSpawn" number of objects each call
    /// </summary>
    public void BatchedSpawning()
    {
        GameObject spawnedObject;
        for (int i = 0; i < objectsPerBatchedSpawn; i++)
        {
            if (objectsToSpawn.Count > 0)
            {
                objectToSpawn = objectsToSpawn.Dequeue();

                spawnedObject = Instantiate(objectToSpawn.prefab, objectToSpawn.position, objectToSpawn.rotation, transform);

                spawnedObject.GetComponent<MeshRenderer>().material = objectToSpawn.material;
                spawnedObject.transform.localScale *= random.Value(0.7f, 1.4f);

                objectsNr++;
            }
        }
    }

    // Forest spawning function
    private void SpawnTreesInForest(GameObject treeObject, Vector3 rayOrigin, Vector3 position, string name, float probToSkip, Material materialForObject)
    {
        
        // Used to introduce some variation in forest sizes.
        int nrObjectsToSpawn = random.Next((int)(objectsInForest * 0.8f), objectsInForest);

        // Use distance to player in priority queue to prioritize spawning objects closer to the player first
        float distToPlayer = Vector3.Distance(position, Universe.player.transform.position);

        // Spawns 5 trees around a found forest spot! Bigger number = denser forest
        for (int i = 0; i < nrObjectsToSpawn; i++)
        {
            if (probToSkip > random.Value()) continue;

            float x = (float)random.Value() * 2 - 1;
            float y = (float)random.Value() * 2 - 1;
            float z = (float)random.Value() * 2 - 1;
            Vector3 localpos = Quaternion.Euler(x, y, z) * rayOrigin;

            // Assumes we are spawning trees on a planet located in origin!
            // If shit is bugged might have to change this ray
            Physics.Raycast(localpos, -localpos, out RaycastHit hit);
            if(hit.transform == transform.parent && hit.distance < foliageHandler.PlanetRadius - foliageHandler.WaterRadius)
            {
                Quaternion rotation = Quaternion.LookRotation(rayOrigin) * Quaternion.Euler(90, 0, 0);
                rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);

                // Add spawn position to priority queue
                objectsToSpawn.Enqueue(new FoliageSpawnData(hit.point - (hit.point.normalized * 0.1f), rotation, treeObject, materialForObject, name), distToPlayer);
                
                if (foliageHandler.debug) Debug.DrawLine(localpos, hit.point, Color.yellow, 10f);
            }
        }
    }

    // Bush spawning function
    private void SpawnBushes(RaycastHit hit, Vector3 rayOrigin)
    {
        GameObject foliageObj = InstantiateObject(foliageHandler.GetBushType(), hit, hit.normal);
        int index = foliageObj.name.Contains("Type") ? 1 : 0;

        Material materialForFoliageObject = foliageHandler.foliageCollections[3].biomeMaterials[index];
        foliageObj.GetComponent<MeshRenderer>().material = materialForFoliageObject;
        objectsNr++;
    }

    // Stone spawning fucntion
    private void SpawnStones(RaycastHit hit, Vector3 rayOrigin)
    {
        GameObject foliageObj = InstantiateObject(foliageHandler.GetStoneType(), hit, hit.normal);

        int index = foliageObj.name.Contains("Type") ? 1 : 0;

        Material materialForFoliageObject = foliageHandler.foliageCollections[3].biomeMaterials[index];
        foliageObj.GetComponent<MeshRenderer>().material = materialForFoliageObject;
        objectsNr++;
    }

    private GameObject InstantiateObject(GameObject prefab, RaycastHit hit, Vector3 up)
    {
        Quaternion rotation = Quaternion.LookRotation(up) * Quaternion.Euler(90, 0, 0);
        rotation *= Quaternion.Euler(0, random.Next(0, 360), 0);
        return Instantiate(prefab, hit.point, rotation, transform);
    }

    
}
