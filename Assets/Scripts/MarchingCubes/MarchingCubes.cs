using System;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public class MarchingCubes
{
    readonly ComputeShader meshGenerator;
    readonly float threshold;
    //readonly int resolution;
    readonly float radius;
    readonly int frequency;
    readonly float amplitude;

    readonly int chunkResolution;
    List<Mesh> meshes;

    /// <summary>
    /// Initializes the MarchingCubes script
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="meshGenerator"></param>
    /// <param name="threshold"></param>
    /// <param name="radius"></param>
    public MarchingCubes(ref MeshFilter[] meshFilters, int chunkResolution, ComputeShader meshGenerator, float threshold, float radius, int frequency, float amplitude)
    {
        //this.meshes = meshes;
        meshes = new List<Mesh>();
        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            mesh.indexFormat = IndexFormat.UInt32;
            meshes.Add(mesh);   
        }
        this.chunkResolution = chunkResolution;
        this.meshGenerator = meshGenerator;
        this.threshold = threshold;
        //this.resolution = resolution;
        this.radius = radius;
        this.amplitude = amplitude;
        this.frequency = frequency;
    }

    /// <summary>
    /// Generate the mesh from the given parameters in the constructor
    /// </summary>
    public void generateMesh(int index, int resolution)
    {
        // Calculate the total number of voxels and the max triangle count possible
        int numVoxelsPerAxis = (resolution << 3) - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        // Set up buffers for the triangles
        ComputeBuffer trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(int) * 3 * 3, ComputeBufferType.Append);
        trianglesBuffer.SetCounterValue(0);

        // Run generateMesh in compute shader
        int kernelIndex = meshGenerator.FindKernel("GenerateMesh");
        meshGenerator.SetInt("frequency", frequency);
        meshGenerator.SetFloat("amplitude", amplitude);
        meshGenerator.SetInt("chunkIndex", index);

        meshGenerator.SetInt("resolution", resolution << 3);
        meshGenerator.SetFloat("threshold", threshold);
        meshGenerator.SetFloat("radius", radius);
        meshGenerator.SetBuffer(kernelIndex, "triangles", trianglesBuffer);
        meshGenerator.Dispatch(kernelIndex, resolution >> 2, resolution >> 2, resolution >> 2);

        // Retrieve triangles
        int length = getLengthBuffer(ref trianglesBuffer);
        Triangle[] triangles = new Triangle[length];
        trianglesBuffer.GetData(triangles, 0, 0, length);

        // Release all buffers
        trianglesBuffer.Release();

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
        Mesh mesh = meshes.ElementAt(index);
        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateBounds();
    }

    // Get the length buffer of type append
    private int getLengthBuffer(ref ComputeBuffer buffer)
    {
        ComputeBuffer counter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        int[] count = { 0 };
        ComputeBuffer.CopyCount(buffer, counter, 0);
        counter.GetData(count);
        counter.Release();
        //MonoBehaviour.print(count[0]);
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
}