using System.Collections;
using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;
using SimpleKeplerOrbits;

public class SpawnPlanets : MonoBehaviour
{
    [HideInInspector] public List<Planet> bodies;
    [SerializeField] private GameObject planetsPrefab;
    [SerializeField] private GameObject sunPrefab;
    [SerializeField] private GameObject planetsParent;
    [SerializeField] private int numberOfPlanets;
    [SerializeField] private int radiusMinValue = 500;
    [SerializeField] private int radiusMaxValue = 1500;
    [SerializeField] private int orbitOffsetMinValue = -10;
    [SerializeField] private int orbitOffsetMaxValue = 10;
    [SerializeField] private int chanceOfMoonsLimit = 5;
    [SerializeField] private int minNumberOfMoons = 1;
    [SerializeField] private int maxNumberOfMoons = 5;

    [SerializeField] private Material sunMaterial;
    [SerializeField] private PillPlayerController player;
    [SerializeField] private DirectionalSun sunLightning;
    [HideInInspector] public GameObject sun;

    [HideInInspector] public bool solarySystemGenerated = false;

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
        sunLightning.Initialize(sun.transform);
        player.Initialize(bodies[spawnPlanetIndex], random.Next());
        solarySystemGenerated = true;
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
        GameObject Sun = Instantiate(sunPrefab);
        Sun.transform.parent = planetsParent.transform;
        Sun.transform.localPosition = new Vector3(0, 0, 0);
        Sun.gameObject.name = "Sun";

        Sun SunPlanetBody = Sun.GetComponent<Sun>();
        SunPlanetBody.diameter = radiusMaxValue * 2;
        SunPlanetBody.SetUpPlanetValues();
        SunPlanetBody.Initialize(player.transform, random.Next());

        Sun.transform.GetChild(0).localScale = new Vector3(SunPlanetBody.diameter, SunPlanetBody.diameter, SunPlanetBody.diameter);

        GameObject velocityHelper = new GameObject();
        velocityHelper.gameObject.name = "VelocityHelper";
        velocityHelper.transform.parent = Sun.transform;
        velocityHelper.transform.localPosition = new Vector3(-100, 0, 0);

        Sun.GetComponent<KeplerOrbitMover>().VelocityHandle = velocityHelper.transform;
        sun = Sun;
        InstantiatePlanets(Sun);
    }

    // Instantiates the all the components on all the planets
    private void InstantiatePlanets(GameObject Sun)
    {
        // Create all other planets and helpers
        for (int i = 0; i < numberOfPlanets; i++)
        {
            GameObject planet = Instantiate(planetsPrefab);
            planet.transform.parent = planetsParent.transform;

            Planet planetBody = planet.GetComponent<Planet>();
            planetBody.bodyName = "Planet " + i;
            planetBody.radius = random.Next(radiusMinValue, radiusMaxValue);


            int nrOfMoonsForPlanet = GetNrOfMoonsToGenerate();
            planet.transform.localPosition = CalculatePositionForPlanet(planetBody, i, nrOfMoonsForPlanet);
            planet.gameObject.name = "Planet " + i;


            planetBody.SetUpPlanetValues();
            planetBody.Initialize(player.transform, random.Next(), i == spawnPlanetIndex);
            InstantiateMoons(planetBody, nrOfMoonsForPlanet);
            bodies.Add(planetBody);

            SetupOrbitComponents(Sun, planet);
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
            float offset = random.Next(radiusMinValue, radiusMaxValue) * 1.2f;
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

            float offset = random.Next(radiusMinValue, radiusMaxValue + 1) * 1.2f;
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
                parent = parentPlanet.transform,
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
            moonsOrbitObject.transform.localPosition = RandomPointOnCircleEdge(parentPlanet.radius * (i + 1));
            moon.gameObject.name = "Moon " + i;

            Planet moonBody = moon.GetComponent<Planet>();
            moonBody.bodyName = "Moon " + i;
            moonBody.radius = random.Next((int)(parentPlanet.radius / 5), (int)((parentPlanet.radius / 2) + 1));
            moonBody.SetUpPlanetValues();
            moonBody.Initialize(player.transform, random.Next(), false); //False here beacause we don't spawn on moons
            parentPlanet.moons.Add(moonBody);
            moonBody.moonsParent = moonsParent;
            SetupOrbitComponents(parentPlanet.gameObject, moonsOrbitObject);
        }
    }

    // Gives back a random position on the edge of a circle given the radius of the circle
    private Vector3 RandomPointOnCircleEdge(float radius)
    {
        var vector2 = random.OnUnitCircle() * radius;
        return new Vector3(vector2.x, 0, vector2.y);
    }

    // Adds all componets for orbit movement for given planet and it's attractor
    private void SetupOrbitComponents(GameObject Attractor, GameObject planet)
    {
        GameObject velocityHelper = new GameObject();
        velocityHelper.gameObject.name = "VelocityHelper";
        velocityHelper.transform.parent = planet.transform;

        int orbitOffset = random.Next(orbitOffsetMinValue, orbitOffsetMaxValue);
        velocityHelper.transform.localPosition = new Vector3(100, orbitOffset, orbitOffset);

        // Assign needed scripts to the planet
        planet.AddComponent<KeplerOrbitMover>();
        planet.AddComponent<KeplerOrbitLineDisplay>();

        // Not nessecarry, used for debug
        planet.GetComponent<KeplerOrbitLineDisplay>().MaxOrbitWorldUnitsDistance = (Attractor.transform.position - planet.transform.position).magnitude * 1.2f;
        planet.GetComponent<KeplerOrbitLineDisplay>().LineRendererReference = planet.GetComponent<LineRenderer>();

        // Setup settings for the orbit script with the sun as the central body
        KeplerOrbitMover planetOrbitMover = planet.GetComponent<KeplerOrbitMover>();
        planetOrbitMover.AttractorSettings.AttractorObject = Attractor.transform;
        if (Attractor.gameObject.name == "Sun")
        {
            planetOrbitMover.AttractorSettings.AttractorMass = Attractor.GetComponent<Sun>().mass;
        }
        else
        {
            planetOrbitMover.AttractorSettings.AttractorMass = Attractor.GetComponent<Planet>().mass;
        }
        planetOrbitMover.AttractorSettings.GravityConstant = Universe.gravitationalConstant;
        planetOrbitMover.VelocityHandle = velocityHelper.transform;
        planetOrbitMover.SetUp();
        planetOrbitMover.SetAutoCircleOrbit();
        planetOrbitMover.ForceUpdateOrbitData();
        planetOrbitMover.LockOrbitEditing = true;
    }
}
