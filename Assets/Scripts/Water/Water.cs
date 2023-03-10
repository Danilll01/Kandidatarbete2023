using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Water
{
    private Mesh mesh;
    private MeshFilter meshFilter;
    private int resolution;
    private int[] trianglesUp;
    private int[] trianglesDown;
    private Vector3 localUp, axisA, axisB;
    private ComputeShader computeShader;
    private float waterRadius;

    /// <summary>
    /// Initializes the water mesh
    /// </summary>
    /// <param name="computeShader"></param>
    /// <param name="meshFilter"></param>
    /// <param name="resolution"></param>
    /// <param name="waterRadius"></param>
    /// <param name="localUp"></param>
    public Water(ComputeShader computeShader, MeshFilter meshFilter, int resolution, float waterRadius, Vector3 localUp)
    {
        this.computeShader = computeShader;
        this.meshFilter = meshFilter;
        this.resolution = resolution;
        this.waterRadius = waterRadius;
        this.localUp = localUp;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    /// <summary>
    /// Constructs the mesh
    /// </summary>
    public void ConstructMesh()
    {
        mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[resolution * resolution];

        ConstructUnitSphere(vertices);

        mesh.vertices = vertices;
        mesh.triangles = trianglesUp;
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }

    /// <summary>
    /// Calls the GPU which creates the mesh
    /// </summary>
    /// <param name="vertices"></param>
    private void ConstructUnitSphere(Vector3[] vertices)
    {

        trianglesUp = new int[(resolution - 1) * (resolution - 1) * 2 * 3];
        trianglesDown = new int[(resolution - 1) * (resolution - 1) * 2 * 3];

        //Calculate the vertices on the GPU
        ComputeBuffer bufferVertices = new ComputeBuffer(resolution * resolution, 3 * sizeof(float));
        ComputeBuffer bufferTrianglesUp = new ComputeBuffer((resolution - 1) * (resolution - 1) * 2 * 3, sizeof(int));
        ComputeBuffer bufferTrianglesDown = new ComputeBuffer((resolution - 1) * (resolution - 1) * 2 * 3, sizeof(int));

        bufferVertices.SetData(vertices);
        bufferTrianglesUp.SetData(trianglesUp);
        bufferTrianglesDown.SetData(trianglesDown);

        int kernelId = computeShader.FindKernel("CSMesh");

        computeShader.SetBuffer(kernelId, "trianglesUp", bufferTrianglesUp);
        computeShader.SetBuffer(kernelId, "trianglesDown", bufferTrianglesDown);
        computeShader.SetBuffer(kernelId, "vertices", bufferVertices);

        computeShader.SetInt("resolution", resolution);
        computeShader.SetFloat("radius", waterRadius);

        computeShader.SetFloats("localUp", new float[] { localUp.x, localUp.y, localUp.z });
        computeShader.SetFloats("axisA", new float[] { axisA.x, axisA.y, axisA.z });
        computeShader.SetFloats("axisB", new float[] { axisB.x, axisB.y, axisB.z });

        computeShader.Dispatch(kernelId, resolution / 32, resolution / 32, 1);

        bufferVertices.GetData(vertices);
        bufferTrianglesUp.GetData(trianglesUp);
        bufferTrianglesDown.GetData(trianglesDown);

        bufferVertices.Dispose();
        bufferTrianglesUp.Dispose();
        bufferTrianglesDown.Dispose();
    }
    /// <summary>
    /// Checks which side of the water the mesh should be renderd on
    /// </summary>
    /// <param name="underWater"></param>
    public void UnderWater(bool underWater)
    {
        mesh.triangles = (underWater) ? trianglesDown : trianglesUp;
        mesh.RecalculateBounds();
        meshFilter.sharedMesh = mesh;
    }
}
