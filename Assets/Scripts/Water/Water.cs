using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Water
{

    MeshFilter meshFilter;
    int resolution;
    Vector3 localUp, axisA, axisB;
    ComputeShader computeShader;
    float waterRadius;

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

    public void ConstructMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 2 * 3];

        constructUnitSphere(vertices, triangles);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
    }

    private void constructUnitSphere(Vector3[] vertices, int[] triangles)
    {
        //Calculate the vertices on the GPU
        ComputeBuffer bufferVertices = new ComputeBuffer(resolution * resolution, 3 * sizeof(float));
        ComputeBuffer bufferTriangles = new ComputeBuffer((resolution - 1) * (resolution - 1) * 2 * 3, sizeof(int));

        bufferVertices.SetData(vertices);
        bufferTriangles.SetData(triangles);

        int kernelId = computeShader.FindKernel("CSMesh");

        computeShader.SetBuffer(kernelId, "triangles", bufferTriangles);
        computeShader.SetBuffer(kernelId, "vertices", bufferVertices);

        computeShader.SetInt("resolution", resolution);
        computeShader.SetFloat("radius", waterRadius);

        computeShader.SetFloats("localUp", new float[] { localUp.x, localUp.y, localUp.z });
        computeShader.SetFloats("axisA", new float[] { axisA.x, axisA.y, axisA.z });
        computeShader.SetFloats("axisB", new float[] { axisB.x, axisB.y, axisB.z });

        computeShader.Dispatch(kernelId, resolution / 32, resolution / 32, 1);

        bufferVertices.GetData(vertices);
        bufferTriangles.GetData(triangles);

        bufferVertices.Dispose();
        bufferTriangles.Dispose();
    }
}
