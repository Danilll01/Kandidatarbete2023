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

    [HideInInspector] public Transform player;
    [HideInInspector] public MarchingCubes marchingCubes;

    //[SerializeField, Range(1, 4)] 
    [SerializeField, Range(1, 14)] public int resolution = 5;

    [SerializeField] private bool willGenerateCreature = false;
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] public SpawnFoliage spawnFoliage;
    [SerializeField] public ChunksHandler chunksHandler;

    /// <summary>
    /// Initializes the planet
    /// </summary>
    /// <param name="player">The player</param>
    /// <param name="randomSeed">Seed to be used</param>
    /// <param name="spawn">True if the player will spawn on the planet</param>
    public void Initialize(Transform player, int randomSeed, bool spawn)
    {
        System.Random rand = new System.Random(randomSeed);

        radius = diameter / 2;

        this.player = player;

        MinMaxTerrainLevel terrainLevel = new MinMaxTerrainLevel();

        // Initialize the meshgenerator
        if (marchingCubes == null)
        {
            threshold = 23 + (float)rand.NextDouble() * 4;
            int frequency = rand.Next(2) + 3;
            amplitude = 1.2f + (float)rand.NextDouble() * 0.4f;
            marchingCubes = new MarchingCubes(1, meshGenerator, threshold, diameter, frequency, amplitude);
        }

        // Init water
        waterDiameter = -(threshold / 255 - 1) * diameter;
        water.transform.localScale = new Vector3(waterDiameter, waterDiameter, waterDiameter);
        water.GetComponent<Renderer>().material = waterMaterial;
        terrainLevel.SetMin(Mathf.Abs((waterDiameter + 1) / 2));

        chunksHandler.Initialize(this, terrainLevel, spawn);

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
}
