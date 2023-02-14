using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField] ComputeShader meshGenerator;
    [SerializeField, Range(0, 255)] float threshold = 200;
    [SerializeField, Range(1, 28)] int resolution = 1;
    [SerializeField, Range(1, 500)] float radius = 1;
    [SerializeField] GameObject meshObj;

    MarchingCubes marchingCubes;

    private void OnValidate()
    {
        Initialize();
        if (marchingCubes != null)
        {
            marchingCubes.generateMesh();
        }
    }

    void Initialize()
    {
        MeshFilter meshFilter = meshObj.GetComponent<MeshFilter>();
        if(meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
        }

        if (meshGenerator != null)
        {
            marchingCubes = new MarchingCubes(meshFilter.sharedMesh, meshGenerator, threshold, resolution, radius);
        }
    }
}
