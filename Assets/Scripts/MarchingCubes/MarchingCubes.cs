using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class MarchingCubes : IDisposable
{
    // A surface cutting through a chunk of n^3 voxels touches roughly n^2 voxels, so the triangle buffer
    // is sized for that instead of the n^3 * 5 worst case. Chunks that overflow it are regenerated with the worst case size.
    private const int surfaceTrianglesPerVoxelSlice = 12;
    private const int triangleStride = sizeof(float) * 3 * 3;

    readonly ComputeShader meshGenerator;
    readonly float threshold;
    public readonly float radius;
    public float seed;

    private List<TerrainLayer> terrainLayers;
    private BiomeSettings biomeSettings;

    private int meshKernel = -1;
    private int heightKernel = -1;
    private ComputeBuffer layersBuffer;
    private ComputeBuffer biomesBuffer;
    private readonly Dictionary<int, Stack<ComputeBuffer>> trianglePool = new Dictionary<int, Stack<ComputeBuffer>>();
    private readonly Stack<ComputeBuffer> counterPool = new Stack<ComputeBuffer>();
    private readonly Stack<ComputeBuffer> heightPool = new Stack<ComputeBuffer>();
    private bool disposed;

    public MarchingCubes(float seed, ComputeShader meshGenerator, float threshold, float radius, List<TerrainLayer> terrainLayers, BiomeSettings biomeSettings)
    {
        this.seed = seed;
        this.meshGenerator = meshGenerator;
        this.threshold = threshold;
        this.radius = radius;
        this.terrainLayers = terrainLayers;
        this.biomeSettings = biomeSettings;
    }

    /// <summary>
    /// Conservative bounds of the surface distance from the planet centre, used to skip chunks that can't contain any terrain
    /// </summary>
    public void GetSurfaceBounds(out float minRadius, out float maxRadius)
    {
        float low = 0, high = 0;
        foreach (TerrainLayer layer in terrainLayers)
        {
            // Each layer is (mountainGround * mountainHeight + flatGround) * strength with both grounds in [0, 1]
            float a = Mathf.Min(0, layer.mountainHeight) * layer.strength;
            float b = (Mathf.Max(0, layer.mountainHeight) + 1) * layer.strength;
            low += Mathf.Min(a, b);
            high += Mathf.Max(a, b);
        }

        minRadius = (.7f + low - threshold / 255f) * radius;
        maxRadius = (.7f + high - threshold / 255f) * radius;
    }

    /// <summary>
    /// Generates the mesh for one chunk. WebGPU compatible (callback-based async readback).
    /// The callback gets the mesh and its vertices, or null when the chunk has no surface.
    /// </summary>
    public void GenerateMesh(int index, int resolution, int chunkRes, Action<Mesh, Vector3[]> onComplete)
    {
        resolution *= 1 << chunkRes;

        int numVoxelsPerAxis = ((resolution << 3) >> chunkRes) - 1;
        int maxTriangleCount = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis * 5;
        int surfaceTriangleCount = Mathf.Min(maxTriangleCount, numVoxelsPerAxis * numVoxelsPerAxis * surfaceTrianglesPerVoxelSlice);

        Dispatch(index, resolution, chunkRes, surfaceTriangleCount, maxTriangleCount, onComplete);
    }

    private void Dispatch(int index, int resolution, int chunkRes, int capacity, int maxTriangleCount, Action<Mesh, Vector3[]> onComplete)
    {
        SetupSharedState();

        ComputeBuffer trianglesBuffer = RentTriangleBuffer(capacity);
        trianglesBuffer.SetCounterValue(0);

        meshGenerator.SetInt("chunkIndex", index);
        meshGenerator.SetInt("chunkResolution", chunkRes);
        meshGenerator.SetInt("resolution", resolution << 3);
        meshGenerator.SetBuffer(meshKernel, "triangles", trianglesBuffer);
        meshGenerator.Dispatch(meshKernel, resolution >> chunkRes, resolution >> chunkRes, resolution >> chunkRes);

        // Read back the triangle count, then only the triangles that were written
        ComputeBuffer counter = counterPool.Count > 0 ? counterPool.Pop() : new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(trianglesBuffer, counter, 0);

        AsyncGPUReadback.Request(counter, counterRequest =>
        {
            ReturnCounter(counter);

            if (disposed)
            {
                trianglesBuffer.Release();
                return;
            }

            if (counterRequest.hasError)
            {
                Debug.LogError("GPU readback error on getting buffer length.");
                ReturnTriangleBuffer(trianglesBuffer);
                onComplete?.Invoke(null, null);
                return;
            }

            int length = counterRequest.GetData<int>()[0];

            if (length >= capacity && capacity < maxTriangleCount)
            {
                // The surface was more complex than expected, run again with room for every possible triangle
                ReturnTriangleBuffer(trianglesBuffer);
                Dispatch(index, resolution, chunkRes, maxTriangleCount, maxTriangleCount, onComplete);
                return;
            }

            length = Mathf.Min(length, capacity);
            if (length == 0)
            {
                ReturnTriangleBuffer(trianglesBuffer);
                onComplete?.Invoke(null, null);
                return;
            }

            AsyncGPUReadback.Request(trianglesBuffer, length * triangleStride, 0, triRequest =>
            {
                ReturnTriangleBuffer(trianglesBuffer);

                if (disposed) return;

                if (triRequest.hasError)
                {
                    Debug.LogError("GPU readback error on triangles.");
                    onComplete?.Invoke(null, null);
                    return;
                }

                // Every triangle has its own three vertices, so the vertex buffer is the readback reinterpreted as Vector3s
                Vector3[] meshVertices = triRequest.GetData<Vector3>().ToArray();
                int vertexCount = meshVertices.Length;

                Mesh mesh = new Mesh();
                mesh.indexFormat = vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.vertices = meshVertices;
                mesh.triangles = SequentialIndices(vertexCount);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                onComplete?.Invoke(mesh, meshVertices);
            });
        });
    }

    /// <summary>
    /// Asynchronously finds the distance from the planet centre to the terrain surface in the given local direction
    /// </summary>
    public void GetSurfaceRadius(Vector3 localDirection, Action<float> onComplete)
    {
        SetupSharedState();

        ComputeBuffer result = heightPool.Count > 0 ? heightPool.Pop() : new ComputeBuffer(1, sizeof(float));

        meshGenerator.SetVector("surfaceDirection", localDirection.normalized);
        meshGenerator.SetBuffer(heightKernel, "surfaceHeight", result);
        meshGenerator.Dispatch(heightKernel, 1, 1, 1);

        AsyncGPUReadback.Request(result, request =>
        {
            float height = request.hasError ? -1 : request.GetData<float>()[0];
            if (disposed) result.Release();
            else heightPool.Push(result);
            if (!disposed && height > 0) onComplete?.Invoke(height);
        });
    }

    // Sets the values that are the same for every dispatch on this planet. The compute shader is shared between planets,
    // so they are set again before each dispatch, but the buffers are only created once.
    private void SetupSharedState()
    {
        if (meshKernel < 0)
        {
            meshKernel = meshGenerator.FindKernel("GenerateMesh");
            heightKernel = meshGenerator.FindKernel("SurfaceHeight");

            layersBuffer = new ComputeBuffer(terrainLayers.Count, sizeof(float) * 10 + sizeof(int));
            layersBuffer.SetData(terrainLayers.ToArray());

            biomesBuffer = new ComputeBuffer(1, sizeof(float) * 8);
            biomesBuffer.SetData(biomeSettings.ToArray());
        }

        meshGenerator.SetFloat("seed", seed);
        meshGenerator.SetFloat("threshold", threshold);
        meshGenerator.SetFloat("radius", radius);
        meshGenerator.SetInt("numTerrainLayers", terrainLayers.Count);
        meshGenerator.SetBuffer(meshKernel, "terrainLayers", layersBuffer);
        meshGenerator.SetBuffer(meshKernel, "biomeSettings", biomesBuffer);
        meshGenerator.SetBuffer(heightKernel, "terrainLayers", layersBuffer);
        meshGenerator.SetBuffer(heightKernel, "biomeSettings", biomesBuffer);
    }

    private ComputeBuffer RentTriangleBuffer(int capacity)
    {
        if (trianglePool.TryGetValue(capacity, out Stack<ComputeBuffer> pool) && pool.Count > 0)
            return pool.Pop();

        return new ComputeBuffer(capacity, triangleStride, ComputeBufferType.Append);
    }

    private void ReturnTriangleBuffer(ComputeBuffer buffer)
    {
        if (disposed)
        {
            buffer.Release();
            return;
        }

        if (!trianglePool.TryGetValue(buffer.count, out Stack<ComputeBuffer> pool))
        {
            pool = new Stack<ComputeBuffer>();
            trianglePool[buffer.count] = pool;
        }
        pool.Push(buffer);
    }

    private void ReturnCounter(ComputeBuffer buffer)
    {
        if (disposed) buffer.Release();
        else counterPool.Push(buffer);
    }

    private static int[] SequentialIndices(int count)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++)
            indices[i] = i;
        return indices;
    }

    /// <summary>
    /// Releases the GPU buffers. Requests still in flight release their own buffers when they finish.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        layersBuffer?.Release();
        biomesBuffer?.Release();
        foreach (Stack<ComputeBuffer> pool in trianglePool.Values)
            foreach (ComputeBuffer buffer in pool)
                buffer.Release();
        trianglePool.Clear();
        foreach (ComputeBuffer buffer in counterPool)
            buffer.Release();
        counterPool.Clear();
        foreach (ComputeBuffer buffer in heightPool)
            buffer.Release();
        heightPool.Clear();
    }
}
