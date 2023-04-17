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

    void Awake()
    {
        // Checks if we are in the Unity editor, this will make it so we don't have to start from start meny
        if (Application.isEditor && Universe.random == null)
        {
            Universe.InitializeRandomWithSeed();
        }
        random = Universe.random;
        GetValues();
        CreatePlanets();
        player.Initialize(bodies[spawnPlanetIndex], random.Next());
        sunLightning.Initialize();
        solarSystemGenerated = true;
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

        Sun SunPlanetBody = sun.GetComponent<Sun>();
        SunPlanetBody.diameter = radiusMaxValue * 2;
        SunPlanetBody.SetUpPlanetValues();
        SunPlanetBody.Initialize(random.Next());

        sun.transform.GetChild(0).localScale = new Vector3(SunPlanetBody.diameter, SunPlanetBody.diameter, SunPlanetBody.diameter);

        Universe.sunPosition = sun.transform;
        
        InstantiatePlanets(sun);
    }

    // Instantiates the all the components on all the planets
    private void InstantiatePlanets(GameObject sun)
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


            int nrOfMoonsForPlanet = GetNrOfMoonsToGenerate();
            planetOrbitObject.transform.localPosition = CalculatePositionForPlanet(planetBody, i, nrOfMoonsForPlanet);
            planetBody.positionRelativeToSunDistance = planetOrbitObject.transform.localPosition.magnitude;
            planet.gameObject.name = "Planet " + i + " body";
            planetBody.SetUpPlanetValues();

            planetBody.Initialize(player.transform, random.Next(), i == spawnPlanetIndex);
            InstantiateMoons(planetBody, nrOfMoonsForPlanet);
            bodies.Add(planetBody);

        }
    }


    private Vector3 CalculatePositionForPlanet(Planet planet, int index, int moonsNumber)
    {
        Vector3 pos = Vector3.one;

        // Calculates the position from the sun given the number of moons 
        if (index == 0)
        {
            float totalRadiusOfCurrentPlanet = planet.radius + (planet.radius * moonsNumber);
            float sunRadius = sun.GetComponent<Sun>().diameter;
            float offset = random.Next(radiusMinValue, radiusMaxValue) * 2f;
            float distanceFromSun = sunRadius + totalRadiusOfCurrentPlanet + offset;

            pos = RandomPointOnCircleEdge(distanceFromSun);
        }

        // Calculates the position from the sun based on previous planets position and radius
        else
        {
            Planet previousPlanet = bodies[index - 1];
            int nrOfMoonsOnPreviousPlanet = previousPlanet.moons.Count;
            float totalRadiusOfPreviousPlanet = previousPlanet.radius + (previousPlanet.radius * nrOfMoonsOnPreviousPlanet);
            float previousPlanetPosMagnitude = previousPlanet.gameObject.transform.position.magnitude;

            float totalRadiusOfCurrentPlanet = planet.radius + (planet.radius * moonsNumber);

            float offset = random.Next(radiusMaxValue, radiusMaxValue * 2) * 2f;
            float distanceFromSun = previousPlanetPosMagnitude + totalRadiusOfPreviousPlanet + totalRadiusOfCurrentPlanet + offset;
            pos = RandomPointOnCircleEdge(distanceFromSun);
        }
        return pos;
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
    private void InstantiateMoons(Planet parentPlanet, int numberOfMoons)
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
            moonsOrbitObject.transform.localPosition = RandomPointOnCircleEdge(parentPlanet.radius * (i + 1.2f));
            moon.gameObject.name = "Moon " + i;

            Planet moonBody = moon.GetComponent<Planet>();
            moonBody.bodyName = "Moon " + i;
            moonBody.radius = random.Next((int)(parentPlanet.radius / 5), (int)((parentPlanet.radius / 2) + 1));
            moonBody.SetUpPlanetValues();
            moonBody.Initialize(player.transform, random.Next(), false); //False here beacause we don't spawn on moons
            parentPlanet.moons.Add(moonBody);
        }
        parentPlanet.moonsParent = moonsParent;
        parentPlanet.InitializeMoonValues();
    }

    // Gives back a random position on the edge of a circle given the radius of the circle
    private Vector3 RandomPointOnCircleEdge(float radius)
    {
        var vector2 = random.OnUnitCircle() * radius;
        return new Vector3(vector2.x, 0, vector2.y);
    }
}
