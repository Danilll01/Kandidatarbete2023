using JetBrains.Annotations;
using System;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MarchingCubes
{
    readonly ComputeShader meshGenerator;
    readonly float threshold;
    public readonly float radius;
    public int chunkResolution;
    public float seed;

    private List<TerrainLayer> terrainLayers;
    private BiomeSettings biomeSettings;

    /// <summary>
    /// Initializes the MarchingCubes script
    /// </summary>
    /// <param name="seed">The seed for the planet</param>
    /// <param name="chunkResolution">The resolution of the chunks overall</param>
    /// <param name="meshGenerator">The computer shader for the marching cubes</param>
    /// <param name="threshold">Cutoffpoint for the terrain</param>
    /// <param name="radius">Radius of the planet</param>
    /// <param name="terrainLayers">The terrainlayers</param>
    /// <param name="biomeSettings">Settings for the biomes</param>
    public MarchingCubes(float seed, int chunkResolution, ComputeShader meshGenerator, float threshold, float radius, List<TerrainLayer> terrainLayers, BiomeSettings biomeSettings)
    {
        this.seed = seed;
        this.chunkResolution = chunkResolution;
        this.meshGenerator = meshGenerator;
        this.threshold = threshold;
        this.radius = radius;
        this.terrainLayers = terrainLayers;
        this.biomeSettings = biomeSettings;
    }

    /// <summary>
    /// Generate the mesh from the given parameters in the constructor while updating min/max terrain levels
    /// </summary>
    public int generateMesh(MinMaxTerrainLevel hightFillerTerrainLevel, int index, int resolution, Mesh mesh)
    {
        int vertexCount = generateMesh(index, resolution, mesh);

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            
            hightFillerTerrainLevel.UpdateMinMax(mesh.vertices[i]);
        }

        return vertexCount;
    }

    /// <summary>
    /// Generate the mesh from the given parameters in the constructor
    /// </summary>
    public int generateMesh(int index, int resolution, Mesh mesh)
    {
        resolution *= 1 << chunkResolution;

        // Calculate the total number of voxels and the max triangle count possible
        int numVoxelsPerAxis = ((resolution << 3) >> chunkResolution) - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        // Set up buffers for the triangles
        ComputeBuffer trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(int) * 3 * 3, ComputeBufferType.Append);
        trianglesBuffer.SetCounterValue(0);

        // Set up buffer for the terrain layers
        ComputeBuffer layersBuffer = new ComputeBuffer(terrainLayers.Count, sizeof(float) * 10 + sizeof(int));
        layersBuffer.SetData(terrainLayers.ToArray());

        // Set up buffer for the Biome settings
        ComputeBuffer biomesBuffer = new ComputeBuffer(1, sizeof(float) * 8);
        biomesBuffer.SetData(biomeSettings.ToArray());

        // Run generateMesh in compute shader
        int kernelIndex = meshGenerator.FindKernel("GenerateMesh");
        meshGenerator.SetFloat("seed", seed);
        meshGenerator.SetInt("chunkIndex", index);
        meshGenerator.SetInt("chunkResolution", chunkResolution);
        meshGenerator.SetInt("resolution", resolution << 3);
        meshGenerator.SetFloat("threshold", threshold);
        meshGenerator.SetFloat("radius", radius);
        meshGenerator.SetBuffer(kernelIndex, "triangles", trianglesBuffer);
        meshGenerator.SetInt("numTerrainLayers", terrainLayers.Count);
        meshGenerator.SetBuffer(kernelIndex, "terrainLayers", layersBuffer);
        meshGenerator.SetBuffer(kernelIndex, "biomeSettings", biomesBuffer);
        meshGenerator.Dispatch(kernelIndex, resolution >> chunkResolution, resolution >> chunkResolution, resolution >> chunkResolution);

        // Retrieve triangles
        int length = getLengthBuffer(ref trianglesBuffer); // This is slow!!!

        Triangle[] triangles = new Triangle[length];
        trianglesBuffer.GetData(triangles, 0, 0, length);
        

        // Release all buffers
        trianglesBuffer.Release();
        layersBuffer.Release();
        biomesBuffer.Release();

        // Process our data from the compute shader
        int[] meshTriangles = new int[length * 3];
        Vector3[] meshVertices = new Vector3[length * 3];

        
        // Set values for the meshtriangles and meshvertices arrays
        for (int i = 0; i < length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                meshVertices[i * 3 + j] = triangles[i][j];
            }
        }

        // Set values in mesh
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return meshVertices.Length;
    }

    // Get the length buffer of type append
    private int getLengthBuffer(ref ComputeBuffer buffer)
    {
        
        ComputeBuffer counter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        int[] count = { 0 };
        ComputeBuffer.CopyCount(buffer, counter, 0);
        counter.GetData(count); //This call takes alot of time!!
        counter.Release();
        return count[0];
    }

    // Triangle struct with three points
    struct Triangle
    {
        public Vector3 vertexA, vertexB, vertexC;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return vertexA;
                    case 1: return vertexB;
                    default: return vertexC;
                }
            }
        }
    }

    public class ChunkGPUCallbackData
    {
        public MinMaxTerrainLevel terrainLevel;
        public Mesh mesh;
        public ComputeBuffer[] buffers; //triangle, layers, biomes

        public ChunkGPUCallbackData(MinMaxTerrainLevel terrainLevel, Mesh mesh, ComputeBuffer[] buffers)
        {
            this.terrainLevel = terrainLevel;
            this.mesh = mesh;
            this.buffers = buffers;
        }
    }
}