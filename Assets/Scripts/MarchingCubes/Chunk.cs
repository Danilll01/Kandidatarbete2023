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

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;
    public MarchingCubes marchingCubes;
    private PillPlayerController player;
    private Planet planet;
    private MinMaxTerrainLevel terrainLevel;
    [HideInInspector] public float chunkSize;

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
            GetComponent<MeshCollider>().enabled = false;
        }
        else
        {
            meshCollider = transform.GetComponent<MeshCollider>();
        }

        //Set lowest resolution as default
        int numVerts = UpdateMesh(lowRes.resolution);

        initialized = true;

        return numVerts;
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

        Destroy(meshFilter.sharedMesh);

        meshFilter.sharedMesh = mesh;

        if (marchingCubes.chunkResolution != 1 && meshCollider.enabled)
            meshCollider.sharedMesh = mesh;

        return numVerts;
    }

    public void UpdateMesh(Mesh mesh, int resolution)
    {
        currentRes = resolution;

        Destroy(meshFilter.sharedMesh);

        meshFilter.sharedMesh = mesh;

        if (marchingCubes.chunkResolution != 1 && meshCollider.enabled)
            meshCollider.sharedMesh = mesh;

        int numVerts = mesh.vertexCount;
        
        if (resolution == highRes.resolution && planet.willGeneratePlanetLife)
        {
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
        }
    }

    public void SetActivated(bool state)
    {
        meshCollider.enabled = state;
        foliageGameObject.SetActive(state);
        creatureGameObject.SetActive(state);
    }

    public bool HasResolution(int resolution)
    {
        return currentRes == resolution;
    }

    public int Index
    {
        get { return index; }
    }
}
