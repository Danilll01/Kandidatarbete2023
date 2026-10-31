using System.Collections;
using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;
using SimpleKeplerOrbits;
using UnityEngine.Serialization;

public class SpawnPlanets : MonoBehaviour
{
    [HideInInspector] public List<Planet> bodies;
    [SerializeField] private GameObject planetsPrefab;
    [SerializeField] private GameObject sunPrefab;
    [SerializeField] private GameObject planetsParent;
    [SerializeField] private int numberOfPlanets;
    [SerializeField] private int radiusMinValue = 500;
    [SerializeField] private int radiusMaxValue = 1500;
    [SerializeField] private int chanceOfMoonsLimit = 5;
    [SerializeField] private int minNumberOfMoons = 1;
    [SerializeField] private int maxNumberOfMoons = 5;

    [SerializeField] private Material sunMaterial;      // Can this be removed?
    [SerializeField] private PillPlayerController player;
    [SerializeField] private DirectionalSun sunLightning;
    [HideInInspector] public GameObject sun;

    [HideInInspector] public bool solarSystemGenerated = false;

    [SerializeField] private bool randomizeSpawnPlanet = false;
    private RandomX random;

    private int spawnPlanetIndex;

    // Radii around a body where it captures the player as the centre of the solar system, with some margin
    // (SolarSystemTransform releases the player at 1.9 radii)
    private const float captureSphereRadii = 2f;

    void Awake()
    {
        // Checks if we are in the Unity editor, this will make it so we don't have to start from start meny
        if (Application.isEditor && Universe.random == null)
        {
            Universe.InitializeRandomWithSeed();
        }
        random = Universe.random;
        Universe.player = player;
        GetValues();
        CreatePlanets();
        player.Initialize(bodies[spawnPlanetIndex], random.Next());
        sunLightning.Initialize();
        solarSystemGenerated = true;
        WorldLoadingScreen.Show(bodies[spawnPlanetIndex].ChunksHandler);
    }

    private void GetValues()
    {
        numberOfPlanets = Universe.nrOfPlanets;

        // Randomize player index
        spawnPlanetIndex = randomizeSpawnPlanet ? random.Next(0,numberOfPlanets) : 0; 
    }

    // Creates all the planets 
    private void CreatePlanets()
    {
        bodies = new List<Planet>();

        // Create a sun object
        sun = Instantiate(sunPrefab);
        sun.transform.parent = planetsParent.transform;
        sun.transform.localPosition = new Vector3(0, 0, 0);
        sun.gameObject.name = "Sun";

        Sun sunPlanetBody = sun.GetComponent<Sun>();
        sunPlanetBody.diameter = radiusMaxValue * 2;
        sunPlanetBody.SetUpPlanetValues();
        sunPlanetBody.Initialize(random.Next());

        sun.transform.GetChild(0).localScale = new Vector3(sunPlanetBody.diameter, sunPlanetBody.diameter, sunPlanetBody.diameter);

        Universe.sunPosition = sun.transform;
        
        InstantiatePlanets();
    }

    // Instantiates the all the components on all the planets
    private void InstantiatePlanets()
    {
        // Create all other planets and helpers
        for (int i = 0; i < numberOfPlanets; i++)
        {
            GameObject planetOrbitObject = new GameObject("Planet " + i )
            {
                transform =
                {
                    parent = planetsParent.transform,
                    localPosition = Vector3.zero
                }
            };
            
            GameObject planet = Instantiate(planetsPrefab, planetOrbitObject.transform, true);
            planet.transform.localPosition = Vector3.zero;

            Planet planetBody = planet.GetComponent<Planet>();
            planetBody.bodyName = "Planet " + i + " body";
            planetBody.radius = random.Next(radiusMinValue, radiusMaxValue);
            
            planet.gameObject.name = "Planet " + i + " body";

            planetOrbitObject.transform.localPosition = CalculatePositionForPlanet(planetBody, i);
            float sunDistance = planetOrbitObject.transform.localPosition.magnitude;
            int nrOfMoonsForPlanet = GetNrOfMoonsToGenerate();
            InstantiateMoons(planetBody, nrOfMoonsForPlanet, sunDistance);

            // Only if this system's moons or capture spheres would reach into the previous system or the sun,
            // move it (with its moons) out just far enough. Other systems stay exactly where they were.
            float minimumSunDistance = MinimumSunDistance(planetBody, i);
            if (sunDistance < minimumSunDistance)
            {
                planetOrbitObject.transform.localPosition = planetOrbitObject.transform.localPosition.normalized * minimumSunDistance;
                sunDistance = minimumSunDistance;
                foreach (Planet moon in planetBody.moons)
                {
                    moon.positionRelativeToSunDistance = sunDistance;
                }
            }
            planetBody.positionRelativeToSunDistance = sunDistance;
            planetBody.Initialize(random.Next(), i == spawnPlanetIndex);
            planetBody.SetUpPlanetValues();
            bodies.Add(planetBody);

        }
    }


    private Vector3 CalculatePositionForPlanet(Planet planet, int index)
    {
        Vector3 pos = Vector3.one;

        // Calculates the position from the sun given the number of moons 
        if (index == 0)
        {
            float totalMoonRadiusOfPlanet = GetTotalMoonRadiusOfPlanet(planet);
            float totalRadiusOfCurrentPlanet = planet.radius + totalMoonRadiusOfPlanet;
            float sunRadius = sun.GetComponent<Sun>().diameter;
            float offset = random.Next(radiusMinValue, radiusMaxValue) * 2f;
            float distanceFromSun = sunRadius + totalRadiusOfCurrentPlanet + offset;

            pos = RandomPointOnCircleEdge(distanceFromSun);
        }

        // Calculates the position from the sun based on previous planets position and radius
        else
        {
            Planet previousPlanet = bodies[index - 1];
            float totalMoonRadiusOfPreviousPlanet = GetTotalMoonRadiusOfPlanet(previousPlanet);
            float totalRadiusOfPreviousPlanet = previousPlanet.radius + totalMoonRadiusOfPreviousPlanet;
            float previousPlanetPosMagnitude = previousPlanet.gameObject.transform.position.magnitude;

            float totalMoonRadiusOfCurrentPlanet = GetTotalMoonRadiusOfPlanet(planet);
            float totalRadiusOfCurrentPlanet = planet.radius + totalMoonRadiusOfCurrentPlanet;

            float offset = random.Next(radiusMaxValue, radiusMaxValue * 2) * 2f;
            float distanceFromSun = previousPlanetPosMagnitude + totalRadiusOfPreviousPlanet + totalRadiusOfCurrentPlanet + offset;
            pos = RandomPointOnCircleEdge(distanceFromSun);
        }
        return pos;
    }

    private float GetTotalMoonRadiusOfPlanet(Planet planet)
    {
        int lastMoonIndex = planet.moons.Count - 1;

        if (lastMoonIndex < 0)
        {
            return 0;
        }
        
        float totalMoonRadius = (planet.moons[lastMoonIndex].transform.position - planet.transform.position).magnitude;
        return totalMoonRadius;
    }

    // The closest a planet can be to the sun without its system reaching into the previous planet's system or the sun
    private float MinimumSunDistance(Planet planet, int index)
    {
        if (index == 0)
        {
            float sunRadius = sun.GetComponent<Sun>().diameter / 2f;
            return sunRadius + SystemReach(planet);
        }

        Planet previousPlanet = bodies[index - 1];
        return previousPlanet.transform.position.magnitude + SystemReach(previousPlanet) + SystemReach(planet);
    }

    /// <summary>
    /// How far from a planet its system reaches: its own capture sphere, or its moons with their capture spheres if further
    /// </summary>
    private static float SystemReach(Planet planet)
    {
        float reach = captureSphereRadii * planet.radius;
        foreach (Planet moon in planet.moons)
        {
            float moonDistance = (moon.transform.position - planet.transform.position).magnitude;
            reach = Mathf.Max(reach, moonDistance + captureSphereRadii * moon.radius);
        }
        return reach;
    }


    private int GetNrOfMoonsToGenerate()
    {
        int shouldHaveMoons = random.Next(1, 10);
        int numberOfMoons = 0;

        if (shouldHaveMoons >= chanceOfMoonsLimit)
        {
            numberOfMoons = random.Next(minNumberOfMoons, maxNumberOfMoons + 1);
        }

        return numberOfMoons;
    }

    // Instantiate moons for the given planet
    private void InstantiateMoons(Planet parentPlanet, int numberOfMoons, float sunDistance)
    {
        GameObject moonsParent = new GameObject("Moons parent")
        {
            transform =
            {
                parent = parentPlanet.transform.parent.transform,
                localPosition = Vector3.zero
            }
        };

        for (int i = 1; i < numberOfMoons + 1; i++)
        {
            GameObject moonsOrbitObject = new GameObject("Moon orbit object")
            {
                transform =
                {
                    parent = moonsParent.transform,
                    localPosition = Vector3.zero
                }
            };
            
            GameObject moon = Instantiate(planetsPrefab, moonsOrbitObject.transform);
            moon.transform.localPosition = Vector3.zero;
            moonsOrbitObject.transform.localPosition = RandomPointOnCircleEdge(parentPlanet.radius * (i * 3f));
            moon.gameObject.name = "Moon " + i;

            Planet moonBody = moon.GetComponent<Planet>();
            moonBody.bodyName = "Moon " + i;
            moonBody.radius = random.Next((int)(parentPlanet.radius / 5), (int)((parentPlanet.radius / 2) + 1));
            moonBody.positionRelativeToSunDistance = sunDistance;
            moonBody.Initialize(random.Next(), false); //False here because we don't spawn on moons
            moonBody.SetUpPlanetValues();
            parentPlanet.moons.Add(moonBody);
        }
        parentPlanet.moonsParent = moonsParent;
        parentPlanet.InitializeMoonValues();
    }

    // Gives back a random position on the edge of a circle given the radius of the circle
    private Vector3 RandomPointOnCircleEdge(float radius)
    {
        Vector2 vector2 = random.OnUnitCircle() * radius;
        return new Vector3(vector2.x, 0, vector2.y);
    }
}
