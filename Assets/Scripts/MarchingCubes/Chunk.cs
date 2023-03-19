using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Chunk : MonoBehaviour
{
    [SerializeField] public Transform creatures;
    [SerializeField] public Foliage foliage;

    private int index;
    private int resolution;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;
    public MarchingCubes marchingCubes;
    private Transform player;
    private MinMaxTerrainLevel terrainLevel;

    [HideInInspector] public Vector3 position;

    public void Setup(int index, MarchingCubes marchingCubes)
    {
        this.index = index;
        this.marchingCubes = marchingCubes;
    }

    /// <summary>
    /// Initalizes a given chunk
    /// </summary>
    /// <param name="index">Chunk index</param>
    /// <param name="resolution"></param>
    /// <param name="marchingCubes">An instance of marching cubes</param>
    /// <param name="player"></param>
    /// <param name="terrainLevel"></param>
    public int Initialize(int resolution, Transform player, MinMaxTerrainLevel terrainLevel)
    {
        this.player = player;
        this.terrainLevel = terrainLevel;

        meshFilter = transform.GetComponent<MeshFilter>();
        meshCollider = transform.GetComponent<MeshCollider>();

        CalculateChunkPosition();
        
        //Set lowest resolution as default
        int meshVerticesLength = UpdateMesh(resolution);
        if (meshVerticesLength > 1000 && marchingCubes.chunkResolution == 3)
        {
            foliage.Initialize(meshVerticesLength, position);
        }

        return meshVerticesLength;
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
        if (this.resolution == resolution)
            return 0;

        this.resolution = resolution;

        mesh = new Mesh();

        int numVerts = marchingCubes.generateMesh(terrainLevel, index, resolution, mesh);
       
        meshFilter.sharedMesh = mesh;

        meshCollider.sharedMesh = mesh;

        return numVerts;
    }
}
