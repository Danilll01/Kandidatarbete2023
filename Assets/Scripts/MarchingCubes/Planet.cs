using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField] ComputeShader meshGenerator;
    [SerializeField] Material waterMaterial;
    [SerializeField, Range(1, 28)] int resolution = 20;
    //[SerializeField, Range(1, 25)] int frequency;
    [SerializeField] GameObject water;
    [SerializeField] public GameObject meshObj;


    float threshold;
    float amplitude;
    float bottomLevel;
    [HideInInspector] public float waterRadius;
    public float radius;
    public float surfaceGravity;
    public string bodyName = "TBT";
    public float mass;
    public List<Planet> moons;

    MarchingCubes marchingCubes;
    [SerializeField] private GenerateCreatures generateCreatures;


    /// <summary>
    /// Initialize mesh for marching cubes
    /// </summary>
    public void Initialize()
    {
        // Get meshfilter and create new mesh if it doesn't exist
        MeshFilter meshFilter = meshObj.GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
        }
        
        
        

        // Initialize the meshgenerator
        if (meshGenerator != null)
        {
            System.Random rand = Universe.random;
            
            threshold = 23 + (float) rand.NextDouble() * 4;
            int frequency = rand.Next(2) + 3;
            amplitude = 1.2f + (float) rand.NextDouble() * 0.4f;
            bottomLevel = 1;
            marchingCubes = new MarchingCubes(meshFilter.sharedMesh, meshGenerator, threshold, resolution, radius, frequency, amplitude, bottomLevel);
        }

        waterRadius = (threshold / 255 - bottomLevel) * radius;

        water.transform.localScale = new Vector3(waterRadius, waterRadius, waterRadius);

        water.GetComponent<Renderer>().material = waterMaterial;

        // Generates the mesh
        if (marchingCubes != null)
        {
            marchingCubes.generateMesh();
            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        // Generate the creatures
        if (generateCreatures != null && bodyName != "Sun" && !bodyName.Contains("Moon"))
        {
            generateCreatures.Initialize(this);
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
