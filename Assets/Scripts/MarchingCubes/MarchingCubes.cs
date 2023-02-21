using System;

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

public class MarchingCubes
{
    readonly ComputeShader meshGenerator;
    readonly float threshold;
    readonly int resolution;
    readonly float diameter;
    readonly int frequency;
    readonly float amplitude;
    readonly float bottomLevel;

    Mesh mesh;

    /// <summary>
    /// Initializes the MarchingCubes script
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="meshGenerator"></param>
    /// <param name="threshold"></param>
    /// <param name="resolution"></param>
    /// <param name="radius"></param>
    public MarchingCubes(Mesh mesh, ComputeShader meshGenerator, float threshold, int resolution, float diameter, int frequency, float amplitude, float bottomLevel)
    {
        this.mesh = mesh;
        mesh.indexFormat = IndexFormat.UInt32;
        this.meshGenerator = meshGenerator;
        this.threshold = threshold;
        this.resolution = resolution;
        this.diameter = diameter;
        this.amplitude = amplitude;
        this.frequency = frequency;
        this.bottomLevel = bottomLevel;
    }

    /// <summary>
    /// Generate the mesh from the given parameters in the constructor
    /// </summary>
    public void generateMesh()
    {
        // Calculate the total number of voxels and the max triangle count possible
        int numVoxelsPerAxis = (resolution << 3) - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;
        int verticesCount = (resolution - 1) * 3 * resolution * resolution;

        // Set up buffers for the triangles
        ComputeBuffer verticesBuffer = new ComputeBuffer(numVoxels * 12, 3 * sizeof(float));
        ComputeBuffer trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(int) * 3, ComputeBufferType.Append);
        trianglesBuffer.SetCounterValue(0);

        // Run generateMesh in compute shader
        int kernelIndex = meshGenerator.FindKernel("GenerateMesh");

        meshGenerator.SetInt("frequency", frequency);
        meshGenerator.SetFloat("amplitude", amplitude);
        meshGenerator.SetFloat("planetBottom", bottomLevel);
        meshGenerator.SetInt("resolution", resolution << 3);
        meshGenerator.SetFloat("threshold", threshold);
        meshGenerator.SetFloat("diameter", diameter);

        meshGenerator.SetBuffer(kernelIndex, "vertices", verticesBuffer);
        meshGenerator.SetBuffer(kernelIndex, "triangles", trianglesBuffer);
        meshGenerator.Dispatch(kernelIndex, resolution, resolution, resolution);

        // Retrieve triangles
        int length = getLengthBuffer(ref trianglesBuffer);
        Triangle[] triangles = new Triangle[length];
        trianglesBuffer.GetData(triangles, 0, 0, length);
        trianglesBuffer.Release();

        // Retrieve vertices
        Vector3[] vertices = new Vector3[numVoxels * 12];
        verticesBuffer.GetData(vertices);
        verticesBuffer.Release();

        int[] meshTriangles = new int[length * 3];

        for (int i = 0; i < length; i++)
        {
            meshTriangles[i] = triangles[i].vertexA;
            meshTriangles[i + 1] = triangles[i].vertexB;
            meshTriangles[i + 2] = triangles[i].vertexC;
        }

        // Set values in mesh
        mesh.Clear();
        mesh.vertices = vertices;
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
        public int vertexA, vertexB, vertexC;
    }
}
