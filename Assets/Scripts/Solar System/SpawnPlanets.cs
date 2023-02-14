using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleKeplerOrbits;

public class SpawnPlanets : MonoBehaviour
{
    [HideInInspector] public List<PlanetBody> bodies;
    [SerializeField] private GameObject planetsPrefab;
    [SerializeField] private GameObject planetsParent;
    [SerializeField] private int numberOfPlanets;
    [SerializeField] public int radiusMinValue = 500;
    [SerializeField] public int radiusMaxValue = 1500;


    [SerializeField] private Material sunMaterial;

    void Awake()
    {
        Setup();
    }

    // Creates all the planets
    void Setup()
    {
        bodies = new List<PlanetBody>();

        // Create a sun object
        GameObject Sun = Instantiate(planetsPrefab);
        Sun.transform.parent = planetsParent.transform;
        Sun.gameObject.name = "Sun";
        Sun.GetComponentInChildren<MeshRenderer>().material = sunMaterial;

        PlanetBody SunPlanetBody = Sun.GetComponent<PlanetBody>();
        SunPlanetBody.bodyName = "Sun";
        SunPlanetBody.radius = radiusMaxValue * 2;
        SunPlanetBody.SetUp();
        bodies.Add(SunPlanetBody);

        // Create all other planets and helpers
        for (int i = 1; i < numberOfPlanets + 1; i++)
        {
            GameObject planet = Instantiate(planetsPrefab);
            planet.transform.parent = planetsParent.transform;
            planet.transform.position = new Vector3(0,0, radiusMaxValue * 5 * i);
            planet.gameObject.name = "Planet " + i;

            PlanetBody planetBody = planet.GetComponent<PlanetBody>();
            planetBody.bodyName = "Planet " + i;
            planetBody.radius = Random.Range(radiusMinValue, radiusMaxValue + 1);
            planetBody.SetUp();
            bodies.Add(planetBody);

            GameObject velocityHelper = new GameObject();
            velocityHelper.gameObject.name = "VelocityHelper";
            velocityHelper.transform.parent = planet.transform;

            // Assign needed scripts to the planet
            planet.AddComponent<KeplerOrbitMover>();
            planet.AddComponent<KeplerOrbitLineDisplay>();

            // Not nessecarry, used for debug
            planet.GetComponent<KeplerOrbitLineDisplay>().MaxOrbitWorldUnitsDistance = 50000;
            planet.GetComponent<KeplerOrbitLineDisplay>().LineRendererReference = planet.GetComponent<LineRenderer>();

            // Setup settings for the orbit script with the sun as the central body
            KeplerOrbitMover planetOrbitMover = planet.GetComponent<KeplerOrbitMover>();
            planetOrbitMover.AttractorSettings.AttractorObject = Sun.transform;
            planetOrbitMover.AttractorSettings.AttractorMass = Sun.GetComponent<PlanetBody>().mass;
            planetOrbitMover.AttractorSettings.GravityConstant = Universe.gravitationalConstant;
            planetOrbitMover.VelocityHandle = velocityHelper.transform;
            planetOrbitMover.SetUp();
            planetOrbitMover.SetAutoCircleOrbit();
            planetOrbitMover.ForceUpdateOrbitData();
        }
    }
}
