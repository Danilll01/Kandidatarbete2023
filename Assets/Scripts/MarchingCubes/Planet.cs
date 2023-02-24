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

    public float diameter;
    public float radius;
    public float surfaceGravity;
    public string bodyName = "TBT";
    public float mass;
    public List<Planet> moons;

    [SerializeField, Range(1, 4)] int chunkResolution = 3; //This is 2^chunkResolution
    [SerializeField, Range(1, 14)] int resolution = 5;
    List<Chunk> chunks;
    [SerializeField] Chunk chunkPrefab;
    [SerializeField] GameObject chunksParent;

    private MarchingCubes marchingCubes;
    [SerializeField] private bool willGenerateCreature = false;
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] private TerrainColor terrainColor;
    [SerializeField] private SpawnFoliage spawnFoliage;
    PillPlayerController player;

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
    public void Initialize(PillPlayerController player, int randomSeed)
    {
        System.Random rand = new System.Random(randomSeed);

        radius = diameter / 2;

        this.player = player;

        // Create all meshes
        createMeshes(chunkResolution);

        // Init water
        float waterDiameter = -(threshold / 255 - 1) * diameter;
        water.transform.localScale = new Vector3(waterDiameter, waterDiameter, waterDiameter);
        water.GetComponent<Renderer>().material = waterMaterial;


        MinMaxTerrainLevel terrainLevel = new MinMaxTerrainLevel();
        terrainLevel.SetMin(Mathf.Abs((waterRadius + 1) / 2));
        terrainColor.ColorPlanet(terrainLevel, rand.Next());
        marchingCubes.generateMesh(terrainLevel);

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

    private void createMeshes(int chunkResolution)
    {
        Destroy(chunksParent);

        chunksParent = new GameObject();
        chunksParent.name = "chunks";
        chunksParent.transform.parent = transform;
        chunksParent.transform.localPosition = Vector3.zero;

        // Initialize the meshgenerator
        if (marchingCubes == null)
        {
            System.Random rand = Universe.random;

            threshold = 23 + (float)rand.NextDouble() * 4;
            int frequency = rand.Next(2) + 3;
            amplitude = 1.2f + (float)rand.NextDouble() * 0.4f;
            marchingCubes = new MarchingCubes(chunkResolution, meshGenerator, threshold, diameter, frequency, amplitude);
        }

        marchingCubes.chunkResolution = chunkResolution;

        // Create all chunks
        chunks = new List<Chunk>();
        int noChunks = (1 << chunkResolution) * (1 << chunkResolution) * (1 << chunkResolution);
        for (int i = 0; i < noChunks; i++)
        {
            Chunk chunk = Instantiate(chunkPrefab);
            chunk.transform.parent = chunksParent.transform;
            chunk.transform.localPosition = Vector3.zero;
            chunk.name = "chunk" + i;
            chunk.Initialize(i, resolution, marchingCubes, player); 
            chunks.Add(chunk);
        }
    }

    /// <summary>
    /// Set up the values for the planets
    /// </summary>
    public void SetUpPlanetValues()
    {
        mass = surfaceGravity * diameter * diameter / Universe.gravitationalConstant;
        gameObject.name = bodyName;
    }
}
