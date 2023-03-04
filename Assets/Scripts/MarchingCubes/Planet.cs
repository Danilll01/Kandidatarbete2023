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

    [HideInInspector] public Transform player;
    [HideInInspector] public MarchingCubes marchingCubes;

    //[SerializeField, Range(1, 4)] 
    [SerializeField, Range(1, 14)] public int resolution = 5;

    [SerializeField] private bool willGenerateCreature = false;
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] public SpawnFoliage spawnFoliage;
    [SerializeField] public ChunksHandler chunksHandler;
    [SerializeField] public WaterHandler waterHandler;

    [SerializeField] private GameObject billBoard;
    [SerializeField] private List<Material> slidesMaterials;

    private float threshold;

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

        /*
        // presentation seed
        if(Universe.seed == 407022)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = transform;
            cube.transform.localScale = new Vector3(10, 10, 10);
            cube.transform.localPosition = new Vector3(-58.54f, 686.35f, -41.89f);
            cube.transform.Rotate(-1.562f, 39.147f, -1.146f);
        }*/

    }


    private bool spawnedSlides = false;
    private const int presentationSeed = 407022;

    private Vector3[] positions = 
    {
        new Vector3(-62.172f, 683.511f, -40.614f),
        new Vector3(-91.02f, 682.58f, -63.53f),
        new Vector3(-121.03f, 680.19f, -65.34f),
        new Vector3(-153.65f, 676.68f, -66.86f),
        new Vector3(-182.763f, 672.556f, -50.687f),
        new Vector3(-204.982f, 666.163f, -4.122f),
        new Vector3(-231.9233f, 660.034f, 18.583f)
    };

    private Vector3[] rotations =
    {
        new Vector3(2.132f, -54.961f, 5.461f),
        new Vector3(-5.903f, 2.13f, 7.63f),
        new Vector3(-5.862f, 1.739f, 9.434f),
        new Vector3(-8.118f, 11.326f, 12.246f),
        new Vector3(-14.948f, 55.852f, 5.426f),
        new Vector3(-11.193f, 38.911f, 12.826f),
        new Vector3(-2.68f, 11.503f, 19.7f)
    };

    private void Update()
    {
        if(!spawnedSlides && spawnFoliage.foliageSpawned && Universe.seed == presentationSeed)
        {
            //Spawn slides
            for(int i = 0; i < slidesMaterials.Count; i++)
            {
                GameObject billBoard = Instantiate(this.billBoard);
                billBoard.transform.parent = transform;
                billBoard.transform.localPosition = positions[i];
                billBoard.transform.Rotate(rotations[i]);
                billBoard.GetComponent<Renderer>().material = slidesMaterials[i];
            }

            spawnedSlides = true;
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

    public Color GetGroundColor()
    {
        return chunksHandler.terrainColor.bottomColor;
    }
}
