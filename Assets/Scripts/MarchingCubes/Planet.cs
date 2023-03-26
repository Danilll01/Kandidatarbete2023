using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using ExtendedRandom;
using SimpleKeplerOrbits;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(GenerateCreatures))]
[RequireComponent(typeof(TerrainColor))]
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

    [SerializeField] private bool willGenerateCreature = false;
    //[SerializeField, Range(1, 4)] 
    [SerializeField, Range(1, 14)] public int resolution = 5;

    [SerializeField] public bool willGeneratePlanetLife = false;
    [Range(0f, 1f)]
    [SerializeField] private float chanceToSpawnPlanetLife = 0.8f; 
    [SerializeField] private GenerateCreatures generateCreatures;
    [SerializeField] public ChunksHandler chunksHandler;
    [SerializeField] public WaterHandler waterHandler;

    [SerializeField] private List<TerrainLayer> terrainLayers;

    private float threshold;
    public FoliageHandler foliageHandler;
    
    public Vector3 rotationAxis;
    [HideInInspector] public GameObject moonsRotationObject;
    private Vector3[] moonsrelativeDistances;
    private bool moonsLocked = true;
    public bool testingRotation = false;

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

        rotationAxis = rand.OnUnitSphere() * radius - Vector3.zero;

        willGeneratePlanetLife = rand.Value() < chanceToSpawnPlanetLife;
        willGenerateCreature = false;
        willGeneratePlanetLife = false;

        // Initialize the meshgenerator
        if (marchingCubes == null)
        {
            threshold = 23 + (float) rand.Value() * 4;
            marchingCubes = new MarchingCubes(1, meshGenerator, threshold, radius, terrainLayers);
        }

        // Init water
        if (willGeneratePlanetLife)
        {
            waterDiameter = Mathf.Abs((threshold / 255 - 1) * 2 * radius * 0.93f);
        }
        else
        {
            waterDiameter = 0; 
        }

        if (foliageHandler != null && !bodyName.Contains("Moon"))
        {
            //foliageHandler.Initialize(this);
        }

        terrainLevel.SetMin(Mathf.Abs((waterDiameter + 1) / 2));

        chunksHandler.Initialize(this, terrainLevel, spawn, rand.Next());

        

        if (willGeneratePlanetLife) 
        {
            // Generate the creatures
            if (generateCreatures != null && !bodyName.Contains("Moon")) {
                generateCreatures.Initialize(this, rand.Next(), spawn);
            }

            if (waterHandler != null && bodyName != "Sun")
            {
                waterHandler.Initialize(this, waterDiameter, GetGroundColor());
            }
        }

    }
    
    public void InitializeMoonsValues()
    {
        moonsrelativeDistances = new Vector3[moons.Count];

        for (int i = 0; i < moons.Count; i++)
        {
            moonsrelativeDistances[i] = moons[i].transform.position - this.transform.position;
        }
    }


    // Gives back a random position on the edge of a circle given the radius of the circle
    private Vector3 RandomPointOnSphereEdge(float radius, RandomX rand)
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
        mass = surfaceGravity * 4 * radius * radius / Universe.gravitationalConstant;
        gameObject.name = bodyName;
    }

    public Color GetGroundColor()
    {
        return chunksHandler.terrainColor.bottomColor;
    }
    
    void Update()
    {
        if (!testingRotation)
        {
            RotateAroundAxis();
        }
        else
        {
            if (player.parent == null)
            {
                RotateAroundAxis();
            }
            else if (player.parent != transform)
            {
                RotateAroundAxis();
            }
            else if(!bodyName.Contains("Moon"))
            {
                RotateMoons();
            }
        }
    }


    private void RotateAroundAxis()
    {
        transform.RotateAround(transform.position, rotationAxis, 5f * Time.deltaTime);
    }

    private void RotateMoons()
    {
        LockMoons(false);

        moonsRotationObject.transform.RotateAround(transform.position, -rotationAxis, 5f * Time.deltaTime);

        for (int i = 0; i < moons.Count; i++)
        {
            Planet moon = moons[i];
            Vector3 direction = moon.transform.position - transform.position;
            moon.gameObject.GetComponent<KeplerOrbitMover>().SetAutoCircleOrbit();
            moon.transform.position = direction.normalized * moonsrelativeDistances[i].magnitude;
        } 

    }

    public void ResetMoons()
    {
        LockMoons(true);

        // Commented for now, but might be useful when trying to fix landing on moons
        //LockMoons(false);
        /*
        for (int i = 0; i < moons.Count; i++)
        {
            Planet moon = moons[i];
            moon.GetComponent<KeplerOrbitMover>().enabled = false;
        }
        moonsParent.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        for (int i = 0; i < moons.Count; i++)
        {
            Planet moon = moons[i];
            Vector3 direction = moon.transform.position - moonsParent.transform.position;
            direction.y = 0;
            moon.transform.position = direction.normalized * moonsrelativeDistances[i].magnitude;
            moon.gameObject.GetComponent<KeplerOrbitMover>().VelocityHandle.localPosition = new Vector3(100, 0, 0);
        }
        */

        //ReactivateMoonOrbits();

    }

    private void ReactivateMoonOrbits()
    {
        for (int i = 0; i < moons.Count; i++)
        {
            Planet moon = moons[i];

            KeplerOrbitMover orbitMover = moon.GetComponent<KeplerOrbitMover>();
            orbitMover.SetUp();
            orbitMover.SetAutoCircleOrbit();
            orbitMover.ForceUpdateOrbitData();
            orbitMover.enabled = true;
        }
    }

    private void LockMoons(bool lockMoons)
    {
        if (moonsLocked != lockMoons)
        {
            foreach (Planet moon in moons)
            {
                moon.gameObject.GetComponent<KeplerOrbitMover>().LockOrbitEditing = lockMoons;
            }
            moonsLocked = lockMoons;
        }
    }
}
