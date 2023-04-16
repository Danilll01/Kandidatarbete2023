using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Water
{
    private Mesh mesh;
    private MeshFilter meshFilter;
    public int resolution;
    private int[] trianglesUp;
    private Vector3 localUp, axisA, axisB;
    private ComputeShader computeShader;
    private float waterRadius;
    private int position;
    private int sideResolution;
    public Vector3 waterPos;
    public GameObject parent;

    /// <summary>
    /// Initializes the water mesh
    /// </summary>
    /// <param name="computeShader"></param>
    /// <param name="meshFilter"></param>
    /// <param name="resolution"></param>
    /// <param name="waterRadius"></param>
    /// <param name="localUp"></param>
    public Water(ComputeShader computeShader, MeshFilter meshFilter, int resolution, float waterRadius, Vector3 localUp, int position, int sideResolution, GameObject parent)
    {
        this.computeShader = computeShader;
        this.meshFilter = meshFilter;
        this.resolution = resolution;
        this.waterRadius = waterRadius;
        this.localUp = localUp;
        this.position = position;
        this.sideResolution = sideResolution;
        this.parent = parent;


        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);

        waterPos = (localUp + 
            (1 / (2.0f * sideResolution) + (position % sideResolution) / (float)sideResolution - .5f) * 2 * axisA +
            (1 / (2.0f * sideResolution) + (position / sideResolution) / (float)sideResolution - .5f) * 2 * axisB).normalized * waterRadius;

    }

    /// <summary>
    /// Constructs the mesh
    /// </summary>
    public void ConstructMesh()
    {
        mesh = new Mesh{indexFormat = IndexFormat.UInt32};

        Vector3[] vertices = new Vector3[resolution * resolution];

        ConstructUnitSphere(vertices);

        mesh.vertices = vertices;
        mesh.triangles = trianglesUp;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        Object.Destroy(meshFilter.sharedMesh);
        meshFilter.sharedMesh = mesh;

    }

    /// <summary>
    /// Calls the GPU which creates the mesh
    /// </summary>
    /// <param name="vertices"></param>
    private void ConstructUnitSphere(Vector3[] vertices)
    {
        trianglesUp   = new int[(resolution - 1) * (resolution - 1) * 2 * 3];

        //Calculate the vertices on the GPU
        ComputeBuffer bufferVertices = new ComputeBuffer(resolution * resolution, 3 * sizeof(float));
        ComputeBuffer bufferTrianglesUp = new ComputeBuffer((resolution - 1) * (resolution - 1) * 2 * 3, sizeof(int));
        ComputeBuffer bufferTrianglesDown = new ComputeBuffer((resolution - 1) * (resolution - 1) * 2 * 3, sizeof(int));

        bufferVertices.SetData(vertices);
        bufferTrianglesUp.SetData(trianglesUp);

        int kernelId = computeShader.FindKernel("CSMesh");

        computeShader.SetBuffer(kernelId, "trianglesUp", bufferTrianglesUp);
        computeShader.SetBuffer(kernelId, "trianglesDown", bufferTrianglesDown);
        computeShader.SetBuffer(kernelId, "vertices", bufferVertices);

        computeShader.SetInt("resolution", resolution);
        computeShader.SetInt("sideResolution", sideResolution);
        computeShader.SetInt("posx", position % sideResolution);
        computeShader.SetInt("posy", position / sideResolution);
        computeShader.SetFloat("radius", waterRadius);

        computeShader.SetFloats("localUp", new float[] { localUp.x, localUp.y, localUp.z });
        computeShader.SetFloats("axisA", new float[] { axisA.x, axisA.y, axisA.z });
        computeShader.SetFloats("axisB", new float[] { axisB.x, axisB.y, axisB.z });

        computeShader.Dispatch(kernelId, resolution / 32, resolution / 32, 1);

        bufferVertices.GetData(vertices);
        bufferTrianglesUp.GetData(trianglesUp);

        bufferVertices.Dispose();
        bufferTrianglesUp.Dispose();
        bufferTrianglesDown.Dispose();
    }
}
