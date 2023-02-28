using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(GenerateCreatures))]
[RequireComponent(typeof(TerrainColor))]
[RequireComponent(typeof(SpawnFoliage))]
public class Planet : MonoBehaviour
{
    [SerializeField] private ComputeShader meshGenerator;
    [SerializeField, Range(0, 255)] private float threshold = 200;
    [SerializeField, Range(1, 25)] private int frequency = 20;
    [SerializeField, Range(0, 5)] private float amplitude = 1;
    [SerializeField, Range(0, 1)] private float bottomLevel = 1;
    [SerializeField] private Material waterMaterial;
    [HideInInspector] public float waterDiameter;
    [SerializeField] private GameObject water;

    public float diameter;
    public float radius;
    public float surfaceGravity;
    public string bodyName = "TBT";
    public float mass;
    public List<Planet> moons;
    
    public List<Vector3> waterPoints;

    public List<Chunk> chunks;
    public Transform player;
    private Material planetMaterial;
    private MarchingCubes marchingCubes;

    private Vector3 rotationAxis;

    [SerializeField, Range(1, 4)] private int chunkResolution = 3; //This is 2^chunkResolution
    [SerializeField, Range(1, 14)] private int resolution = 5;
    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] private GameObject chunksParent;


    [SerializeField] private bool willGenerateCreature = false;
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] private TerrainColor terrainColor;
    [SerializeField] private SpawnFoliage spawnFoliage;
    

    /// <summary>
    /// Initialize mesh for marching cubes
    /// </summary>
    public void Initialize(Transform player, int randomSeed)
    {
        System.Random rand = new System.Random(randomSeed);
        UnityEngine.Random.InitState(randomSeed);

        radius = diameter / 2;

        this.player = player;

        MinMaxTerrainLevel terrainLevel = new MinMaxTerrainLevel();

        rotationAxis = RandomPointOnCircleEdge(radius) - Vector3.zero;

        // Create all meshes
        createMeshes(chunkResolution, terrainLevel);

        // Init water
        waterDiameter = -(threshold / 255 - 1) * diameter;
        water.transform.localScale = new Vector3(waterDiameter, waterDiameter, waterDiameter);
        water.GetComponent<Renderer>().material = waterMaterial;

        terrainLevel.SetMin(Mathf.Abs((waterDiameter + 1) / 2));
        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, rand.Next());

        // Sets the material of all chuncks
        foreach (Chunk chunk in chunks) 
        {
            chunk.SetMaterial(planetMaterial);
        }

        if (willGenerateCreature) 
        {
            // Generate the creatures
            if (generateCreatures != null && !bodyName.Contains("Moon")) {
                generateCreatures.Initialize(this, rand.Next());
            }
        }

        if (spawnFoliage != null && !bodyName.Contains("Moon"))
        {
            spawnFoliage.Initialize(this, waterDiameter, rand.Next());
        }
    }

    // Gives back a random position on the edge of a circle given the radius of the circle
    private Vector3 RandomPointOnCircleEdge(float radius)
    {
        var vector2 = UnityEngine.Random.insideUnitCircle.normalized * radius;
        return new Vector3(vector2.x, 0, vector2.y);
    }

    private void createMeshes(int chunkResolution, MinMaxTerrainLevel terrainLevel)
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
            chunk.Initialize(i, resolution, marchingCubes, player, terrainLevel);
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

    public void ShowCreatures(bool show)
    {
        if (generateCreatures != null)
        {
            generateCreatures.ShowCreatures(show);
        }
    }

    void Update()
    {
        if (player.GetComponent<PillPlayerController>().attractor != this)
        {
            RotateAroundAxis();
        }
    }


    private void RotateAroundAxis()
    {
        transform.RotateAround(transform.position, rotationAxis, 10 * Time.deltaTime);
    }
}
