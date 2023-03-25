using ExtendedRandom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatureSpawning : MonoBehaviour
{
    [SerializeField] private float terrainSteepnesAngle = 30f;

    public bool initialized = false;

    // Spawning spots
    private Vector3[] creatureSpots = null;

    private CreatureHandler creatureHandler;
    private RandomX random;
    private Vector3 chunkPosition;
    private int[] spawningRatios;
    private int positionArrayLength;

    public void Initialize(int meshVerticesLength, Vector3 position, int seed)
    {
        creatureHandler = transform.parent.parent.parent.GetComponent<Planet>().creatureHandler;
        if (creatureHandler == null && creatureHandler.isInstantiated) return;

        random = new RandomX(seed);

        // Determines how much foliage there should be on this chunk
        positionArrayLength = (int) (meshVerticesLength * creatureHandler.Density);

        // Where to start shooting rays from
        chunkPosition = position;

        spawningRatios = GetSpawningRatios();

        // Generates all spawn points for this chunk
        InitCreatures();

        initialized = true;
    }

    private void InitCreatures()
    {
        // Generates arrays with viable spawning positions
        creatureSpots = new Vector3[positionArrayLength];
        Vector3 pos = creatureHandler.PlanetRadius * chunkPosition.normalized;

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
    }

    public void SpawnCreatures()
    {
        
        // Not initialized or already spawned
        if (creatureSpots == null) return;

        // Checks when to exit
        int hits = 0;

        // Constant variables
        Vector3 planetPos = creatureHandler.PlanetPosition;
        float radius = creatureHandler.PlanetRadius;
        float waterRadius = creatureHandler.WaterRadius;

        // Loops though all spots for this chunk
        foreach (Vector3 spot in creatureSpots)
        {
            // Shots a ray towards the center of the planet 
            Vector3 rayOrigin = spot + planetPos;
            Ray ray = new Ray(rayOrigin, planetPos - rayOrigin);
            Physics.Raycast(ray, out RaycastHit hit);

            if (creatureHandler.debug)
            {
                if (hit.transform == transform.parent) Debug.DrawLine(rayOrigin, hit.point, Color.green, 10f);
                else Debug.DrawLine(rayOrigin, hit.point, Color.red, 10f);
            }

            // Checks if the ray hit the correct chunk
            if (hit.transform == transform.parent && hit.distance < radius - waterRadius)
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                SpawnPack(rayOrigin, rotation, GetCreatureToSpawn());
                
                hits++;
            }

            // Exits early if max nr of things has spawned
            if (hits == positionArrayLength)
            {
                if (creatureHandler.debug) Debug.Log("Spawning break");
                break;
            }

        }
        if (creatureHandler.debug) Debug.Log("Hits: " + hits + " %: " + hits / (float) positionArrayLength * 100f);

        // Removes spots making the chunk unable to spawn new trees
        creatureSpots = null;
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
            if (Physics.Raycast(ray, out hit, creatureHandler.PlanetRadius))
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

    private CreaturePack GetCreatureToSpawn()
    {
        if (spawningRatios.Length != creatureHandler.packs.Length) Debug.Log("Creatures and ratios needs to be the same size");

        int total = 0;

        foreach (int ratio in spawningRatios)
        {
            total += ratio;
        }

        float randomNum = random.Next(0, total);

        float accumulatedSum = 0;

        for (int i = 0; i < spawningRatios.Length; i++)
        {
            if (randomNum > accumulatedSum)
            {
                accumulatedSum += spawningRatios[i];
            }
            else
            {
                return creatureHandler.packs[i];
            }
        }

        return creatureHandler.packs[0];
    }

    private int[] GetSpawningRatios()
    {
        int[] ratios = new int[creatureHandler.packs.Count()];

        for (int i = 0; i < creatureHandler.packs.Count(); i++)
        {
            ratios[i] = creatureHandler.packs[i].ratio;
        }

        return ratios;
    }
}
