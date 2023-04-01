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
    private GameObject sun;
    private GameObject planetsParent;
    [SerializeField] private PillPlayerController player;
    private bool rotateSolarSystem;
    private bool setUpSolarSystemRotation;
    private KeplerOrbitMover sunKeplerOrbitMover;
    private Vector3[] relativePlanetSunDistances;
    private Vector3 rotationAxis;
    private float rotationspeed;
    private GameObject fakeOrbitObject;
    
    private bool releasePlayer = false;
    private Planet planetToReleasePlayerFrom;


    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }
        
        planetsParent = this.gameObject;
        planetToReleasePlayerFrom = null;
        
        // Create a fake orbit object the sun can orbit around while solar system is rotating
        fakeOrbitObject = new GameObject("fake orbit object")
        {
            transform =
            {
                parent = planetsParent.transform
            }
        };
    }
    
    private void InitializeValues()
    {
        // Get the initial distances from the planets to the sun
        if (relativePlanetSunDistances == null)
        {
            relativePlanetSunDistances = new Vector3[spawnPlanets.bodies.Count];
            for (int i = 0; i < spawnPlanets.bodies.Count; i++)
            {
                Planet planet = spawnPlanets.bodies[i];
                relativePlanetSunDistances[i] = planet.transform.parent.position;
            }
        }
        Universe.player.Planet = activePlanet;
    }

    void Update()
    {
        if (sun == null && spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }
        if (!spawnPlanets.solarSystemGenerated)
        {
            return;
        }
        InitializeValues();

        if (!releasePlayer)
        {
            UpdateClosestPlanet();

            // If the player is not on any planet, reset the solar system
            if (activePlanet != oldActivePlanet && activePlanet == null)
            {
                rotateSolarSystem = false;
                ResetPlanetOrbit(oldActivePlanet);
                oldActivePlanet.ResetMoons();
                planetToReleasePlayerFrom = oldActivePlanet;
                releasePlayer = true;
                oldActivePlanet = activePlanet;
            }
            // If the player has entered a new planet, move the solar system accordingly
            else if (activePlanet != oldActivePlanet)
            {
                MovePlanets();
                activePlanet.rotateMoons = true;
                oldActivePlanet = activePlanet;
            }
        }
        else
        {
            CheckWhenToReleasePlayer();
        }
        
        Universe.player.Planet = activePlanet;
    }

    private void CheckWhenToReleasePlayer()
    {
        Vector3 distance = planetToReleasePlayerFrom.transform.parent.position;
        if (distance.magnitude > 100f)
        {
            player.transform.SetParent(null, true);
            releasePlayer = false;
            player.attractor = null;
            planetToReleasePlayerFrom.ResetMoons();
            ResetPlanets();
            TurnOnOrbit(planetToReleasePlayerFrom.transform.parent.gameObject);
        }
    }

    private void LateUpdate()
    {
        if (rotateSolarSystem)
        {
            SetUpRotation();
            RotateSolarSystem();
            
            int activePlanetIndex = spawnPlanets.bodies.IndexOf(activePlanet);
            Vector3 direction = sun.transform.position - fakeOrbitObject.transform.position;
            sun.transform.position = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;
        }
    }
    // Setup components for solar system rotation
    private void SetUpRotation()
    {
        if (setUpSolarSystemRotation) return;

        for (int i = 0; i < spawnPlanets.bodies.Count; i++)
        {
            Planet planet = spawnPlanets.bodies[i];
            planet.HandleSolarSystemOrbit();
        }
        rotationAxis = activePlanet.rotationAxis;
        rotationspeed = activePlanet.rotationSpeed;
        setUpSolarSystemRotation = true;
    }

    private void ResetPlanets()
    {
        for (int i = 0; i < spawnPlanets.bodies.Count; i++)
        {
            Planet planet = spawnPlanets.bodies[i];
            planet.solarSystemRotationActive = false;
            planet.ResetMoons();
            planet.transform.parent.GetComponent<KeplerOrbitMover>().SetAutoCircleOrbit();
            planet.transform.parent.GetComponent<KeplerOrbitMover>().LockOrbitEditing = true;
        }
        setUpSolarSystemRotation = false;
    }

    private void RotateSolarSystem()
    {
        planetsParent.transform.RotateAround(activePlanet.transform.position, -rotationAxis,  rotationspeed * Time.deltaTime);
    }

    private void UpdateClosestPlanet()
    {
        // Loops over all planets and checks if the player is on it or has left it
        foreach (Planet planet in spawnPlanets.bodies)
        {
            
            // We are not looking to land on the moon right now. Will be fixed later
            if (planet.bodyName.Contains("Moon")) continue;
            
            float distance =  (player.transform.position - planet.transform.position).magnitude;
            if (distance <= (planet.radius * 1.26) && planet != activePlanet)
            {
                activePlanet = planet;
                player.transform.parent = activePlanet.transform;
                break;
            }
            if(planet == activePlanet && distance > (planet.radius * 1.4))
            {
                activePlanet = null;
                break;
            }
        }
    }

    private void ResetPlanetOrbit(Planet planet)
    {
        Transform planetTransform = planet.transform;
        
        
        //Turn of orbiting on the sun
        KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        sunOrbitMover.enabled = false;

        // Reset solar system and planet rotations
        planetsParent.transform.rotation = Quaternion.identity;
        
        planetTransform.parent.SetParent(planetsParent.transform, true);
        planetTransform.rotation = Quaternion.Inverse(planet.moonsParent.transform.rotation);
        planet.transform.parent.GetComponent<KeplerOrbitMover>().VelocityHandle.localPosition = new Vector3(100, 0, 0);
        
        // Place the sun back at origo
        Vector3 distanceFromOrigin = sun.transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;
        planetTransform.parent.position = fakeOrbitObject.transform.position;
    }

    private void MovePlanets()
    {
        activePlanet.transform.parent.GetComponent<KeplerOrbitMover>().enabled = false;

        // Calculate the distance from the planet that should be centered at origo
        // Move the solar system by that distance to place planet in origo
        Transform planetTransform = activePlanet.transform;
        Vector3 distanceFromOrigin = planetTransform.transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;
        planetTransform.parent.parent = null;
        fakeOrbitObject.transform.position = Vector3.zero;
        
        player.attractor = activePlanet;

        // Activate orbit on the sun to fake the movement of the planet
        ActivateSunOrbit(fakeOrbitObject);
        rotateSolarSystem = true;
    }

    private void ActivateSunOrbit(GameObject planetToOrbit)
    {
        // Activate orbit with attraction to planet
        KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        sunOrbitMover.ResetOrbit();
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
        orbitMover.ResetOrbit();
        orbitMover.SetUp();
        orbitMover.ForceUpdateOrbitData();
        orbitMover.SetAutoCircleOrbit();
        orbitMover.enabled = true;
    }
}
