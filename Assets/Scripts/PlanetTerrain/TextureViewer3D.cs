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
    [SerializeField] private ComputeShader shader;

    private RenderTexture renderTexture;
    

    Material material;
    void Start() {

        material = GetComponentInChildren<MeshRenderer>().material;
        int size = numChunks * (numPointsPerAxis - 1) + 1;
        Create3DTexture(ref renderTexture, size, "VisulizeArray");
        
    }

    public void Display() { }


    void Update() {
        material.SetFloat("sliceDepth", sliceDepth);
        //material.SetTexture("DisplayTexture", FindObjectOfType<GenTest>().rawDensityTexture);
        material.SetTexture("DisplayTexture", renderTextureSimple);

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