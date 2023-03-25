using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using ExtendedRandom;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static ChunksHandler;

public class Chunk : MonoBehaviour
{
    [SerializeField] public CreatureSpawning creatures;
    [SerializeField] public Foliage foliage;

    [SerializeField] private GameObject foliageGameObject;
    [SerializeField] public GameObject creatureGameObject;

    private int index;

    // Resolutions of chunk
    private int currentRes;
    private ResolutionSetting highRes;
    private ResolutionSetting mediumRes;
    private ResolutionSetting lowRes;
    private Vector3 previousPlayerPos;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;
    public MarchingCubes marchingCubes;
    private Transform player;
    private Planet planet;
    private MinMaxTerrainLevel terrainLevel;
    [HideInInspector] public float chunkSize;

    private bool lowChunkResChunks;

    private RandomX random;

    [HideInInspector] public Vector3 position;
    [HideInInspector] public bool initialized = false;
    public bool debug = false;

    public void Setup(int index, MarchingCubes marchingCubes)
    {
        this.index = index;
        this.marchingCubes = marchingCubes;

        CalculateChunkPosition();
    }

    /// <summary>
    /// Initalizes a given chunk
    /// </summary>
    /// <param name="resolution"></param>
    /// <param name="player"></param>
    /// <param name="terrainLevel"></param>
    public int Initialize(Planet planet, Transform player, MinMaxTerrainLevel terrainLevel, ChunksHandler chunkHandler, int seed)
    {
        this.planet = planet;
        highRes = chunkHandler.highRes;
        mediumRes = chunkHandler.mediumRes;
        lowRes = chunkHandler.lowRes;
        random = new RandomX(seed);

        this.player = player;
        this.terrainLevel = terrainLevel;
        chunkSize = (2 * chunkHandler.planetRadius) / (1 << marchingCubes.chunkResolution);

        meshFilter = transform.GetComponent<MeshFilter>();
        if(marchingCubes.chunkResolution == 1)
        {
            lowChunkResChunks = true;
            GetComponent<MeshCollider>().enabled = false;
        }
        else
        {
            lowChunkResChunks = false;
            meshCollider = transform.GetComponent<MeshCollider>();
        }

        //Set lowest resolution as default
        int numVerts = UpdateMesh(lowRes.resolution);

        //previousPlayerPos = player.localPosition + new Vector3(100,100,100);
        previousPlayerPos = Vector3.zero;

        //if (!lowChunkResChunks && numVerts != 0)
            //UpdateChunk();

        initialized = true;

        return numVerts;
    }

    private void Update()
    {
        if(initialized && !lowChunkResChunks)
        {
            if (debug)
            {
                print("Chunk: " + index + "prev: " + previousPlayerPos);
                print("Curr: " + player.localPosition);
                print("Mag: " + Vector3.Magnitude(player.localPosition - previousPlayerPos));
            }
            // Check every 5 meter so that we don't check all the time
            if (!previousPlayerPos.Equals(Vector3.zero) && Vector3.Magnitude(player.localPosition - previousPlayerPos) < 5)
                return;
            
            previousPlayerPos = player.localPosition;

            UpdateChunk();
        }
    }

    private void UpdateChunk()
    {
        float playerDistance = Vector3.Magnitude(player.localPosition - position);
        print("P dist: " + playerDistance + " less than " + highRes.upperRadius * chunkSize);
        if (playerDistance < highRes.upperRadius * chunkSize)
        {
            meshCollider.enabled = true;
            foliageGameObject.SetActive(true);
            creatureGameObject.SetActive(true);

            if (planet.willGeneratePlanetLife)
            {
                int numVerts = UpdateMesh(highRes.resolution);

                if (!foliage.initialized)
                {
                    if (numVerts > 500)
                        foliage.Initialize(numVerts, position, random.Next());
                }

                if (!creatures.initialized)
                {
                    if (numVerts > 500)
                        creatures.Initialize(numVerts, position, random.Next());
                }

            }
            else
            {
                UpdateMesh(highRes.resolution);
            }
        }
        else if (mediumRes.lowerRadius * chunkSize < playerDistance && playerDistance < mediumRes.upperRadius * chunkSize)
        {
            foliageGameObject.SetActive(false);
            creatureGameObject.SetActive(false);
            meshCollider.enabled = false;
            UpdateMesh(mediumRes.resolution);

        }
        else if (lowRes.lowerRadius * chunkSize < playerDistance)
        {
            foliageGameObject.SetActive(false);
            creatureGameObject.SetActive(false);
            meshCollider.enabled = false;
            UpdateMesh(lowRes.resolution);
        }
    }

    /// <summary>
    /// Sets the material of the chunk
    /// </summary>
    public void SetMaterial(Material material) {
        GetComponent<MeshRenderer>().material = material;
    }

    private void CalculateChunkPosition()
    {
        // Extra the chunkindex in terms of x,y,z
        int mask = 0;
        for (int i = 0; i < marchingCubes.chunkResolution; i++)
        {
            mask += 1 << i;
        }

        Vector3 chunkIndex = 
            new Vector3(
                (index & (mask << (marchingCubes.chunkResolution * 0))) >> (marchingCubes.chunkResolution * 0),
                (index & (mask << (marchingCubes.chunkResolution * 1))) >> (marchingCubes.chunkResolution * 1),
                (index & (mask << (marchingCubes.chunkResolution * 2))) >> (marchingCubes.chunkResolution * 2)
            );
            
        position = -(chunkIndex - (Mathf.Pow(2, marchingCubes.chunkResolution) - 1) / 2 * Vector3.one) * (marchingCubes.radius * 2 / (1 << (marchingCubes.chunkResolution)));
    }

    private int UpdateMesh(int resolution)
    {
        if (currentRes == resolution)
            return 0;

        currentRes = resolution;

        mesh = new Mesh();

        int numVerts = marchingCubes.generateMesh(terrainLevel, index, currentRes, mesh);
       
        meshFilter.sharedMesh = mesh;

        if (marchingCubes.chunkResolution != 1 && meshCollider.enabled)
            meshCollider.sharedMesh = mesh;

        return numVerts;
    }
}
