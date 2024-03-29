using System;
using System.Collections.Generic;
using ExtendedRandom;
using SimpleKeplerOrbits;
using UnityEngine;


[RequireComponent(typeof(TerrainColor))]
public class Planet : MonoBehaviour
{
    [SerializeField] private ComputeShader meshGenerator;
    [HideInInspector] public float waterDiameter;
    
    [HideInInspector] public float radius;
    [HideInInspector] public string bodyName = "TBT";
    [HideInInspector] public float mass;
    [HideInInspector] public List<Planet> moons;
    [HideInInspector] public MinMaxTerrainLevel terrainLevel;

    private Transform player;
    [HideInInspector] public MarchingCubes marchingCubes;

    [SerializeField, Range(1, 14)] public int resolution = 5;

    public bool willGeneratePlanetLife = false;
    [SerializeField, Range(0f, 1f)] private float chanceToSpawnPlanetLife = 0.8f;
    [SerializeField] private ChunksHandler chunksHandler;
    [SerializeField] private WaterHandler waterHandler;
    public AtmosphereHandler atmosphereHandler;
    public FoliageHandler foliageHandler;
    public CreatureHandler creatureHandler;

    [Header("Terrain")] [SerializeField, Range(0, 1)]
    private float waterLevel = 0.92f;

    [SerializeField] private List<TerrainLayer> terrainLayers;
    public BiomeSettings biomeSettings;
    [HideInInspector] public Color[] biomeColors;

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
    private Quaternion[] moonRotationToKeep;

    private bool reset;

    private RandomX rand;

    

    /// <summary>
    /// Initializes the planet
    /// </summary>
    /// <param name="randomSeed">Seed to be used</param>
    /// <param name="spawn">True if the player will spawn on the planet</param>
    public void Initialize(int randomSeed, bool spawn)
    {
        rand = new RandomX(randomSeed);

        player = Universe.player.transform;

        terrainLevel = new MinMaxTerrainLevel();
        

        rotationAxis = rand.OnUnitSphere() * radius;
        rotationSpeed = rand.Next(3, 6);
        moonRotationToKeep = new Quaternion[moons.Count];
        for (int i = 0; i < moons.Count; i++)
        {
            moonRotationToKeep[i] = moons[i].transform.parent.rotation;
        }

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
            threshold = 23 + rand.Value() * 4;
            float biomeSeed = rand.Value();
            biomeSettings.seed = biomeSeed;
            
            marchingCubes = new MarchingCubes(biomeSeed, 1, meshGenerator, threshold, radius, terrainLayers, biomeSettings);
        }

        // Set water diameter
        waterDiameter = Mathf.Abs((threshold / 255 - 1) * 2 * radius * waterLevel) * rand.Value(0.99f, 1.01f);

        terrainLevel.SetMin(Mathf.Abs((waterDiameter + 1) / 2));

        chunksHandler.Initialize(this, terrainLevel, spawn, rand.Next());

        if (foliageHandler != null)
        {
            foliageHandler.Initialize(this, rand.Next());
        }

        if (creatureHandler != null)
        {
            creatureHandler.Initialize(this, rand.Next());
        }

        

        if (willGeneratePlanetLife) 
        {
            if (waterHandler != null && bodyName != "Sun")
            {
                waterHandler.Initialize(this, waterDiameter, rand.Next());
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
            
            if (!Universe.player.boarded)
            {
                player.parent.parent.SetParent(null, true);
            }
            else
            {
                Universe.spaceShip.parent.parent.SetParent(null, true);
            }
            
        }

        for (int i = 0; i < moons.Count; i++)
        {
            moonRotationToKeep[i] = moons[i].transform.parent.rotation;
        }
    }

    /// <summary>
    /// Basically an update function that is called from the SolarSystemTransform script
    /// </summary>
    public void Run()
    {
        
        Transform currentPlayerMover = Universe.player.boarded ? Universe.spaceShip.parent : player.parent;
        
        if (currentPlayerMover != transform && !playerIsOnMoon)
        {
            RotateAroundAxis();
            
            if (solarSystemRotationActive)
            {
                parentOrbitMover.transform.RotateAround(Universe.sunPosition.position, Universe.sunPosition.TransformDirection(Vector3.up),
                    orbitSpeed * Time.deltaTime * 1f);
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
                    speedToRotateAroundWith * Time.deltaTime * 1f);
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
                    Transform parent = moon.transform.parent;
                    parent.position = ClosestPointOnPlane(moonsParent.transform.position, moonsParent.transform.TransformDirection(Vector3.up), parent.transform.position);
                    parent.rotation = moonRotationToKeep[i];
                }
            }
        }
        else
        {
            for (int i = 0; i < moons.Count; i++)
            {
                Planet moon = moons[i];
                MakeMoonOrbitAndRotate(moon, i);
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

                Transform parent = moon.transform.parent;
                parent.position = ClosestPointOnPlane(moonsParent.transform.position, moonsParent.transform.TransformDirection(Vector3.up), parent.transform.position);
                parent.rotation = moonRotationToKeep[i];
            }
        }
        else
        {
            for (int i = 0; i < moons.Count; i++)
            {
                Planet moon = moons[i];
                MakeMoonOrbitAndRotate(moon, i);
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
        float density = rand.Value(2.4f, 3f);
        float volume = (4f / 3f) * (float)Math.PI * radius * radius * radius;
        
        mass = density * volume;
        gameObject.name = bodyName;
        
        biomeSettings.distance = Vector3.Distance(transform.position, Universe.sunPosition.position);

        chunksHandler.SetupMaterial();

        biomeColors = chunksHandler.terrainColor.biomeColors;
    }

    public BiomeSettings Biome => biomeSettings;

    public Color GetSeaColor => waterHandler.GetWaterColor;

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
    private void KeepPlanetAtSameDistanceToSun()
    {
        Vector3 sunPosition = Universe.sunPosition.position;
        Transform sunTransform = Universe.sunPosition.transform;

        parentOrbitMover.transform.position = ClosestPointOnPlane(sunPosition,
            sunTransform.TransformDirection(Vector3.up), parentOrbitMover.transform.position);

        Vector3 direction = parentOrbitMover.transform.position - sunPosition;
        parentOrbitMover.transform.position = sunPosition + (direction.normalized * positionRelativeToSunDistance);
    }

    private Vector3 ClosestPointOnPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return point + DistanceFromPlane(planeOffset, planeNormal, point) * planeNormal;
    }

    private static float DistanceFromPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
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
            if (playerIsOnMoon)
            {
                float radius = (parentOrbitMover.position - moons[activeMoonIndex].transform.position).magnitude;
                Universe.DrawGizmosCircle(moons[activeMoonIndex].transform.position, sunTransform.up, radius, 32);
            }
            else
            {
                float radius = (parentOrbitMover.position - sunTransform.position).magnitude;
                Universe.DrawGizmosCircle(sunTransform.position, sunTransform.up, radius, 32);
            }
            
            foreach (Planet moon in moons)
            {
                Transform moonsParentTransform = moonsParent.transform;
                float moonRadius = (moon.transform.position - moonsParentTransform.position).magnitude;
                Universe.DrawGizmosCircle(moonsParentTransform.position, moonsParentTransform.up, moonRadius, 32);
            }
        }
    }
}