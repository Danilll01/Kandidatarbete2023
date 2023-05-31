using ExtendedRandom;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CreatureSpawning : MonoBehaviour
{
    [SerializeField] private float terrainSteepnesAngle = 30f;
    [SerializeField] private int packsPerBatchedSpawn = 1;

    public bool initialized = false;

    // Spawning spots
    private Vector3[] creatureSpots = null;

    private Planet planet;
    private CreatureHandler creatureHandler;
    private RandomX random;
    private Vector3 chunkPosition;
    private int positionArrayLength;

    private Queue<SpawnPack> objectsToSpawn;
    private int objectsToSpawnIndex = 0;
    private bool readyToSpawn = false;

    /// <summary>
    /// Initializes creature spawning and primes ray start positions
    /// </summary>
    /// <param name="meshVerticesLength"></param>
    /// <param name="position"></param>
    /// <param name="seed"></param>
    public void Initialize(int meshVerticesLength, Vector3 position, int seed)
    {
        planet = transform.parent.parent.parent.GetComponent<Planet>();
        creatureHandler = planet.creatureHandler;
        if (creatureHandler == null && creatureHandler.isInstantiated) return;

        random = new RandomX(seed);

        // Determines how much foliage there should be on this chunk
        positionArrayLength = (int)(meshVerticesLength * creatureHandler.Density);

        // Where to start shooting rays from
        chunkPosition = position;

        // Generates all spawn points for this chunk
        InitCreatures();

        initialized = true;
    }

    private void InitCreatures()
    {
        // Generates arrays with viable spawning positions
        creatureSpots = new Vector3[positionArrayLength];
        Vector3 pos = creatureHandler.transform.rotation  * (chunkPosition.normalized * creatureHandler.PlanetRadius);

        // I would say, let Manfred change this if needed (This generates all spawn spots)
        // Check the debug function above if interested in how it works
        for (int i = 0; i < positionArrayLength; i++)
        {
            float x = (float)random.Value() * 18 - 9;
            float y = (float)random.Value() * 18 - 9;
            float z = (float)random.Value() * 18 - 9;
            Vector3 localpos = Quaternion.Euler(x, y, z) * pos;
            creatureSpots[i] = localpos;
        }

        objectsToSpawn = new Queue<SpawnPack>();
    }

    /// <summary>
    /// Shoots rays to determinate where a pack should spawn and add it the "objectsToSpawn" array.
    /// </summary>
    public void GeneratePackSpawns()
    {
        // Not initialized or already spawned
        if (creatureSpots == null) return;

        // Checks when to exit
        int hits = 0;

        // Constant variables
        Vector3 planetPos = creatureHandler.PlanetPosition;
        float radius = creatureHandler.PlanetRadius;
        float waterRadius = creatureHandler.WaterRadius;

        // Set up raycasts
        int rayCount = creatureSpots.Length;
        var results = new NativeArray<RaycastHit>(rayCount, Allocator.TempJob);
        var commands = new NativeArray<RaycastCommand>(rayCount, Allocator.TempJob);
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 origin = creatureSpots[i] + planetPos;
            Vector3 direction = planetPos - origin;
            commands[i] = new RaycastCommand(origin, direction);
        }
        // Send them off
        JobHandle rayHandle = RaycastCommand.ScheduleBatch(commands, results, 1);
        rayHandle.Complete();

        // Loops though all spots for this chunk
        for (int i = 0; i < commands.Length; i++)
        {
            RaycastHit hit = results[i];
            Vector3 rayOrigin = commands[i].from;

            if (creatureHandler.debug)
            {
                if (hit.transform == transform.parent) Debug.DrawLine(rayOrigin, hit.point, Color.green, 10f);
                else Debug.DrawLine(rayOrigin, hit.point, Color.red, 10f);
            }

            // Checks if the ray hit the correct chunk
            if (hit.transform == transform.parent && hit.distance < radius - waterRadius)
            {
                hits++;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);

                // Add pack to array of objects to spawn
                bool foundCreature = GetCreatureToSpawn(hit.point, out CreaturePack newCreature);
                if (!foundCreature)
                {
                    continue;
                }
                objectsToSpawn.Enqueue(new SpawnPack(rayOrigin, rotation, newCreature));
                objectsToSpawnIndex++;
            }

            // Exits early if max nr of things has spawned
            if (hits == positionArrayLength)
            {
                if (creatureHandler.debug) Debug.Log("Spawning break");
                break;
            }

        }
        if (creatureHandler.debug) Debug.Log("Hits: " + hits + " %: " + hits / (float)positionArrayLength * 100f);

        results.Dispose();
        commands.Dispose();

        // Removes spots making the chunk unable to spawn new trees
        creatureSpots = null;
        objectsToSpawnIndex = 0;
        readyToSpawn = true;
    }

    /// <summary>
    /// Spawns "packsPerBatchedSpawn" number of packs each call
    /// </summary>
    public void BatchedSpawning()
    {
        if (readyToSpawn && objectsToSpawn.Count > 0)
        {
            int totalIndex = objectsToSpawnIndex;
            while (totalIndex < objectsToSpawnIndex + packsPerBatchedSpawn)
            {
                if (totalIndex >= objectsToSpawn.Count || objectsToSpawn.Count == 0)
                {
                    return;
                }
                SpawnPack newPack = objectsToSpawn.Dequeue();
                SpawnPack(newPack.rayOrigin, newPack.rotation, newPack.creature);

                totalIndex++;
            }
            objectsToSpawnIndex += totalIndex - objectsToSpawnIndex;
        }
    }

    private void SpawnPack(Vector3 rayOrigin, Quaternion rotation, CreaturePack packData)
    {
        // How many creatures in this pack
        int packSize = random.Next(packData.minPackSize, packData.maxPackSize);

        // Used to keep track of all creature positions in a pack
        Vector3[] positions = new Vector3[packSize];

        // Create the pack
        for (int i = 0; i < packSize; i++)
        {
            Vector3 randomOrigin = rayOrigin + rotation * random.InsideUnitCircle() * packData.packRadius;

            Ray ray = new(randomOrigin, creatureHandler.PlanetPosition - randomOrigin);
            RaycastHit hit;

            // Registered a hit
            if (Physics.Raycast(ray, out hit, creatureHandler.PlanetRadius, 1 << LayerMask.NameToLayer("Planet")))
            {
                // Check if the hit colliding with a creature
                if (hit.transform.CompareTag("Creature"))
                {
                    if (creatureHandler.debug) Debug.Log("Hit creature");
                    continue;
                }

                // Check if the hit is coliiding with water
                if (creatureHandler.WaterRadius > Vector3.Distance(hit.point, creatureHandler.PlanetPosition))
                {
                    if (creatureHandler.debug) Debug.Log("Hit water");
                    continue;
                }

                // Check if "hit.point" is close to a point in positions
                if (CloseToListOfPoints(positions, hit.point, packData.prefabRadius))
                {
                    if (creatureHandler.debug) Debug.Log("Too close to another creature in pack");
                    continue;
                }

                // Check if the terrain is too steep to place a creature on 
                if (AngleTooSteep(randomOrigin, hit.point, hit.normal))
                {
                    if (creatureHandler.debug) Debug.Log("Angle too steep");
                    continue;
                }

                // Creates a rotation for the new object that always is rotated towards the planet
                Quaternion rotation2 = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(90, 0, 0);
                //Quaternion rotation2 = Quaternion.LookRotation(hit.point) * Quaternion.Euler(90, 0, 0);
                GameObject newObject = Instantiate(packData.prefab, hit.point, rotation2, transform);
                newObject.transform.rotation = rotation2;
                newObject.name = newObject.name.Replace("(Clone)", "").Trim();

                if (creatureHandler.debug) Debug.DrawLine(randomOrigin, hit.point, Color.cyan, 10f);

                positions[i] = hit.point;
            }

        }
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
        if (creatureHandler.debug) Debug.Log("Angle: " + angle);
        // If the angle is too steep, return false
        if (angle > terrainSteepnesAngle)
        {
            return true;
        }
        return false;
    }

    private bool GetCreatureToSpawn(Vector3 position, out CreaturePack packToSpawn)
    {
        int total = 0;

        //Remove packs based on local biome
        BiomeValue localBiome = Biomes.EvaluteBiomeMap(planet.Biome, position);
        List<CreaturePack> acceptablePacks = new List<CreaturePack>();
        for (int i = 0; i < creatureHandler.packs.Length; i++)
        {
            if (localBiome.IsInsideRangeCelcius(creatureHandler.packs[i].range))
            {
                //THIS MAY BE PERFORMANCE REDUCING DUE TO CREATUREPACK BEING STRUCT WHICH MEAN ALOT OF COPYING IS NEEDED DUE TO NO REFERENCES
                acceptablePacks.Add(creatureHandler.packs[i]);
            }
        }

        foreach (CreaturePack pack in acceptablePacks)
        {
            total += pack.ratio;
        }

        float randomNum = random.Next(0, total + 1);

        float accumulatedSum = 0;

        for (int i = 0; i < acceptablePacks.Count; i++)
        {
            accumulatedSum += acceptablePacks[i].ratio;
            if (randomNum <= accumulatedSum)
            {
                packToSpawn = acceptablePacks[i];
                return true;
            }
        }

        if (acceptablePacks.Count != 0)
        {
            packToSpawn = acceptablePacks[0];
            return true;
        }
        else
        {
            packToSpawn = new CreaturePack();
            return false;
        }
    }

    public bool FinishedSpawning
    {
        get { return objectsToSpawn.Count == 0; }
    }
}
