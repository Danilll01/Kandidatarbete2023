using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Chunk : MonoBehaviour
{
    [SerializeField] public Transform creatures;
    [SerializeField] public Foliage foliage;

    [SerializeField] private GameObject foliageGameObject;
    [SerializeField] private GameObject creatureGameObject;

    private int index;

    // Resolutions of chunk
    private int currentRes;
    private int highRes;
    private int mediumRes;
    private int lowRes;
    private bool updated = false;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;
    public MarchingCubes marchingCubes;
    private Transform player;
    private MinMaxTerrainLevel terrainLevel;
    private float chunkSize;

    ChunksHandler chunkHandler;

    [HideInInspector] public Vector3 position;
    [HideInInspector] public bool initialized = false;

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
    public int Initialize(Transform player, MinMaxTerrainLevel terrainLevel, ChunksHandler chunkHandler)
    {
        highRes = chunkHandler.highRes;
        mediumRes = chunkHandler.mediumRes;
        lowRes = chunkHandler.lowRes;

        this.chunkHandler = chunkHandler;
        this.player = player;
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
        int numVerts = UpdateMesh(lowRes);
        //if (meshVerticesLength > 500 && marchingCubes.chunkResolution == chunkHandler.highChunkRes)
        //{
        //    foliage.Initialize(meshVerticesLength, position);
        //}

        initialized = true;

        return numVerts;
    }

    private void Update()
    {
        if(marchingCubes.chunkResolution == chunkHandler.highChunkRes && initialized)
        {
            float playerDistance = Vector3.Magnitude(player.localPosition - position);

            if (playerDistance < 1.3 * chunkSize)
            {
                meshCollider.enabled = true;
                foliageGameObject.SetActive(true);
                creatureGameObject.SetActive(true);
                if (!foliage.initialized)
                {
                    int numVerts = UpdateMesh(highRes);
                    if (numVerts > 500)
                        foliage.Initialize(numVerts, position);
                }
                else
                    UpdateMesh(highRes);
                    
            } 
            else if (1.5 * chunkSize < playerDistance && playerDistance < 2 * chunkSize)
            {
                foliageGameObject.SetActive(false);
                creatureGameObject.SetActive(false);
                meshCollider.enabled = false;
                UpdateMesh(mediumRes);
                
            }
            else if (2.3 * chunkSize < playerDistance)
            {
                foliageGameObject.SetActive(false);
                creatureGameObject.SetActive(false);
                meshCollider.enabled = false;
                UpdateMesh(lowRes);
            }
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

    public int UpdateMesh(int resolution)
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
