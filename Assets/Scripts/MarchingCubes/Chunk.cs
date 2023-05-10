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
    private PillPlayerController player;
    private Planet planet;
    private MinMaxTerrainLevel terrainLevel;
    [HideInInspector] public float chunkSize;

    private bool lowChunkResChunks;

    private RandomX random;

    [HideInInspector] public Vector3 position;
    [HideInInspector] public bool initialized = false;

    public void Setup(int index, MarchingCubes marchingCubes)
    {
        this.index = index;
        this.marchingCubes = marchingCubes;

        CalculateChunkPosition();
    }

    /// <summary>
    /// Initializes a given chunk
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="player"></param>
    /// <param name="terrainLevel"></param>
    /// <param name="chunkHandler"></param>
    /// <param name="seed"></param>
    /// <returns></returns>
    public int Initialize(Planet planet, MinMaxTerrainLevel terrainLevel, ChunksHandler chunkHandler, int seed)
    {
        this.planet = planet;
        highRes = chunkHandler.highRes;
        mediumRes = chunkHandler.mediumRes;
        lowRes = chunkHandler.lowRes;
        random = new RandomX(seed);

        this.player = Universe.player;
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

        initialized = true;

        return numVerts;
    }

    private void Update()
    {
        if (!initialized || lowChunkResChunks) return;
        
        Vector3 playerPos = player.boarded ? Universe.spaceShip.localPosition : player.transform.localPosition;
            
        // Check every 5 meter so that we don't check all the time
        if (Vector3.Magnitude(playerPos - previousPlayerPos) < 2)
            return;
            
        previousPlayerPos = playerPos;

        float playerDistance = Vector3.Magnitude(playerPos - position);
        if (playerDistance < highRes.upperRadius)
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
                        foliage.Initialize(numVerts, position, random.Next(), planet);
                }

                if (!creatures.initialized)
                {
                    if (numVerts > 500)
                        creatures.Initialize(numVerts, position, random.Next());
                }
                if (!creatures.finishedSpawning)
                {
                    creatures.BatchedSpawning();
                }
                foliage.BatchedSpawning();

            } else
            {
                UpdateMesh(highRes.resolution);
            }
                    
                    
        } 
        else if (mediumRes.lowerRadius < playerDistance && playerDistance < mediumRes.upperRadius)
        {
            foliageGameObject.SetActive(false);
            creatureGameObject.SetActive(false);
            meshCollider.enabled = false;
            UpdateMesh(mediumRes.resolution);
                
        }
        else if (lowRes.lowerRadius < playerDistance)
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

        int numVerts = marchingCubes.generateMesh(terrainLevel, index, currentRes, mesh, !lowChunkResChunks);

        Destroy(meshFilter.sharedMesh);

        meshFilter.sharedMesh = mesh;

        if (marchingCubes.chunkResolution != 1 && meshCollider.enabled)
            meshCollider.sharedMesh = mesh;

        return numVerts;
    }
}
