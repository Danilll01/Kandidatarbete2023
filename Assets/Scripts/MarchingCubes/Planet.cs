using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(GenerateCreatures))]
[RequireComponent(typeof(TerrainColor))]
[RequireComponent(typeof(SpawnFoliage))]
[RequireComponent(typeof(SpawnFoliage))]
public class Planet : MonoBehaviour
{
    [SerializeField] private ComputeShader meshGenerator;
    [SerializeField] private Material waterMaterial;
    [HideInInspector] public float waterDiameter;

    [HideInInspector, Obsolete]public float diameter;
    [HideInInspector] public float radius;
    [HideInInspector] public float surfaceGravity;
    [HideInInspector] public string bodyName = "TBT";
    [HideInInspector] public float mass;
    [HideInInspector] public List<Planet> moons;
    
    [HideInInspector] public List<Vector3> waterPoints;

    public List<Chunk> chunks;
    private Material planetMaterial;

    public Vector3 rotationAxis;

    [SerializeField, Range(1, 4)] private int chunkResolution = 3; //This is 2^chunkResolution
    [SerializeField, Range(1, 14)] public int resolution = 5;
    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] private GameObject chunksParent;
    [HideInInspector] public Transform player;
    [HideInInspector] public MarchingCubes marchingCubes;


    [SerializeField] private bool willGenerateCreature = false;
    [SerializeField] private GenerateCreatures generateCreatures;

    private System.Random rand;

    [SerializeField] public SpawnFoliage spawnFoliage;
    [SerializeField] public ChunksHandler chunksHandler;
    [SerializeField] public WaterHandler waterHandler;

    private float threshold;

    /// <summary>
    /// Initializes the planet
    /// </summary>
    /// <param name="player">The player</param>
    /// <param name="randomSeed">Seed to be used</param>
    /// <param name="spawn">True if the player will spawn on the planet</param>
    public void Initialize(Transform player, int randomSeed, bool spawn)
    {
        rand = new System.Random(randomSeed);

        radius = diameter / 2;

        this.player = player;

        MinMaxTerrainLevel terrainLevel = new MinMaxTerrainLevel();

        rotationAxis = RandomPointOnSphereEdge(radius) - Vector3.zero;
        
        // Initialize the meshgenerator
        if (marchingCubes == null)
        {
            threshold = 23 + (float)rand.NextDouble() * 4;
            int frequency = rand.Next(2) + 3;
            float amplitude = 1.2f + (float)rand.NextDouble() * 0.4f;
            marchingCubes = new MarchingCubes(1, meshGenerator, threshold, diameter, frequency, amplitude);
        }

        // Init water
        waterDiameter = -(threshold / 255 - 1) * diameter;

        terrainLevel.SetMin(Mathf.Abs((waterDiameter + 1) / 2));

        chunksHandler.Initialize(this, terrainLevel, spawn, rand.Next());

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

        if (waterHandler != null && bodyName != "Sun")
        {
            waterHandler.Initialize(this, waterDiameter, GetGroundColor());
        }
    }

    // Gives back a random position on the edge of a circle given the radius of the circle
    private Vector3 RandomPointOnSphereEdge(float radius)
    {
        Vector3 randomVector = new Vector3(rand.Next(1, 360), rand.Next(1, 360), rand.Next(1, 360));
        var vector3 = randomVector.normalized * radius;
        return new Vector3(vector3.x, vector3.y, vector3.z);
    }


    /// <summary>
    /// Set up the values for the planets
    /// </summary>
    public void SetUpPlanetValues()
    {
        mass = surfaceGravity * diameter * diameter / Universe.gravitationalConstant;
        gameObject.name = bodyName;
    }

    public Color GetGroundColor()
    {
        return chunksHandler.terrainColor.bottomColor;
    }

    void Update()
    {
        if (player.parent == null)
        {
            RotateAroundAxis();
        }
        else if (player.parent.GetComponent<Planet>() != this)
        {
            RotateAroundAxis();
        }
    }


    private void RotateAroundAxis()
    {
        transform.RotateAround(transform.position, rotationAxis, Time.deltaTime);
    }
}
