using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleKeplerOrbits;

public class SpawnPlanets : MonoBehaviour
{
    [HideInInspector] public List<Planet> bodies;
    [SerializeField] private GameObject planetsPrefab;
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

    void Awake()
    {
        CreatePlanets();
    }

    // Creates all the planets 
    private void CreatePlanets()
    {
        bodies = new List<Planet>();

        // Create a sun object
        GameObject Sun = Instantiate(planetsPrefab);
        Sun.transform.parent = planetsParent.transform;
        Sun.transform.localPosition = new Vector3(0, 0, 0);
        Sun.gameObject.name = "Sun";
        Sun.GetComponentInChildren<MeshRenderer>().material = sunMaterial;

        Planet SunPlanetBody = Sun.GetComponent<Planet>();
        SunPlanetBody.bodyName = "Sun";
        SunPlanetBody.radius = radiusMaxValue * 2;
        SunPlanetBody.SetUpPlanetValues();
        SunPlanetBody.Initialize();
        bodies.Add(SunPlanetBody);
        InstantiatePlanets(Sun);
        
    }

    // Instantiates the all the components on all the planets
    private void InstantiatePlanets(GameObject Sun)
    {
        // Create all other planets and helpers
        for (int i = 1; i < numberOfPlanets + 1; i++)
        {
            GameObject planet = Instantiate(planetsPrefab);
            planet.transform.parent = planetsParent.transform;

            Planet planetBody = planet.GetComponent<Planet>();
            planetBody.bodyName = "Planet " + i;
            planetBody.radius = Random.Range(radiusMinValue, radiusMaxValue + 1);


            int nrOfMoonsForPlanet = GetNrOfMoonsToGenerate();
            planet.transform.localPosition = CalculatePositionForPlanet(planetBody, i, nrOfMoonsForPlanet);//RandomPointOnCircleEdge(radiusMaxValue * (maxNumberOfMoons + 0.2f) * i);
            planet.gameObject.name = "Planet " + i;

            
            planetBody.SetUpPlanetValues();
            planetBody.Initialize();
            InstantiateMoons(planetBody, nrOfMoonsForPlanet);
            bodies.Add(planetBody);
           
            SetupOrbitComponents(Sun, planet);
        }
    }

    
    private Vector3 CalculatePositionForPlanet(Planet planet, int index, int moonsNumber)
    {
        Vector3 pos = Vector3.one;

        // Calculates the position from the sun given the number of moons 
        if (index == 1)
        {
            float totalRadiusOfCurrentPlanet = planet.radius + (planet.radius * moonsNumber);
            float sunRadius = bodies[0].radius;
            float offset = Random.Range(radiusMinValue, radiusMaxValue + 1) * 1.2f;
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

            float offset = Random.Range(radiusMinValue, radiusMaxValue + 1) * 1.2f;
            float distanceFromSun = previousPlanetPosMagnitude + totalRadiusOfPreviousPlanet + totalRadiusOfCurrentPlanet + offset;
            pos = RandomPointOnCircleEdge(distanceFromSun);
        }
        return pos;
    }


    private int GetNrOfMoonsToGenerate()
    {
        int shouldHaveMoons = Random.Range(1, 10);
        int numberOfMoons = 0;

        if (shouldHaveMoons >= chanceOfMoonsLimit)
        {
            numberOfMoons = Random.Range(minNumberOfMoons, maxNumberOfMoons + 1);
        }

        return numberOfMoons;
    }

    // Instantiate moons for the given planet
    private void InstantiateMoons(Planet parentPlanet, int numberOfMoons)
    {
        for (int i = 1; i < numberOfMoons + 1; i++)
        {
            GameObject moon = Instantiate(planetsPrefab);
            moon.transform.parent = parentPlanet.transform;
            moon.transform.localPosition = RandomPointOnCircleEdge(parentPlanet.radius * i);
            moon.gameObject.name = "Moon " + i;

            Planet moonBody = moon.GetComponent<Planet>();
            moonBody.bodyName = "Moon " + i;
            moonBody.radius = Random.Range(parentPlanet.radius / 5, (parentPlanet.radius / 2) + 1);
            moonBody.SetUpPlanetValues();
            moonBody.Initialize();
            parentPlanet.moons.Add(moonBody);
            SetupOrbitComponents(parentPlanet.gameObject, moon);
        }
    }

    // Gives back a random position on the edge of a circle given the radius of the circle
    private Vector3 RandomPointOnCircleEdge(float radius)
    {
        var vector2 = Random.insideUnitCircle.normalized * radius;
        return new Vector3(vector2.x, 0, vector2.y);
    }

    // Adds all componets for orbit movement for given planet and it's attractor
    private void SetupOrbitComponents(GameObject Attractor, GameObject planet)
    {
        GameObject velocityHelper = new GameObject();
        velocityHelper.gameObject.name = "VelocityHelper";
        velocityHelper.transform.parent = planet.transform;

        int orbitOffset = Random.Range(orbitOffsetMinValue, orbitOffsetMaxValue);
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
        planetOrbitMover.AttractorSettings.AttractorMass = Attractor.GetComponent<Planet>().mass;
        planetOrbitMover.AttractorSettings.GravityConstant = Universe.gravitationalConstant;
        planetOrbitMover.VelocityHandle = velocityHelper.transform;
        planetOrbitMover.SetUp();
        planetOrbitMover.SetAutoCircleOrbit();
        planetOrbitMover.ForceUpdateOrbitData();
        planetOrbitMover.LockOrbitEditing = true;
    }
}
