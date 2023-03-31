using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using ExtendedRandom;
using SimpleKeplerOrbits;
using UnityEngine;

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
    
    [HideInInspector] public Vector3 rotationAxis;
    [HideInInspector] public float rotationSpeed;
    [HideInInspector] public GameObject moonsParent;

    [SerializeField] private List<TerrainLayer> terrainLayers;

    private float threshold;
    public FoliageHandler foliageHandler;
    public bool rotateMoons;
    private bool moonsLocked = true;
    private Vector3[] moonsrelativeDistances;
    [HideInInspector] public float positionrelativeToSunDistance;
    private bool setUpSystemRotationComponents;
    private KeplerOrbitMover parentOrbitMover;
    public bool solarSystemRotationActive;

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
        
        rotationAxis = rand.OnUnitSphere() * radius;
        rotationSpeed = rand.Next(1,5);

        willGeneratePlanetLife = rand.Value() < chanceToSpawnPlanetLife;

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
            foliageHandler.Initialize(this);
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

    public void InitializeMoonValues()
    {
        moonsrelativeDistances = new Vector3[moons.Count];

        for (int i = 0; i < moons.Count; i++)
        {
            moonsrelativeDistances[i] = moons[i].transform.parent.position - transform.position;
        }
        
        parentOrbitMover = transform.parent.GetComponent<KeplerOrbitMover>();
        
    }

    private void LateUpdate()
    {
        if (player.parent != transform)
        {
            RotateAroundAxis();
        }
        else if (rotateMoons && !bodyName.Contains("Moon"))
        {
            RotateMoons();
        }

        if (solarSystemRotationActive)
        {
            LockMoons(false);
            KeepPlanetAtSameDistanceToSun();
            KeepMoonsAtSameDistanceFromPlanet();
        }
    }

    private void RotateAroundAxis()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void RotateMoons()
    {
        LockMoons(false);
        moonsParent.transform.Rotate(-rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
        
        for (int i = 0; i < moons.Count; i++)
        {
            Transform moon = moons[i].transform;
            Vector3 direction = moon.parent.transform.position - moonsParent.transform.position;
            moon.parent.transform.position = direction.normalized * moonsrelativeDistances[i].magnitude;
        }
    }

    public void ResetMoons()
    {
        rotateMoons = false;
        moonsParent.transform.rotation = Quaternion.identity;
        for (int i = 0; i < moons.Count; i++)
        {
            Transform moon = moons[i].transform;
            KeplerOrbitMover orbitMover = moon.parent.GetComponent<KeplerOrbitMover>();
            orbitMover.ForceUpdateOrbitData();
            orbitMover.SetAutoCircleOrbit();
        }
    }
    
    
    public void LockMoons(bool lockMoons)
    {
        if (moonsLocked != lockMoons)
        {
            foreach (Planet moon in moons)
            {
                moon.transform.parent.GetComponent<KeplerOrbitMover>().LockOrbitEditing = lockMoons;
                if (lockMoons)
                {
                    moon.transform.parent.GetComponent<KeplerOrbitMover>().SetAutoCircleOrbit();
                }
            }
            moonsLocked = lockMoons;
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

    public void HandleSolarSystemOrbit()
    {
        if (bodyName.Contains("Planet") && player.parent != transform)
        {
            SetUpComponents();
            solarSystemRotationActive = true;
        }
    }

    private void KeepPlanetAtSameDistanceToSun()
    {
        Vector3 sunPosition = parentOrbitMover.AttractorSettings.AttractorObject.transform.position;
        Vector3 direction = parentOrbitMover.transform.position - sunPosition;
        parentOrbitMover.transform.position = sunPosition + (direction.normalized * positionrelativeToSunDistance);
    }

    private void KeepMoonsAtSameDistanceFromPlanet()
    {
        if (!rotateMoons)
        {
            for (int i = 0; i < moons.Count; i++)
            {
                Transform moon = moons[i].transform;
                Vector3 direction = moon.parent.transform.position - moonsParent.transform.position;
                moon.parent.transform.position = moonsParent.transform.position + (direction.normalized * moonsrelativeDistances[i].magnitude);
            }
        }
    }

    private void SetUpComponents()
    {
        if (setUpSystemRotationComponents) return;
        parentOrbitMover.LockOrbitEditing = false;
        setUpSystemRotationComponents = true;
    }
}
