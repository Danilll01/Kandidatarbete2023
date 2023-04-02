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

    [SerializeField] private bool willGenerateCreature = false;
    //[SerializeField, Range(1, 4)] 
    [SerializeField, Range(1, 14)] public int resolution = 5;

    public bool willGeneratePlanetLife = false;
    [Range(0f, 1f)]
    [SerializeField] private float chanceToSpawnPlanetLife = 0.8f; 
    [SerializeField] private GenerateCreatures generateCreatures;
    
    [HideInInspector] public Vector3 rotationAxis;
    [HideInInspector] public float rotationSpeed;
    [HideInInspector] public GameObject moonsParent;
    [SerializeField] public FoliageHandler foliageHandler;

    public ChunksHandler chunksHandler;
    public WaterHandler waterHandler;
    public AtmosphereHandler atmosphereHandler;

    [Header("Terrain")]
    [SerializeField, Range(0, 1)] private float waterLevel = 0.92f;
    [SerializeField] private List<TerrainLayer> terrainLayers;
    [SerializeField] private BiomeSettings biomeSettings;

    private float threshold;
    public bool rotateMoons;
    private bool moonsLocked = true;
    private Vector3[] moonsrelativeDistances;
    [HideInInspector] public float positionrelativeToSunDistance;
    private bool setUpSystemRotationComponents;
    private KeplerOrbitMover parentOrbitMover;
    public bool solarSystemRotationActive;

    public Vector3 axisToRotateAround;

    [Header("Orbits")]
    [SerializeField] private string attractorName = "";
    [SerializeField] private float distanceToAttractor = 0;


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

        if (atmosphereHandler != null && bodyName != "Sun")
        {
            // Will generate planet life currently decides if there is atmosphere, could change this later when a better system is created
            // Depending on system, it could be advantageous to give the strength of the atmosphere too, this will have to be sent in as a parameter then 
            atmosphereHandler.Initialize(radius, waterDiameter / 2, willGeneratePlanetLife,rand.Next()); 
            
        }
    }

    // Get the initial distances from the moons to the planet
    public void InitializeMoonValues()
    {
        moonsrelativeDistances = new Vector3[moons.Count];

        for (int i = 0; i < moons.Count; i++)
        {
            moonsrelativeDistances[i] = moons[i].transform.parent.position - transform.position;
        }
        
        parentOrbitMover = transform.parent.GetComponent<KeplerOrbitMover>();
        
    }

    public void Run()
    {
        if (player.parent != transform)
        {
            RotateAroundAxis();
        }
        else if (rotateMoons)
        {
            RotateMoons();
        }

        if (solarSystemRotationActive)
        {
            attractorName = "Sun";
            //LockMoons(false);
            parentOrbitMover.transform.RotateAround(Universe.sunPosition.position, Vector3.up, 5f * Time.deltaTime);
            //parentOrbitMover.transform.up = Universe.sunPosition.up;
            //parentOrbitMover.transform.RotateAround(Universe.sunPosition.GetComponent<KeplerOrbitMover>().AttractorSettings.AttractorObject.position, -axisToRotateAround, rotationSpeed * Time.deltaTime);
            KeepPlanetAtSameDistanceToSun();
            RotateMoons();


            //KeepMoonsAtSameDistanceFromPlanet();

            distanceToAttractor = (parentOrbitMover.transform.position - Universe.sunPosition.position).magnitude;

            //parentOrbitMover.ResetOrbit();
            parentOrbitMover.ForceUpdateOrbitData();
            parentOrbitMover.SetAutoCircleOrbit();
        }
    }

    private void RotateAroundAxis()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void RotateMoons()
    {
        Vector3 sunPosition = parentOrbitMover.AttractorSettings.AttractorObject.transform.position;
        Transform sunTransform = parentOrbitMover.AttractorSettings.AttractorObject.transform;
        moonsParent.transform.position = ClosestPointOnPlane(sunPosition, sunTransform.TransformDirection(Vector3.up), moonsParent.transform.position);
        moonsParent.transform.up = sunTransform.up;

        for (int i = 0; i < moons.Count; i++)
        {
            Transform moon = moons[i].transform;
            moon.transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
            moon.parent.transform.RotateAround(moonsParent.transform.position, Vector3.up, 5f * Time.deltaTime);

            Vector3 direction = moon.parent.transform.position - moonsParent.transform.position;
            moon.parent.transform.position = moonsParent.transform.position + (direction.normalized * moonsrelativeDistances[i].magnitude);

            moon.parent.transform.position = ClosestPointOnPlane(moonsParent.transform.position, moonsParent.transform.TransformDirection(Vector3.up), moon.parent.transform.position);
            moon.parent.transform.up = moonsParent.transform.up;

            moon.parent.GetComponent<KeplerOrbitMover>().ForceUpdateOrbitData();
            moon.parent.GetComponent<KeplerOrbitMover>().SetAutoCircleOrbit();
        }
    }

    // Reset the moons rotation
    public void ResetMoons()
    {
        rotateMoons = false;
        moonsParent.transform.rotation = Quaternion.identity;
        for (int i = 0; i < moons.Count; i++)
        {
            Transform moon = moons[i].transform;
            KeplerOrbitMover orbitMover = moon.parent.GetComponent<KeplerOrbitMover>();
            moon.localPosition = new Vector3(moon.localPosition.x, 0, moon.localPosition.z);
            orbitMover.ResetOrbit();
            orbitMover.ForceUpdateOrbitData();
            orbitMover.SetAutoCircleOrbit();
        }

        setUpSystemRotationComponents = false;
    }
    
    // Lock or unlock the moons orbits
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

    // Set up the components for solar system orbit
    public void HandleSolarSystemOrbit(Vector3 rotationaxis)
    {
        if (bodyName.Contains("Planet") && player.parent != transform)
        {
            SetUpComponents(rotationAxis);
            solarSystemRotationActive = true;
        }
    }

    private void KeepPlanetAtSameDistanceToSun()
    {
        Vector3 sunPosition = Universe.sunPosition.position;
        Transform sunTransform = parentOrbitMover.AttractorSettings.AttractorObject.transform;

        Vector3 direction = parentOrbitMover.transform.position - sunPosition;
        parentOrbitMover.transform.position = sunPosition + (direction.normalized * positionrelativeToSunDistance);

        parentOrbitMover.transform.position = ClosestPointOnPlane(sunPosition, sunTransform.TransformDirection(Vector3.up), parentOrbitMover.transform.position);

        /*
        Vector3d orbitNormal3D = parentOrbitMover.AttractorSettings.AttractorObject.GetComponent<KeplerOrbitMover>().OrbitData.OrbitNormal;
        Vector3 orbitNormal = new Vector3((float)orbitNormal3D.x, (float)orbitNormal3D.y, (float)orbitNormal3D.z);
        parentOrbitMover.transform.position = ClosestPointOnPlane(Vector3.zero, orbitNormal, parentOrbitMover.transform.position);
        parentOrbitMover.transform.up = orbitNormal;
        */


    }

    private Vector3 ClosestPointOnPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return point + DistanceFromPlane(planeOffset, planeNormal, point) * planeNormal;
    }

    private float DistanceFromPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return Vector3.Dot(planeOffset - point, planeNormal);
    }

    private void KeepMoonsAtSameDistanceFromPlanet()
    {
        if (!rotateMoons)
        {
            for (int i = 0; i < moons.Count; i++)
            {
                Transform moon = moons[i].transform;
                Vector3 direction = moon.parent.transform.position - moonsParent.transform.position;

                Vector3 sunPosition = parentOrbitMover.AttractorSettings.AttractorObject.transform.position;
                Transform sunTransform = parentOrbitMover.AttractorSettings.AttractorObject.transform;
                moon.parent.transform.position = moonsParent.transform.position + (direction.normalized * moonsrelativeDistances[i].magnitude);


                Vector3d orbitNormal3D = parentOrbitMover.AttractorSettings.AttractorObject.GetComponent<KeplerOrbitMover>().OrbitData.OrbitNormal;
                Vector3 orbitNormal = new Vector3((float)orbitNormal3D.x, (float)orbitNormal3D.y, (float)orbitNormal3D.z);
                moon.parent.transform.position = ClosestPointOnPlane(Vector3.zero, orbitNormal, moon.parent.transform.position);
                moon.parent.transform.up = orbitNormal;

            }
        }
    }

    private void SetUpComponents(Vector3 rotationAxis)
    {
        if (setUpSystemRotationComponents) return;
        axisToRotateAround = rotationAxis;
        parentOrbitMover.LockOrbitEditing = false;
        parentOrbitMover.ResetOrbit();
        //parentOrbitMover.SetUp();
        parentOrbitMover.ForceUpdateOrbitData();
        parentOrbitMover.SetAutoCircleOrbit();
        setUpSystemRotationComponents = true;
    }
}
