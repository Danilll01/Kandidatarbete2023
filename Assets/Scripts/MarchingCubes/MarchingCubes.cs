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
    readonly float radius;

    Mesh mesh;

    public MarchingCubes(Mesh mesh, ComputeShader meshGenerator, float threshold, int resolution, float radius)
    {
        this.mesh = mesh;
        mesh.indexFormat = IndexFormat.UInt32;
        this.meshGenerator = meshGenerator;
        this.threshold = threshold;
        this.resolution = resolution;
        this.radius = radius;
    }

    public void generateMesh()
    {
        int numVoxelsPerAxis = (resolution << 3) - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        ComputeBuffer trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(int) * 3 * 3, ComputeBufferType.Append);
        trianglesBuffer.SetCounterValue(0);

        // Run generateMesh in compute shader
        int kernelIndex = meshGenerator.FindKernel("GenerateMesh");
        meshGenerator.SetInt("resolution", resolution << 3);
        meshGenerator.SetFloat("threshold", threshold);
        meshGenerator.SetFloat("radius", radius);
        meshGenerator.SetBuffer(kernelIndex, "triangles", trianglesBuffer);
        meshGenerator.Dispatch(kernelIndex, resolution, resolution, resolution);

        // Retrieve triangles
        int length = getLengthBuffer(ref trianglesBuffer) * 3 * 3;
        Triangle[] triangles = new Triangle[length];
        trianglesBuffer.GetData(triangles, 0, 0, length);

        trianglesBuffer.Release();

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

        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateBounds();
    }

    private int getLengthBuffer(ref ComputeBuffer buffer)
    {
        ComputeBuffer counter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        int[] count = { 0 };
        ComputeBuffer.CopyCount(buffer, counter, 0);
        counter.GetData(count);
        counter.Release();
        MonoBehaviour.print("We have: " + count[0] + " triangles");
        return count[0];
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
