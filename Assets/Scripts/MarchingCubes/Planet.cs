using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField] ComputeShader meshGenerator;
    [SerializeField, Range(0, 255)] float threshold = 200;
    [SerializeField, Range(1, 28)] int resolution = 1;
    [SerializeField] GameObject meshObj;

    public float diameter;
    public float surfaceGravity;
    public string bodyName = "TBT";
    private Transform meshHolder;
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
            marchingCubes = new MarchingCubes(meshFilter.sharedMesh, meshGenerator, threshold, resolution, (diameter / 2));
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
        mass = surfaceGravity * diameter / Universe.gravitationalConstant;
        meshHolder = transform.GetChild(0);
        //meshHolder.localScale = Vector3.one * (diameter/2);
        gameObject.name = bodyName;
    }
}
