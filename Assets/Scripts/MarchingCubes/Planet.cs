using System;
using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;

[RequireComponent(typeof(TerrainColor))]
public class Planet : MonoBehaviour
{
    [SerializeField] private ComputeShader meshGenerator;
    [SerializeField] private Material waterMaterial; // Can this be removed?
    [HideInInspector] public float waterDiameter;

    [HideInInspector, Obsolete]public float diameter;
    [HideInInspector] public float radius;
    [HideInInspector] public float surfaceGravity;
    [HideInInspector] public string bodyName = "TBT";
    [HideInInspector] public float mass;
    [HideInInspector] public List<Planet> moons;
    
    [HideInInspector] public List<Vector3> waterPoints;

    [HideInInspector] public Transform player;
    [HideInInspector] public MarchingCubes marchingCubes;

    [SerializeField, Range(1, 14)] public int resolution = 5;

    public bool willGeneratePlanetLife = false;
    [SerializeField, Range(0f, 1f)] private float chanceToSpawnPlanetLife = 0.8f;
    public ChunksHandler chunksHandler;
    public WaterHandler waterHandler;
    public AtmosphereHandler atmosphereHandler;
    public FoliageHandler foliageHandler;
    public CreatureHandler creatureHandler;

    [Header("Terrain")]
    [SerializeField, Range(0, 1)] private float waterLevel = 0.92f;
    [SerializeField] private List<TerrainLayer> terrainLayers;
    [SerializeField] private BiomeSettings biomeSettings;

    private float threshold;
   
    /// <summary>
    /// Initializes the planet
    /// </summary>
    /// <param name="player">The player</param>
    /// <param name="randomSeed">Seed to be used</param>
    /// <param name="spawn">True if the player will spawn on the planet</param>
    public void Initialize(Transform player, int randomSeed, bool spawn)
    {
        RandomX rand = new RandomX(randomSeed);

        this.player = player;

        MinMaxTerrainLevel terrainLevel = new MinMaxTerrainLevel();

        willGeneratePlanetLife = rand.Value() < chanceToSpawnPlanetLife;

        // Initialize the meshgenerator
        if (marchingCubes == null)
        {
            threshold = 23 + (float) rand.Value() * 4;
            marchingCubes = new MarchingCubes(rand.Value() * 123.123f, 1, meshGenerator, threshold, radius, terrainLayers, biomeSettings);
        }

        // Init water
        if (willGeneratePlanetLife)
        {
            waterDiameter = Mathf.Abs((threshold / 255 - 1) * 2 * radius * waterLevel);
        }
        else
        {
            waterDiameter = 0; 
        }

        if (!bodyName.Contains("Moon"))
        {
            if (foliageHandler != null)
            {
                foliageHandler.Initialize(this);
            }

            if (creatureHandler != null)
            {
                creatureHandler.Initialize(this);
            }
        }

        terrainLevel.SetMin(Mathf.Abs((waterDiameter + 1) / 2));

        chunksHandler.Initialize(this, terrainLevel, spawn, rand.Next());

        if (willGeneratePlanetLife) 
        {
            if (waterHandler != null && bodyName != "Sun")
            {
                waterHandler.Initialize(this, waterDiameter, GetGroundColor());
            }
        }

        if (atmosphereHandler != null && bodyName != "Sun")
        {
            // Will generate planet life currently decides if there is atmosphere, could change this later when a better system is created
            // Depending on system, it could be advantageous to give the strength of the atmosphere too, this will have to be sent in as a parameter then 
            atmosphereHandler.Initialize(radius, waterDiameter / 2, willGeneratePlanetLife,rand.Next()); 
            
        }
    }

    /// <summary>
    /// Set up the values for the planets
    /// </summary>
    public void SetUpPlanetValues()
    {
        mass = surfaceGravity * 4 * radius * radius / Universe.gravitationalConstant;
        gameObject.name = bodyName;
    }

    public Color GetGroundColor()
    {
        return chunksHandler.terrainColor.bottomColor;
    }
}
