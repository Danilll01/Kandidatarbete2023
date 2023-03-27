using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleKeplerOrbits;

public class SolarSystemTransform : MonoBehaviour
{
    [SerializeField] private SpawnPlanets spawnPlanets;
    private Planet activePlanet = null;
    private Planet oldActivePlanet = null;
    private Planet[] planets;
    private GameObject sun;
    private GameObject planetsParent;
    [SerializeField] private GameObject player;
    private bool rotate;
    private bool setUpSolarSystemRotation;
    private KeplerOrbitMover sunKeplerOrbitMover;
    private Vector3 rotationAxis;


    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }
        planetsParent = this.gameObject;
        planets = planetsParent.GetComponentsInChildren<Planet>();
    }

    void Update()
    {
        if (sun == null && spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }
        if (!spawnPlanets.solarySystemGenerated)
        {
            return;
        }

        UpdateClosestPlanet();

        // If the player is not on any planet, reset the solar system
        if (activePlanet != oldActivePlanet && activePlanet == null)
        {
            oldActivePlanet.ResetMoons();
            ResetPlanetOrbit(oldActivePlanet.gameObject);
            oldActivePlanet = activePlanet;
        }
        // If the player has entered a new planet, move the solar system accordingly
        else if (activePlanet != oldActivePlanet)
        {
            MovePlanets();
            activePlanet.rotateMoons = true;
            oldActivePlanet = activePlanet;
        }
        Universe.player.Planet = activePlanet;
    }

    private void FixedUpdate()
    {
        if (rotate)
        {
            SetUpRotation();
            RotateSolarSystem();
        }
    }
    // Setup components for system rotation
    private void SetUpRotation()
    {
        if (setUpSolarSystemRotation) return;

        sunKeplerOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        rotationAxis = activePlanet.rotationAxis;
        sunKeplerOrbitMover.LockOrbitEditing = false;
        setUpSolarSystemRotation = true;
    }
    

    private void RotateSolarSystem()
    {
        
    }

    private void UpdateClosestPlanet()
    {
        // Loops over all planets and checks if the player is on it or has left it
        foreach (Planet planet in planets)
        {
            if (planet.bodyName.Contains("Moon")) continue;
            
            float distance =  (player.transform.position - planet.transform.position).magnitude;
            if (distance <= (planet.radius + 300f) && planet != activePlanet)
            {
                activePlanet = planet;
                player.transform.parent = activePlanet.transform;
                break;
            }
            if(planet == activePlanet && distance > (planet.radius + 500f))
            {
                activePlanet = null;
                break;
            }
        }
    }

    private void ResetPlanetOrbit(GameObject planet)
    {
        TurnOnOrbit(planet.gameObject);

        //Turn of orbitig on the sun
        KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        sunOrbitMover.enabled = false;

        // Center the solar system at origo again and remove player as a child of planet
        planetsParent.transform.position = Vector3.zero;
        player.transform.parent = null;

    }

    private void MovePlanets()
    {
        activePlanet.gameObject.GetComponent<KeplerOrbitMover>().enabled = false;

        // Calculate the distance from the planet that should be centered and origo
        // Move the solar system by that distance to place planet in origo
        Vector3 distanceFromOrigin = activePlanet.transform.GetChild(0).transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;

        // Activate orbit on the sun to fake the movement of the planet
        ActivateSunOrbit(activePlanet.gameObject);
        //rotate = true;
    }

    private void ActivateSunOrbit(GameObject planetToOrbit)
    {
        // Activate orbit with attraction to planet
        KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        sunOrbitMover.AttractorSettings.AttractorObject = planetToOrbit.transform;

        // AttractorMass is set to be the same mass as the sun to get the same velocity the planet had.
        sunOrbitMover.AttractorSettings.AttractorMass = sun.GetComponent<Sun>().mass;
        sunOrbitMover.AttractorSettings.GravityConstant = 2;
        TurnOnOrbit(sun);

        // Not nessecarry, used for debug. Sets up orbit display
        KeplerOrbitLineDisplay sunOrbitDisplay = sun.GetComponent<KeplerOrbitLineDisplay>();
        sunOrbitDisplay.MaxOrbitWorldUnitsDistance = (planetToOrbit.transform.position - sunOrbitMover.gameObject.transform.position).magnitude * 1.2f;
        sunOrbitDisplay.LineRendererReference = sunOrbitMover.gameObject.GetComponent<LineRenderer>();
        sunOrbitDisplay.enabled = true;
    }

    private void TurnOnOrbit(GameObject planet)
    {
        // Turns on orbit for the given planet
        KeplerOrbitMover orbitMover = planet.GetComponent<KeplerOrbitMover>();
        orbitMover.SetUp();
        orbitMover.SetAutoCircleOrbit();
        orbitMover.ForceUpdateOrbitData();
        orbitMover.enabled = true;
    }
}
