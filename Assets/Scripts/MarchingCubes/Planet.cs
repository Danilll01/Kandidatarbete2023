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
    public float orbitSpeed;
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
    private Transform parentOrbitMover;
    public bool solarSystemRotationActive;

    public Vector3 axisToRotateAround;
    public float speedToRotateAroundWith;

    [Header("Orbits")]
    [SerializeField] private string attractorName = "";
    [SerializeField] private float distanceToAttractor = 0;
    private Quaternion solarSystemRotationBeforeReset;
    private Vector3 directionToSunBeforeReset;
    private Vector3[] moonsDirectionToPlanetBeforeReset;
    private bool reset;


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
        rotationSpeed = rand.Next(3,6);
        orbitSpeed = 360 / (3f * Mathf.PI * Mathf.Sqrt(Mathf.Pow(transform.position.magnitude, 3)) * 0.000006673f);

        willGeneratePlanetLife = rand.Value() < chanceToSpawnPlanetLife;
        willGeneratePlanetLife = false;

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
        
        parentOrbitMover = transform.parent;
        
    }

    public void Run()
    {
        if (player.parent != transform)
        {
            RotateAroundAxis();
            parentOrbitMover.transform.RotateAround(Universe.sunPosition.position, Vector3.up, orbitSpeed * Time.deltaTime);
            KeepPlanetAtSameDistanceToSun();
            RotateMoons(false);
        }
        else if (rotateMoons)
        {
            RotateMoons(true);
        }
        else
        {
            // For testing, remove later
            RotateMoons(false);
        }
    }

    public void SetUpResetComponents(Quaternion planetRotationBefore)
    {
        solarSystemRotationBeforeReset = planetRotationBefore;
        directionToSunBeforeReset = parentOrbitMover.transform.position - Universe.sunPosition.position;

        
        moonsDirectionToPlanetBeforeReset = new Vector3[moons.Count];
        for (int i = 0; i < moons.Count; i++)
        {
            Transform moonTransform = moons[i].transform;
            moonsDirectionToPlanetBeforeReset[i] = moonTransform.position - parentOrbitMover.transform.position;
        }
        reset = true;
    }

    public void ResetPlanetAndMoons()
    {
        KeepPlanetAtSameDistanceToSun();
        ResetMoons();

        directionToSunBeforeReset = Quaternion.Inverse(solarSystemRotationBeforeReset) * directionToSunBeforeReset;
        Vector3 directionatZeroY = directionToSunBeforeReset;
        directionatZeroY.y = 0;
        directionToSunBeforeReset = Quaternion.FromToRotation(directionToSunBeforeReset, directionatZeroY) * directionToSunBeforeReset;

        parentOrbitMover.transform.rotation = Quaternion.identity;
        parentOrbitMover.transform.position = Universe.sunPosition.position + directionToSunBeforeReset;

        moonsParent.transform.rotation = Quaternion.identity;
        moonsParent.transform.localPosition = Vector3.zero;
        for (int i = 0; i < moonsDirectionToPlanetBeforeReset.Length; i++)
        {
            Vector3 moonDirection = moonsDirectionToPlanetBeforeReset[i];

            moonDirection = Quaternion.Inverse(solarSystemRotationBeforeReset) * moonDirection;
            Vector3 moonDirectionAtZeroY = moonDirection;
            moonDirectionAtZeroY.y = 0;
            moonDirection = Quaternion.FromToRotation(moonDirection, moonDirectionAtZeroY) * moonDirection;

            moonsDirectionToPlanetBeforeReset[i] = moonDirection;
            moons[i].transform.parent.rotation = Quaternion.identity;
            moons[i].transform.parent.position = parentOrbitMover.transform.position + moonDirection;
        }
    }

    private void OnDrawGizmos()
    {
        if (moonsrelativeDistances == null)
        {
            return;
        }

        Transform sunTransform = Universe.sunPosition;
        float radius = (parentOrbitMover.position - sunTransform.position).magnitude;
        Universe.DrawGizmosCircle(sunTransform.position, sunTransform.up, radius, 32);

        foreach (Planet moon in moons)
        {
            Transform moonsParentTransform = moonsParent.transform;
            float moonRadius = (moon.transform.position - moonsParentTransform.position).magnitude;
            Universe.DrawGizmosCircle(moonsParentTransform.position, moonsParentTransform.up, moonRadius, 32);
        }
        
        if (reset && player.parent != transform)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(sunTransform.position, sunTransform.position + directionToSunBeforeReset);
        }
        else if (reset)
        {
            for (int i = 0; i < moonsDirectionToPlanetBeforeReset.Length; i++)
            {
                Vector3 moonDirection = moonsDirectionToPlanetBeforeReset[i];

                Gizmos.color = Color.red;
                Gizmos.DrawLine(moonsParent.transform.position, moonsParent.transform.position + moonDirection);
            }
        }
    }

    private void RotateAroundAxis()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void RotateMoons(bool moonsParentIsActivePlanet)
    {
        Transform sunTransform = Universe.sunPosition;

        if (moonsParentIsActivePlanet)
        {
            moonsParent.transform.RotateAround(Vector3.zero, -axisToRotateAround,  speedToRotateAroundWith * Time.deltaTime);
        }
        moonsParent.transform.localPosition = Vector3.zero;
        moonsParent.transform.up = sunTransform.up;

        for (int i = 0; i < moons.Count; i++)
        {
            Transform moon = moons[i].transform;
            moon.transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
            moon.parent.transform.RotateAround(moonsParent.transform.position, Vector3.up, 2f * Time.deltaTime);

            Vector3 direction = moon.parent.transform.position - moonsParent.transform.position;
            moon.parent.transform.position = moonsParent.transform.position + (direction.normalized * moonsrelativeDistances[i].magnitude);

            moon.parent.transform.position = ClosestPointOnPlane(moonsParent.transform.position, moonsParent.transform.TransformDirection(Vector3.up), moon.parent.transform.position);
            moon.parent.transform.up = moonsParent.transform.up;
        }
    }

    // Reset the moons rotation
    public void ResetMoons()
    {
        rotateMoons = false;
        moonsParent.transform.rotation = Quaternion.Euler(0, moonsParent.transform.rotation.y, 0);//Quaternion.identity;
        setUpSystemRotationComponents = false;
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
    public void HandleSolarSystemOrbit(Vector3 rotationaxis, float speed)
    {
        if (bodyName.Contains("Planet") && player.parent != transform)
        {
            SetUpComponents(rotationAxis, speed);
            solarSystemRotationActive = true;
        }
    }

    public void KeepPlanetAtSameDistanceToSun()
    {
        Vector3 sunPosition = Universe.sunPosition.position;
        Transform sunTransform = Universe.sunPosition.transform;

        parentOrbitMover.transform.position = ClosestPointOnPlane(sunPosition, sunTransform.TransformDirection(Vector3.up), parentOrbitMover.transform.position);

        Vector3 direction = parentOrbitMover.transform.position - sunPosition;
        parentOrbitMover.transform.position = sunPosition + (direction.normalized * positionrelativeToSunDistance);

    }

    private Vector3 ClosestPointOnPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return point + DistanceFromPlane(planeOffset, planeNormal, point) * planeNormal;
    }

    private float DistanceFromPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return Vector3.Dot(planeOffset - point, planeNormal);
    }

    private void SetUpComponents(Vector3 rotationAxis, float speed)
    {
        if (setUpSystemRotationComponents) return;
        axisToRotateAround = rotationAxis;
        speedToRotateAroundWith = speed;
    }
}
