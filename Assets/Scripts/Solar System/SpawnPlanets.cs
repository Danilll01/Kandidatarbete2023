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


    [SerializeField] private Material sunMaterial;

    void Awake()
    {
        CreatePlanets();
    }

    /// <summary>
    /// Creates all the planets
    /// </summary>
    void CreatePlanets()
    {
        bodies = new List<Planet>();

        // Create a sun object
        GameObject Sun = Instantiate(planetsPrefab);
        Sun.transform.parent = planetsParent.transform;
        Sun.gameObject.name = "Sun";
        Sun.GetComponentInChildren<MeshRenderer>().material = sunMaterial;

        Planet SunPlanetBody = Sun.GetComponent<Planet>();
        SunPlanetBody.bodyName = "Sun";
        SunPlanetBody.radius = radiusMaxValue * 2;
        SunPlanetBody.SetUpPlanetValues();
        SunPlanetBody.Initialize();
        bodies.Add(SunPlanetBody);

        // Create all other planets and helpers
        for (int i = 1; i < numberOfPlanets + 1; i++)
        {
            GameObject planet = Instantiate(planetsPrefab);
            planet.transform.parent = planetsParent.transform;
            planet.transform.position = new Vector3(0,0, radiusMaxValue * 5 * i);
            planet.gameObject.name = "Planet " + i;

            Planet planetBody = planet.GetComponent<Planet>();
            planetBody.bodyName = "Planet " + i;
            planetBody.radius = Random.Range(radiusMinValue, radiusMaxValue + 1);
            planetBody.SetUpPlanetValues();
            planetBody.Initialize();
            bodies.Add(planetBody);

            GameObject velocityHelper = new GameObject();
            velocityHelper.gameObject.name = "VelocityHelper";
            velocityHelper.transform.parent = planet.transform;

            int orbitOffset = Random.Range(orbitOffsetMinValue, orbitOffsetMaxValue);
            velocityHelper.transform.localPosition = new Vector3(100, orbitOffset, orbitOffset);

            // Assign needed scripts to the planet
            planet.AddComponent<KeplerOrbitMover>();
            planet.AddComponent<KeplerOrbitLineDisplay>();

            // Not nessecarry, used for debug
            planet.GetComponent<KeplerOrbitLineDisplay>().MaxOrbitWorldUnitsDistance = 50000;
            planet.GetComponent<KeplerOrbitLineDisplay>().LineRendererReference = planet.GetComponent<LineRenderer>();

            // Setup settings for the orbit script with the sun as the central body
            KeplerOrbitMover planetOrbitMover = planet.GetComponent<KeplerOrbitMover>();
            planetOrbitMover.AttractorSettings.AttractorObject = Sun.transform;
            planetOrbitMover.AttractorSettings.AttractorMass = Sun.GetComponent<Planet>().mass;
            planetOrbitMover.AttractorSettings.GravityConstant = Universe.gravitationalConstant;
            planetOrbitMover.VelocityHandle = velocityHelper.transform;
            planetOrbitMover.SetUp();
            planetOrbitMover.SetAutoCircleOrbit();
            planetOrbitMover.ForceUpdateOrbitData();
        }
    }
}
