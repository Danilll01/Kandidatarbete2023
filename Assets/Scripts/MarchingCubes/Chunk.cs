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

    private int index;
    private int resolution;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;
    private MarchingCubes marchingCubes;
    private Transform player;
    private MinMaxTerrainLevel terrainLevel;

    [HideInInspector] public Vector3 position;

    /// <summary>
    /// Initalizes a given chunk
    /// </summary>
    /// <param name="index">Chunk index</param>
    /// <param name="resolution"></param>
    /// <param name="marchingCubes">An instance of marching cubes</param>
    /// <param name="player"></param>
    /// <param name="terrainLevel"></param>
    public void Initialize(int index, int resolution, MarchingCubes marchingCubes, Transform player, MinMaxTerrainLevel terrainLevel)
    {
        this.index = index;
        this.marchingCubes = marchingCubes;
        this.player = player;
        this.terrainLevel = terrainLevel;

        meshFilter = transform.GetComponent<MeshFilter>();
        meshCollider = transform.GetComponent<MeshCollider>();

        calculateChunkPosition();

        //Set lowest resolution as default
        updateMesh(resolution);
    }

    /// <summary>
    /// Sets the material of the chunk
    /// </summary>
    public void SetMaterial(Material material) {
        GetComponent<MeshRenderer>().material = material;
    }

    private void calculateChunkPosition()
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

        // Use the chunkindex to calculate the position of the chunk
        position = -(chunkIndex - 1.5f * Vector3.one) * (marchingCubes.radius * 2 / (1 << (marchingCubes.chunkResolution)));
    }

    public void updateMesh(int resolution)
    {
        if (this.resolution == resolution)
            return;

        this.resolution = resolution;

        mesh = new Mesh();

        
        marchingCubes.generateMesh(terrainLevel, index, resolution, mesh);
        

        meshFilter.sharedMesh = mesh;

        meshCollider.sharedMesh = mesh;
    }
}
