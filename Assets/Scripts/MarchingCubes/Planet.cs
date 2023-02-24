using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(GenerateCreatures))]
[RequireComponent(typeof(TerrainColor))]
public class Planet : MonoBehaviour
{
    [SerializeField] private ComputeShader meshGenerator;
    [SerializeField, Range(0, 255)] private float threshold = 200;
    [SerializeField, Range(1, 28)] private int resolution = 20;
    [SerializeField, Range(1, 25)] private int frequency = 20;
    [SerializeField, Range(0, 5)] private float amplitude = 1;
    [SerializeField, Range(0, 1)] private float bottomLevel = 1;
    [SerializeField] private Material waterMaterial;
    [SerializeField] private GameObject water;
    [SerializeField] private GameObject meshObj;

    public float radius;
    public float surfaceGravity;
    public string bodyName = "TBT";
    public float mass;
    public List<Planet> moons;

    private MarchingCubes marchingCubes;
    [SerializeField] private bool willGenerateCreature = false;
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] private TerrainColor terrainColor;
    [SerializeField] private SpawnFoliage spawnFoliage;

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
    public void Initialize(int randomSeed)
    {
        System.Random rand = new System.Random(randomSeed);

        // Get meshfilter and create new mesh if it doesn't exist
        MeshFilter meshFilter = meshObj.GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
        }

        // Initialize the meshgenerator
        if (meshGenerator != null)
        {
            threshold = 23 + (float)rand.NextDouble() * 4;
            int frequency = rand.Next(2) + 3;
            amplitude = 1.2f + (float)rand.NextDouble() * 0.4f;
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
            

            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            terrainColor.ColorPlanet(terrainLevel, rand.Next());
        }

        if (willGenerateCreature) 
        {

            // Generate the creatures
            if (generateCreatures != null && bodyName != "Sun" && !bodyName.Contains("Moon")) {
                generateCreatures.Initialize(this, rand.Next());
            }
        }

        if (spawnFoliage != null && bodyName != "Sun" && !bodyName.Contains("Moon"))
        {
            spawnFoliage.Initialize(this, waterRadius, rand.Next());
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
