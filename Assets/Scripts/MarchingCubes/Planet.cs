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
    public float diameter;
    public float surfaceGravity;
    public string bodyName = "TBT";
    public float mass;
    public List<Planet> moons;

    [SerializeField, Range(1, 4)] int chunkResolution = 3; //This is 2^chunkResolution
    [SerializeField, Range(1, 14)] int resolution = 5;
    List<Chunk> chunks;
    MarchingCubes marchingCubes;
    PillPlayerController player;

    [SerializeField] Chunk chunkPrefab;
    [SerializeField] GameObject chunksParent;
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] private SpawnFoliage spawnFoliage;


    /// <summary>
    /// Initialize mesh for marching cubes
    /// </summary>
    public void Initialize(PillPlayerController player)
    {
        this.player = player;
        createMeshes(chunkResolution);

        float waterDiameter = -(threshold / 255 - 1) * diameter;

        water.transform.localScale = new Vector3(waterDiameter, waterDiameter, waterDiameter);

        water.GetComponent<Renderer>().material = waterMaterial;

        // Generate the creatures
        if (generateCreatures != null && bodyName != "Sun" && !bodyName.Contains("Moon"))
        {
            generateCreatures.Initialize(this);
        }

        if (spawnFoliage != null && bodyName != "Sun" && !bodyName.Contains("Moon"))
        {
            spawnFoliage.Initialize(this, waterDiameter);
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

        chunks = new List<Chunk>();
        int noChunks = (1 << chunkResolution) * (1 << chunkResolution) * (1 << chunkResolution);

        // Create all chunks
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
