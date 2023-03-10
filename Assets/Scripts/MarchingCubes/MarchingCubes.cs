using JetBrains.Annotations;
using System;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MarchingCubes
{
    readonly ComputeShader meshGenerator;
    readonly float threshold;
    public readonly float diameter;
    readonly int frequency;
    readonly float amplitude;
    public int chunkResolution;

    AsyncGPUReadbackRequest asyncRequest;

    /// <summary>
    /// Initializes the MarchingCubes script
    /// </summary>
    /// <param name="meshGenerator">The meshgenerator compute shader</param>
    /// <param name="threshold">The cut off threshold to be used</param>
    /// <param name="diameter"></param>
    public MarchingCubes(int chunkResolution, ComputeShader meshGenerator, float threshold, float diameter, int frequency, float amplitude)
    {
        this.chunkResolution = chunkResolution;
        this.meshGenerator = meshGenerator;
        this.threshold = threshold;
        this.diameter = diameter;
        this.amplitude = amplitude;
        this.frequency = frequency;
    }

    /// <summary>
    /// Generate the mesh from the given parameters in the constructor
    /// </summary>
    public void generateMesh(MinMaxTerrainLevel hightFillerTerrainLevel, int index, int resolution, Mesh mesh)
    {
        resolution *= 1 << chunkResolution;

        // Calculate the total number of voxels and the max triangle count possible
        int numVoxelsPerAxis = ((resolution << 3) >> chunkResolution) - 1;
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
        meshGenerator.SetInt("chunkResolution", chunkResolution);
        meshGenerator.SetInt("resolution", resolution << 3);
        meshGenerator.SetFloat("threshold", threshold);
        meshGenerator.SetFloat("diameter", diameter);
        meshGenerator.SetBuffer(kernelIndex, "triangles", trianglesBuffer);
        meshGenerator.Dispatch(kernelIndex, resolution >> chunkResolution, resolution >> chunkResolution, resolution >> chunkResolution);

        
        // Retrieve triangles
        int length = getLengthBuffer(ref trianglesBuffer); // This is slow!!!

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
                hightFillerTerrainLevel.UpdateMinMax(triangles[i][j]);  //This is slow, need to implement fix so that this is only called the first time the chunks are created
                meshVertices[i * 3 + j] = triangles[i][j];
            }
        }

        // Set values in mesh
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateBounds();
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
}