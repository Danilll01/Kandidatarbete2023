using ExtendedRandom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureSpawning : MonoBehaviour
{


    public bool initialized = false;

    // Spawning spots
    private Vector3[] creatureSpots = null;

    private CreatureHandler creatureHandler;
    private RandomX random;
    private Vector3 chunkPosition;
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
        Debug.Log("NOWDNDOWAD");
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
            if (hit.transform == transform.parent)
            {
                SpawnPack(hit, rayOrigin);
                
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

    private void SpawnPack(RaycastHit hit, Vector3 rayOrigin)
    {

    }
}
