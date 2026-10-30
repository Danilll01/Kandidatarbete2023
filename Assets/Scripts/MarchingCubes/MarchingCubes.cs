using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class MarchingCubes
{
    readonly ComputeShader meshGenerator;
    readonly float threshold;
    public readonly float radius;
    public int chunkResolution;
    public float seed;

    private List<TerrainLayer> terrainLayers;
    private BiomeSettings biomeSettings;

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
    /// Generate mesh and update min/max terrain levels. Uses explicit chunkRes to avoid shared state issues.
    /// </summary>
    public void generateMesh(MinMaxTerrainLevel terrainLevel, int index, int resolution, int chunkRes, Action<Mesh, int> onComplete)
    {
        generateMesh(index, resolution, chunkRes, (mesh, vertexCount) =>
        {
            if (mesh != null)
            {
                Vector3[] verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    terrainLevel.UpdateMinMax(verts[i]);
                }
            }

            onComplete?.Invoke(mesh, vertexCount);
        });
    }

    /// <summary>
    /// Generate mesh using explicit chunkRes parameter. WebGPU compatible (callback-based async readback).
    /// </summary>
    public void generateMesh(int index, int resolution, int chunkRes, Action<Mesh, int> onComplete)
    {
        resolution *= 1 << chunkRes;

        // Calculate the total number of voxels and the max triangle count possible
        int numVoxelsPerAxis = ((resolution << 3) >> chunkRes) - 1;
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
        meshGenerator.SetInt("chunkResolution", chunkRes);
        meshGenerator.SetInt("resolution", resolution << 3);
        meshGenerator.SetFloat("threshold", threshold);
        meshGenerator.SetFloat("radius", radius);
        meshGenerator.SetBuffer(kernelIndex, "triangles", trianglesBuffer);
        meshGenerator.SetInt("numTerrainLayers", terrainLayers.Count);
        meshGenerator.SetBuffer(kernelIndex, "terrainLayers", layersBuffer);
        meshGenerator.SetBuffer(kernelIndex, "biomeSettings", biomesBuffer);
        meshGenerator.Dispatch(kernelIndex, resolution >> chunkRes, resolution >> chunkRes, resolution >> chunkRes);

        // Read back counter asynchronously
        ComputeBuffer counter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(trianglesBuffer, counter, 0);

        AsyncGPUReadback.Request(counter, (AsyncGPUReadbackRequest counterRequest) =>
        {
            counter.Release();

            if (counterRequest.hasError)
            {
                Debug.LogError("GPU readback error on getting buffer length.");
                trianglesBuffer.Release();
                layersBuffer.Release();
                biomesBuffer.Release();
                onComplete?.Invoke(null, 0);
                return;
            }

            int length = counterRequest.GetData<int>()[0];

            if (length == 0)
            {
                trianglesBuffer.Release();
                layersBuffer.Release();
                biomesBuffer.Release();
                onComplete?.Invoke(null, 0);
                return;
            }

            // Read back triangle data asynchronously
            AsyncGPUReadback.Request(trianglesBuffer, length * sizeof(float) * 9, 0, (AsyncGPUReadbackRequest triRequest) =>
            {
                trianglesBuffer.Release();
                layersBuffer.Release();
                biomesBuffer.Release();

                if (triRequest.hasError)
                {
                    Debug.LogError("GPU readback error on triangles.");
                    onComplete?.Invoke(null, 0);
                    return;
                }

                var triangles = triRequest.GetData<Triangle>().ToArray();

                // Process our data from the compute shader
                int[] meshTriangles = new int[length * 3];
                Vector3[] meshVertices = new Vector3[length * 3];

                for (int i = 0; i < length; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        meshTriangles[i * 3 + j] = i * 3 + j;
                        meshVertices[i * 3 + j] = triangles[i][j];
                    }
                }

                Mesh mesh = new Mesh();
                mesh.indexFormat = IndexFormat.UInt32;
                mesh.vertices = meshVertices;
                mesh.triangles = meshTriangles;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                onComplete?.Invoke(mesh, meshVertices.Length);
            });
        });
    }

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
}
