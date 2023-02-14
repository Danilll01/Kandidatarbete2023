using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField] ComputeShader meshGenerator;
    [SerializeField, Range(0, 255)] float threshold = 200;
    [SerializeField, Range(1, 32)] int resolution = 1;
    [SerializeField, Range(1, 500)] float radius = 1;

    MarchingCubes marchingCubes;
    MeshFilter meshFilter;

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
        if (meshFilter == null)
        {
            meshFilter = new MeshFilter();
            GameObject meshObj = new GameObject("mesh");
            meshObj.transform.parent = transform;

            meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            meshFilter = meshObj.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
        }

        if (meshGenerator != null)
        {
            marchingCubes = new MarchingCubes(meshFilter.sharedMesh, meshGenerator, threshold, resolution, radius);
        }
    }
}
