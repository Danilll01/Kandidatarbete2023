using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GenerateCreatures))]
[RequireComponent(typeof(TerrainColor))]
public class Planet : MonoBehaviour
{
    [SerializeField] ComputeShader meshGenerator;
    [SerializeField, Range(0, 255)] float threshold = 200;
    [SerializeField, Range(1, 28)] int resolution = 20;
    [SerializeField, Range(1, 25)] int frequency = 20;
    [SerializeField, Range(0, 5)] float amplitude = 1;
    [SerializeField, Range(0, 1)] float bottomLevel = 1;
    [SerializeField] Material waterMaterial;
    [SerializeField] GameObject water;
    [SerializeField] GameObject meshObj;

    public float radius;
    public float surfaceGravity;
    public string bodyName = "TBT";
    public float mass;
    public List<Planet> moons;

    MarchingCubes marchingCubes;
    [SerializeField] private bool willGenerateCreature = false;
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] private TerrainColor terrainColor;
    [SerializeField] private SpawnFoliage spawnFoliage;

    public void OnValidate() {
        Initialize();
    }

    void Start() {
        if (generateCreatures == null) { 
            generateCreatures = GetComponent<GenerateCreatures>();
        }

        if (terrainColor == null) {
            terrainColor = GetComponent<TerrainColor>();
        }
    }

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

        float waterRadius = (threshold / 255 - bottomLevel) * radius;

        water.transform.localScale = new Vector3(waterRadius, waterRadius, waterRadius);

        water.GetComponent<Renderer>().material = waterMaterial;

        // Generates the mesh
        if (marchingCubes != null) {
            MinMaxTerrainLevel terrainLevel = new MinMaxTerrainLevel();
            terrainLevel.SetMin(Mathf.Abs((waterRadius + 1) / 2));
            marchingCubes.generateMesh(terrainLevel);
            terrainColor.ColorPlanet(terrainLevel);

            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        if (willGenerateCreature) 
        {

            // Generate the creatures
            if (generateCreatures != null && bodyName != "Sun" && !bodyName.Contains("Moon")) {
                generateCreatures.Initialize(this);
            }
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
