using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField] ComputeShader meshGenerator;
    [SerializeField] Material waterMaterial;
    [SerializeField] Material planetMaterial;
    //[SerializeField, Range(1, 25)] int frequency;
    [SerializeField] GameObject water;


    float threshold;
    float amplitude;
    public float radius;
    public float surfaceGravity;
    public string bodyName = "TBT";
    public float mass;
    public List<Planet> moons;
    readonly int chunkResolution = 3; //This is 2^chunkResolution
    readonly int resolution = 5;


    MeshFilter[] meshFilters;
    MarchingCubes marchingCubes;
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] private SpawnFoliage spawnFoliage;


    /// <summary>
    /// Initialize mesh for marching cubes
    /// </summary>
    public void Initialize()
    {
        if (meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[(1 << chunkResolution) * (1 << chunkResolution) * (1 << chunkResolution)];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                GameObject meshObj = new GameObject("mesh");
                meshObj.transform.parent = transform;


                meshObj.AddComponent<MeshRenderer>().sharedMaterial = planetMaterial;

                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshObj.transform.localPosition = Vector3.zero;
                meshFilters[i].sharedMesh = new Mesh();
            }
        }

        // Initialize the meshgenerator
        if (meshGenerator != null)
        {
            System.Random rand = Universe.random;

            threshold = 23 + (float)rand.NextDouble() * 4;
            int frequency = rand.Next(2) + 3;
            amplitude = 1.2f + (float)rand.NextDouble() * 0.4f;
            marchingCubes = new MarchingCubes(chunkResolution, meshGenerator, threshold, radius, frequency, amplitude);
        }

        float waterRadius = (threshold / 255 - 1) * radius;

        water.transform.localScale = new Vector3(waterRadius, waterRadius, waterRadius);

        water.GetComponent<Renderer>().material = waterMaterial;

        // Generates the mesh
        if (marchingCubes != null)
        {
            for (int i = 0; i < meshFilters.Length; i++)
            {
                marchingCubes.generateMesh(i, resolution, meshFilters[i].sharedMesh);
                MeshCollider meshCollider = meshFilters[i].gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilters[i].sharedMesh;
            }
        }

        // Generate the creatures
        if (generateCreatures != null && bodyName != "Sun" && !bodyName.Contains("Moon"))
        {
            generateCreatures.Initialize(this);
        }

        if (spawnFoliage != null && bodyName != "Sun" && !bodyName.Contains("Moon"))
        {
            spawnFoliage.Initialize(this, waterRadius);
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
