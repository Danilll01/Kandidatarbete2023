using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class TextureViewer3D : MonoBehaviour {

    [Range(0, 1)]
    [SerializeField] private float sliceDepth;

    [SerializeField] private RenderTexture renderTextureSimple;
    [SerializeField] private int numChunks = 4;
    [SerializeField] private int numPointsPerAxis = 10;
    public float boundsSize = 10;
    public float noiseScale = 1f;
    public float noiseHeightMultiplier = 1f;

    [SerializeField] private ComputeShader shader;

    private RenderTexture renderTexture;
    

    Material material;
    void Start() {

        material = GetComponentInChildren<MeshRenderer>().material;
        int size = numChunks * (numPointsPerAxis - 1) + 1;
        Create3DTexture(ref renderTexture, size, "VisulizeArray");

        // Set textures on compute shaders
        shader.SetTexture(0, "DensityTexture", renderTexture);

    }

    public void Display() { }


    void Update() {
        material.SetFloat("sliceDepth", sliceDepth);
        //material.SetTexture("DisplayTexture", FindObjectOfType<GenTest>().rawDensityTexture);
        material.SetTexture("DisplayTexture", renderTexture);

    }

    void ComputeDensity() {
        // Get points (each point is a vector4: xyz = position, w = density)
        int textureSize = renderTexture.width;

        shader.SetInt("textureSize", textureSize);

        shader.SetFloat("planetSize", boundsSize);
        shader.SetFloat("noiseHeightMultiplier", noiseHeightMultiplier);
        shader.SetFloat("noiseScale", noiseScale);

        Dispatch(shader, textureSize, textureSize, textureSize);

    }


    private void Create3DTexture(ref RenderTexture texture, int size, string name) {
        
        var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat;
        if (texture == null || !texture.IsCreated() || texture.width != size || texture.height != size || texture.volumeDepth != size || texture.graphicsFormat != format) {
            //Debug.Log ("Create tex: update noise: " + updateNoise);
            if (texture != null) {
                texture.Release();
            }
            const int numBitsInDepthBuffer = 0;
            texture = new RenderTexture(size, size, numBitsInDepthBuffer);
            texture.graphicsFormat = format;
            texture.volumeDepth = size;
            texture.enableRandomWrite = true;
            texture.dimension = TextureDimension.Tex3D;


            texture.Create();
        }
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.name = name;
    }


    // Creates and sets up the buffers
    private void CreateAndSetBuffer<T>(ref ComputeBuffer buffer, T[] data, ComputeShader cs, string nameID, int kernelIndex = 0) {
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        CreateStructuredBuffer<T>(ref buffer, data.Length);
        buffer.SetData(data);
        cs.SetBuffer(kernelIndex, nameID, buffer);
    }

    private void CreateStructuredBuffer<T>(ref ComputeBuffer buffer, int count) {
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        bool createNewBuffer = buffer == null || !buffer.IsValid() || buffer.count != count || buffer.stride != stride;
        if (createNewBuffer) {
            Release(buffer);
            buffer = new ComputeBuffer(count, stride);
        }
    }

    /// Releases supplied buffer/s if not null
	public static void Release(params ComputeBuffer[] buffers) {
        for (int i = 0; i < buffers.Length; i++) {
            if (buffers[i] != null) {
                buffers[i].Release();
            }
        }
    }



    // Dispatches buffer

    /// Convenience method for dispatching a compute shader.
	/// It calculates the number of thread groups based on the number of iterations needed.
	private void Dispatch(ComputeShader cs, int numIterationsX, int numIterationsY = 1, int numIterationsZ = 1, int kernelIndex = 0) {
        Vector3Int threadGroupSizes = GetThreadGroupSizes(cs, kernelIndex);
        int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
        int numGroupsY = Mathf.CeilToInt(numIterationsY / (float)threadGroupSizes.y);
        int numGroupsZ = Mathf.CeilToInt(numIterationsZ / (float)threadGroupSizes.y);
        cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
    }

    private Vector3Int GetThreadGroupSizes(ComputeShader compute, int kernelIndex = 0) {
        uint x, y, z;
        compute.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
        return new Vector3Int((int)x, (int)y, (int)z);
    }
}