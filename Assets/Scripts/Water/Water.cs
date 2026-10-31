using UnityEngine;
using UnityEngine.Rendering;

public class Water
{

    private Vector3 localUp, axisA, axisB;
    public Vector3 waterPos;

    private readonly float waterRadius;
    private readonly int position;
    private readonly int sideResolution;
    public int resolution;
    public bool highRes = false;

    private readonly ComputeShader computeShader;
    private MeshFilter meshFilter;
    private Mesh meshLow;
    private Mesh meshHigh;


    /// <summary>
    /// Initializes the water mesh
    /// </summary>
    public Water(ComputeShader computeShader, MeshFilter meshFilter, int resolution, float waterRadius, Vector3 localUp, int position, int sideResolution)
    {
        this.computeShader = computeShader;
        this.meshFilter = meshFilter;
        this.resolution = resolution;
        this.waterRadius = waterRadius;
        this.localUp = localUp;
        this.position = position;
        this.sideResolution = sideResolution;


        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);

        waterPos = (localUp +
            (1 / (2.0f * sideResolution) + (position % sideResolution) / (float)sideResolution - .5f) * 2 * axisA +
            (1 / (2.0f * sideResolution) + (position / sideResolution) / (float)sideResolution - .5f) * 2 * axisB).normalized * waterRadius;
    }

    /// <summary>
    /// Constructs both mesh resolutions using async GPU readback (WebGPU compatible).
    /// High-res mesh is applied as soon as its callback fires.
    /// </summary>
    public void ConstructMesh()
    {
        // Only the high resolution mesh is built. A low resolution one was built too, but nothing ever switched to it.
        ConstructMeshAsync(resolution, (mesh) =>
        {
            meshHigh = mesh;
            ApplyMesh(true);
        });
    }

    private void ConstructMeshAsync(int res, System.Action<Mesh> onComplete)
    {
        int vertCount = res * res;
        int triCount = (res - 1) * (res - 1) * 2 * 3;

        Vector3[] vertices = new Vector3[vertCount];
        int[] trianglesUp = new int[triCount];

        // Set up GPU buffers. The compute shader writes every vertex and index, so nothing needs to be uploaded first
        // (uploading the empty arrays for every water patch at once overflowed the graphics ring buffer)
        ComputeBuffer bufferVertices = new ComputeBuffer(vertCount, 3 * sizeof(float));
        ComputeBuffer bufferTrianglesUp = new ComputeBuffer(triCount, sizeof(int));

        int kernelId = computeShader.FindKernel("CSMesh");

        computeShader.SetBuffer(kernelId, "trianglesUp", bufferTrianglesUp);
        computeShader.SetBuffer(kernelId, "vertices", bufferVertices);

        computeShader.SetInt("resolution", res);
        computeShader.SetInt("sideResolution", sideResolution);
        computeShader.SetInt("posx", position % sideResolution);
        computeShader.SetInt("posy", position / sideResolution);
        computeShader.SetFloat("radius", waterRadius);

        computeShader.SetFloats("localUp", new float[] { localUp.x, localUp.y, localUp.z });
        computeShader.SetFloats("axisA", new float[] { axisA.x, axisA.y, axisA.z });
        computeShader.SetFloats("axisB", new float[] { axisB.x, axisB.y, axisB.z });

        computeShader.Dispatch(kernelId, res / 32, res / 32, 1);

        // Async readback for vertices
        AsyncGPUReadback.Request(bufferVertices, (AsyncGPUReadbackRequest vertRequest) =>
        {
            if (vertRequest.hasError)
            {
                Debug.LogError("GPU readback error on water vertices.");
                bufferVertices.Dispose();
                bufferTrianglesUp.Dispose();
                return;
            }

            vertRequest.GetData<Vector3>().CopyTo(vertices);

            // Chain: now read back triangles
            AsyncGPUReadback.Request(bufferTrianglesUp, (AsyncGPUReadbackRequest triRequest) =>
            {
                bufferVertices.Dispose();
                bufferTrianglesUp.Dispose();

                if (triRequest.hasError)
                {
                    Debug.LogError("GPU readback error on water triangles.");
                    return;
                }

                triRequest.GetData<int>().CopyTo(trianglesUp);

                // Build and deliver the mesh
                Mesh mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.vertices = vertices;
                mesh.triangles = trianglesUp;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                onComplete?.Invoke(mesh);
            });
        });
    }


    public void ApplyMesh(bool highResolution)
    {
        if(highResolution)
        {
            meshFilter.sharedMesh = meshHigh;
            highRes = true;
        }
        else if (meshLow != null)
        {
            meshFilter.sharedMesh = meshLow;
            highRes = false;
        }

    }
}
