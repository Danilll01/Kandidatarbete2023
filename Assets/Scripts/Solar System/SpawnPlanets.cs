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

            Vector3 position = CalculatePositionForPlanet(planetBody, i);
            int nrOfMoonsForPlanet = GetNrOfMoonsToGenerate();

            // Move the planet out so its whole system, with its moons and their capture spheres, fits outside the previous one
            position += position.normalized * SystemRadius(planetBody.radius, nrOfMoonsForPlanet);
            planetOrbitObject.transform.localPosition = position;
            float sunDistance = position.magnitude;
            InstantiateMoons(planetBody, nrOfMoonsForPlanet, sunDistance);
            planetBody.positionRelativeToSunDistance = sunDistance;
            planetBody.Initialize(random.Next(), i == spawnPlanetIndex);
            planetBody.SetUpPlanetValues();
            bodies.Add(planetBody);

        }
    }


    private Vector3 CalculatePositionForPlanet(Planet planet, int index)
    {
        Vector3 pos = Vector3.one;

        // Calculates the position from the sun. The planet's own system radius is added by the caller once its moons are known.
        if (index == 0)
        {
            float sunRadius = sun.GetComponent<Sun>().diameter;
            float offset = random.Next(radiusMinValue, radiusMaxValue) * 2f;
            float distanceFromSun = sunRadius + offset;

            pos = RandomPointOnCircleEdge(distanceFromSun);
        }

        // Calculates the position from the sun based on previous planets position and system radius
        else
        {
            Planet previousPlanet = bodies[index - 1];
            float previousSystemRadius = SystemRadius(previousPlanet.radius, previousPlanet.moons.Count);
            float previousPlanetPosMagnitude = previousPlanet.gameObject.transform.position.magnitude;

            float offset = random.Next(radiusMaxValue, radiusMaxValue * 2) * 2f;
            float distanceFromSun = previousPlanetPosMagnitude + previousSystemRadius + offset;
            pos = RandomPointOnCircleEdge(distanceFromSun);
        }
        return pos;
    }

    /// <summary>
    /// How far from a planet its system reaches: its outermost moon plus that moon's capture sphere, or the planet's own
    /// capture sphere if that is further. Moon i orbits at 3 * i planet radii and is at most half a planet radius big.
    /// </summary>
    private static float SystemRadius(float planetRadius, int numberOfMoons)
    {
        float moonsReach = numberOfMoons > 0 ? 3f * numberOfMoons * planetRadius + captureSphereRadii * planetRadius / 2f : 0;
        return Mathf.Max(captureSphereRadii * planetRadius, moonsReach);
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
