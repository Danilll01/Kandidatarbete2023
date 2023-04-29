using System;
using System.Collections.Generic;
using ExtendedRandom;
using SimpleKeplerOrbits;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(TerrainColor))]
public class Planet : MonoBehaviour
{
    [SerializeField] private ComputeShader meshGenerator;
    [SerializeField] private Material waterMaterial; // Can this be removed?
    [HideInInspector] public float waterDiameter;

    [HideInInspector, Obsolete] public float diameter;
    [HideInInspector] public float radius;
    [HideInInspector] public float surfaceGravity;
    [HideInInspector] public string bodyName = "TBT";
    [HideInInspector] public float mass;
    [HideInInspector] public List<Planet> moons;
    
    public List<Vector3> waterPoints;

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

    [Header("Terrain")] [SerializeField, Range(0, 1)]
    
    private float waterLevel = 0.92f;

    private static readonly Color[] seaColors = new Color[] {
        new Color (219f/255, 144f/255, 101f/255),
        new Color (125f/255, 219f/255, 102f/255),
        new Color (102/255,  219f/255, 195f/255),
        new Color (102f/255, 183f/255, 219f/255),
        new Color (102f/255, 219f/255, 144f/255),
        new Color (102f/255, 105f/255, 219f/255),
        new Color (207f/255, 102f/255, 219f/255),
        new Color (219f/255, 102f/255, 142f/255),
        new Color (219f/255, 102f/255, 102f/255),
        new Color (251f/255, 70f/255, 47f/255),
        new Color (46f/255,  250f/255, 198f/255),
        new Color (47f/255,  233f/255, 250f/255),
        new Color (47f/255,  186f/255, 250f/255),
        new Color (47f/255,  137f/255, 250f/255),
        new Color (163f/255, 47f/255, 250f/255),
        new Color (0f/255,   44f/255, 147f/255),
        new Color (0f/255,   147f/255, 135f/255),
        new Color (0f/255,   147f/255, 136f/255),
        new Color (146f/255, 255f/255, 247f/255)
    };
    private Color seaColor;

    [SerializeField] private List<TerrainLayer> terrainLayers;
    public BiomeSettings biomeSettings;

    private float threshold;

    [Header("Orbits")] public float orbitSpeed;
    [HideInInspector] public Vector3 rotationAxis;
    [HideInInspector] public float rotationSpeed;
    [HideInInspector] public GameObject moonsParent;

    private Vector3 axisToRotateAround;
    private float speedToRotateAroundWith;

    public bool rotateMoons;

    public Vector3[] moonsRelativeDistances;
    [HideInInspector] public float positionRelativeToSunDistance;

    private bool setUpSystemRotationComponents;
    private Transform parentOrbitMover;
    public bool solarSystemRotationActive;
    public bool playerIsOnMoon;
    public int activeMoonIndex;

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
        
        seaColor = seaColors[rand.Next(seaColors.Length)];


        rotationAxis = rand.OnUnitSphere() * radius;
        rotationSpeed = rand.Next(3, 6);

        if (bodyName.Contains("Moon"))
        {
            Vector3 localPos = (transform.parent.parent.position - transform.position);
            orbitSpeed =  360 / (3f * Mathf.PI * Mathf.Sqrt(Mathf.Pow(localPos.magnitude, 3)) * 0.00006673f) * 2.5f;
        }
        else
        {
            orbitSpeed = 360 / (3f * Mathf.PI * Mathf.Sqrt(Mathf.Pow(transform.position.magnitude, 3)) * 0.000006673f);
        }

        willGeneratePlanetLife = rand.Value() < chanceToSpawnPlanetLife;

        // Initialize the meshgenerator
        if (marchingCubes == null)
        {
            threshold = 23 + (float) rand.Value() * 4;
            float biomeSeed = rand.Value();
            biomeSettings.seed = biomeSeed;
            
            marchingCubes = new MarchingCubes(biomeSeed, 1, meshGenerator, threshold, radius, terrainLayers, biomeSettings);
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

        if (foliageHandler != null)
        {
            foliageHandler.Initialize(this);
        }

        if (creatureHandler != null)
        {
            creatureHandler.Initialize(this, rand.Next());
        }

        terrainLevel.SetMin(Mathf.Abs((waterDiameter + 1) / 2));

        chunksHandler.Initialize(this, terrainLevel, spawn, rand.Next());

        if (willGeneratePlanetLife) 
        {
            if (waterHandler != null && bodyName != "Sun")
            {
                waterHandler.Initialize(this, waterDiameter, GetSeaColor());
            }
        }

        if (atmosphereHandler != null && bodyName != "Sun")
        {
            // Will generate planet life currently decides if there is atmosphere, could change this later when a better system is created
            // Depending on system, it could be advantageous to give the strength of the atmosphere too, this will have to be sent in as a parameter then 
            atmosphereHandler.Initialize(radius, waterDiameter / 2, willGeneratePlanetLife, rand.Next());
        }
    }

    // Get the initial distances from the moons to the planet
    public void InitializeMoonValues()
    {
        moonsRelativeDistances = new Vector3[moons.Count];

        for (int i = 0; i < moons.Count; i++)
        {
            moonsRelativeDistances[i] = moons[i].transform.parent.position - transform.position;
        }

        parentOrbitMover = transform.parent;
    }

    private void SetUpRotationComponents(Vector3 rotationAxisOfActivePlanet, float speed)
    {
        if (setUpSystemRotationComponents) return;
        axisToRotateAround = rotationAxisOfActivePlanet;
        speedToRotateAroundWith = speed;
        
        ResetMoonsParentRotation();
        
        if (playerIsOnMoon)
        {
            player.transform.parent.parent.SetParent(null, true);
        }
    }

    /// <summary>
    /// Basically an update function that is called from the SolarSystemTransform script
    /// </summary>
    public void Run()
    {
        if (player.parent != transform && !playerIsOnMoon)
        {
            RotateAroundAxis();
            
            if (solarSystemRotationActive)
            {
                parentOrbitMover.transform.RotateAround(Universe.sunPosition.position, Universe.sunPosition.TransformDirection(Vector3.up),
                    orbitSpeed * Time.deltaTime * 2.5f);
            }
            else
            {
                parentOrbitMover.transform.RotateAround(Universe.sunPosition.position, Vector3.up,
                    orbitSpeed * Time.deltaTime);
            }

            KeepPlanetAtSameDistanceToSun();
            RotateAndOrbitMoons(false);
        }
        else if (playerIsOnMoon)
        {
            RotateAroundAxis();
            if (solarSystemRotationActive)
            {
                parentOrbitMover.transform.RotateAround(Vector3.zero, Universe.sunPosition.TransformDirection(Vector3.up),
                    speedToRotateAroundWith * Time.deltaTime * 2.5f);
            }
            else
            {
                parentOrbitMover.transform.RotateAround(Vector3.zero, Vector3.up,
                    speedToRotateAroundWith * Time.deltaTime);
            }
            RotateAndOrbitMoonsAndParentPlanet();
        }
        else if (rotateMoons)
        {
            RotateAndOrbitMoons(true);
        }
    }

    private void RotateAroundAxis()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }
    
    private void RotateAndOrbitMoonsAndParentPlanet()
    {
        Transform sunTransform = Universe.sunPosition;

        if (solarSystemRotationActive)
        {
            // Rotate the active planets moons manually since it is not affected by solar system rotation
            
            parentOrbitMover.RotateAround(Vector3.zero, -axisToRotateAround, speedToRotateAroundWith * Time.deltaTime);
            Vector3 direction = transform.parent.position - Vector3.zero;
            parentOrbitMover.position = Vector3.zero + (direction.normalized * moonsRelativeDistances[activeMoonIndex].magnitude);
            parentOrbitMover.position = ClosestPointOnPlane(Vector3.zero, sunTransform.TransformDirection(Vector3.up), parentOrbitMover.position);

            moonsParent.transform.RotateAround(parentOrbitMover.position, -axisToRotateAround, speedToRotateAroundWith* Time.deltaTime);
            moonsParent.transform.localPosition = Vector3.zero;
            moonsParent.transform.rotation = sunTransform.rotation;

            for (int i = 0; i < moons.Count; i++)
            {
                Planet moon = moons[i];
                if (activeMoonIndex != i)
                {
                    MakeMoonOrbitAndRotate(moon, i);
                    Transform parent = moon.transform.parent.transform;
                    parent.position = ClosestPointOnPlane(moonsParent.transform.position, moonsParent.transform.TransformDirection(Vector3.up), parent.transform.position);
                    parent.transform.rotation = Quaternion.identity;
                }
                
            }
        }
        else
        {
            for (int i = 0; i < moons.Count; i++)
            {
                Planet moon = moons[i];
                MakeMoonOrbitAndRotate(moon, i);
                moon.transform.parent.rotation = Quaternion.identity;
            }
        }
    }

    private void RotateAndOrbitMoons(bool moonsParentIsActivePlanet)
    {
        Transform sunTransform = Universe.sunPosition;

        if (solarSystemRotationActive)
        {
            // Rotate the active planets moons manually since it is not affected by solar system rotation
            if (moonsParentIsActivePlanet)
            {
                moonsParent.transform.RotateAround(Vector3.zero, -axisToRotateAround, speedToRotateAroundWith * Time.deltaTime);
            }

            moonsParent.transform.localPosition = Vector3.zero;
            moonsParent.transform.rotation = sunTransform.rotation;

            for (int i = 0; i < moons.Count; i++)
            {
                Planet moon = moons[i];
                MakeMoonOrbitAndRotate(moon, i);

                Transform parent = moon.transform.parent.transform;
                parent.position = ClosestPointOnPlane(moonsParent.transform.position, moonsParent.transform.TransformDirection(Vector3.up), parent.transform.position);
                parent.rotation = Quaternion.identity;
            }
        }
        else
        {
            for (int i = 0; i < moons.Count; i++)
            {
                Planet moon = moons[i];
                MakeMoonOrbitAndRotate(moon, i);
                moon.transform.parent.rotation = Quaternion.identity;
            }
        }
    }

    private void MakeMoonOrbitAndRotate(Planet moon, int i)
    {
        moon.transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);

        Transform parentTransform = moon.transform.parent.transform;
        Transform moonsParentTransform = moonsParent.transform;
        parentTransform.RotateAround(moonsParentTransform.position, Universe.sunPosition.TransformDirection(Vector3.up), moon.orbitSpeed * Time.deltaTime * 2.5f);

        Vector3 direction = parentTransform.position - moonsParentTransform.position;
        parentTransform.position = moonsParentTransform.position + (direction.normalized * moonsRelativeDistances[i].magnitude);
    }

    /// <summary>
    /// Set up the values for the planets
    /// </summary>
    public void SetUpPlanetValues()
    {
        mass = surfaceGravity * 4 * radius * radius / Universe.gravitationalConstant;
        gameObject.name = bodyName;
    }

    public Color GetSeaColor()
    {
        return seaColor;
    }

    public BiomeSettings Biome
    {
        get { return biomeSettings; }
    }

    public float DistanceToSun
    {
        get { return Vector3.Distance(transform.position, Universe.sunPosition.position); }
    }

    /// <summary>
    /// Set up the components for solar system orbit
    /// </summary>
    public void HandleSolarSystemOrbit(Vector3 rotationAxisOfActivePlanet, float speed)
    {
        if (bodyName.Contains("Planet"))
        {
            SetUpRotationComponents(rotationAxisOfActivePlanet, speed);
            solarSystemRotationActive = true;
        }
    }

    /// <summary>
    /// Reset the orbit components of the planet and the moons
    /// </summary>
    public void ResetOrbitComponents()
    {
        if (bodyName.Contains("Planet"))
        {
            ResetMoonsParentRotation();
            KeepPlanetAtSameDistanceToSun();
            solarSystemRotationActive = false;
        }
    }

    private void ResetMoonsParentRotation()
    {
        
        foreach (Planet moon in moons)
        {
            moon.transform.parent.SetParent(null, true);
        }
        

        moonsParent.transform.rotation = Universe.sunPosition.rotation;

        foreach (Planet moon in moons)
        {
            moon.transform.parent.SetParent(moonsParent.transform, true);
        }
    }

    /// <summary>
    /// Adjusts the planets position to keep the same distance to the sun
    /// </summary>
    public void KeepPlanetAtSameDistanceToSun()
    {
        Vector3 sunPosition = Universe.sunPosition.position;
        Transform sunTransform = Universe.sunPosition.transform;

        parentOrbitMover.transform.position = ClosestPointOnPlane(sunPosition,
            sunTransform.TransformDirection(Vector3.up), parentOrbitMover.transform.position);

        Vector3 direction = parentOrbitMover.transform.position - sunPosition;
        parentOrbitMover.transform.position = sunPosition + (direction.normalized * positionRelativeToSunDistance);
    }

    public void IncreaseDistanceToSunByAmount(float amount)
    {
        positionRelativeToSunDistance += amount;
    }

    public void DecreaseDistanceToSunByAmount(float amount)
    {
        positionRelativeToSunDistance -= amount;
    }

    private Vector3 ClosestPointOnPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return point + DistanceFromPlane(planeOffset, planeNormal, point) * planeNormal;
    }

    private float DistanceFromPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return Vector3.Dot(planeOffset - point, planeNormal);
    }

    private void OnDrawGizmos()
    {
        if (moonsRelativeDistances == null)
        {
            return;
        }

        if (bodyName.Contains("Planet"))
        {
            Transform sunTransform = Universe.sunPosition;
            float radius = (parentOrbitMover.position - sunTransform.position).magnitude;
            Universe.DrawGizmosCircle(sunTransform.position, sunTransform.up, radius, 32);
            
            foreach (Planet moon in moons)
            {
                Transform moonsParentTransform = moonsParent.transform;
                float moonRadius = (moon.transform.position - moonsParentTransform.position).magnitude;
                Universe.DrawGizmosCircle(moonsParentTransform.position, moonsParentTransform.up, moonRadius, 32);
            }
        }
        

        
    }
}
