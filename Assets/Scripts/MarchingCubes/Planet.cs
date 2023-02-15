using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField] ComputeShader meshGenerator;
    [SerializeField, Range(0, 255)] float threshold = 200;
    [SerializeField, Range(1, 28)] int resolution = 1;
    [SerializeField] GameObject meshObj;

    public float radius;
    public float surfaceGravity;
    public string bodyName = "TBT";
    public float mass;

    MarchingCubes marchingCubes;


    public void Initialize()
    {
        // Get meshfilter and create new mesh if it doesn't exist
        MeshFilter meshFilter = meshObj.GetComponent<MeshFilter>();
        if(meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
        }

        // Initialize the meshgenerator
        if (meshGenerator != null)
        {
            marchingCubes = new MarchingCubes(meshFilter.sharedMesh, meshGenerator, threshold, resolution, radius);
        }

        if (marchingCubes != null)
        {
            marchingCubes.generateMesh();
        }
    }

    /// <summary>
    /// Set up the values for the planets
    /// </summary>
    public void SetUpPlanetValues()
    {
        mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        gameObject.name = bodyName;
    }
}
