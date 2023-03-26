using System.Collections;
using System.Collections.Generic;
using ExtendedRandom;
using SimpleKeplerOrbits;
using UnityEngine;

public class SpawnMoons : MonoBehaviour
{
    public GameObject planetsPrefab;
    private RandomX random;
    public Transform player;
    

    // Start is called before the first frame update
    void Start()
    {
       
        random = new RandomX(100);
        // Create all other planets and helpers                                                                              
        GameObject planet = Instantiate(planetsPrefab);

        Planet planetBody = planet.GetComponent<Planet>();
        planetBody.bodyName = "Planet ";
        planetBody.radius = 100;


        int nrOfMoonsForPlanet = 3;
        planet.transform.localPosition = Vector3.zero;
        planet.gameObject.name = "Planet ";


        planetBody.SetUpPlanetValues();
        planetBody.Initialize(player, 100, false);
        InstantiateMoons(planetBody, nrOfMoonsForPlanet);

        planetBody.InitializeMoonsValues();
        //SetupOrbitComponents(Sun, planet);
    }


    private void InstantiateMoons(Planet parentPlanet, int numberOfMoons)
    {
        GameObject moonsParent = new GameObject("Moons Parent");
        moonsParent.transform.parent = parentPlanet.transform;
        moonsParent.transform.localPosition = Vector3.zero;

        for (int i = 1; i < numberOfMoons + 1; i++)
        {
            GameObject moonsOrbitObject = new GameObject("Moons Orbit Object");
            moonsOrbitObject.transform.parent = moonsParent.transform;
            moonsOrbitObject.transform.localPosition = Vector3.zero;
            GameObject moon = Instantiate(planetsPrefab);
            moon.transform.parent = moonsOrbitObject.transform;
            moon.transform.localPosition = Vector3.zero;
            moonsOrbitObject.transform.localPosition = RandomPointOnCircleEdge(parentPlanet.radius * (i + 1));
            moon.gameObject.name = "Moon " + i;

            Planet moonBody = moon.GetComponent<Planet>();
            moonBody.bodyName = "Moon " + i;
            moonBody.radius = random.Next((int)(parentPlanet.radius / 5), (int)((parentPlanet.radius / 2) + 1));
            moonBody.SetUpPlanetValues();
            moonBody.Initialize(null, random.Next(), false); //False here because we don't spawn on moons      
            parentPlanet.moons.Add(moonBody);
            parentPlanet.moonsOrbitObjects.Add(moonsOrbitObject);
            //SetupOrbitComponents(parentPlanet.gameObject, moonsOrbitObject);
        }

        parentPlanet.moonsParent = moonsParent;
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

        velocityHelper.transform.localPosition = new Vector3(100, 0, 0);

        // Assign needed scripts to the planet                                                                                                                      
        planet.AddComponent<KeplerOrbitMover>();
        planet.AddComponent<KeplerOrbitLineDisplay>();

        // Not nessecarry, used for debug                                                                                                                           
        planet.GetComponent<KeplerOrbitLineDisplay>().MaxOrbitWorldUnitsDistance =
            (Attractor.transform.position - planet.transform.position).magnitude * 1.2f;
        planet.GetComponent<KeplerOrbitLineDisplay>().LineRendererReference = planet.GetComponent<LineRenderer>();

        // Setup settings for the orbit script with the sun as the central body                                                                                     
        KeplerOrbitMover planetOrbitMover = planet.GetComponent<KeplerOrbitMover>();
        planetOrbitMover.AttractorSettings.AttractorObject = Attractor.transform;
        planetOrbitMover.AttractorSettings.AttractorMass = 10;

        planetOrbitMover.AttractorSettings.GravityConstant = Universe.gravitationalConstant;
        planetOrbitMover.VelocityHandle = velocityHelper.transform;
        planetOrbitMover.SetUp();
        planetOrbitMover.SetAutoCircleOrbit();
        planetOrbitMover.LockOrbitEditing = true;
        planetOrbitMover.ForceUpdateOrbitData();
    }
}