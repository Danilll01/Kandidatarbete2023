using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleKeplerOrbits;

public class SolarSystemTransform : MonoBehaviour
{
    [SerializeField] private SpawnPlanets spawnPlanets;
    [SerializeField] private int activePlanetIndex = 0;
    private GameObject sun;
    private GameObject planetsParent;
    [SerializeField] private GameObject player;
    private int playerOnPlanetIndex = 0;
    [SerializeField] private bool rotateSolarSystem = false;
    private GameObject fakeOrbitObject;


    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }
        planetsParent = this.gameObject;
        fakeOrbitObject = new GameObject("fake orbit object");
        fakeOrbitObject.transform.parent = planetsParent.transform;
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

        CheckIfPlayerOnAnyPlanet();

        // If the player is not on any planet, reset the solar system
        if (playerOnPlanetIndex != activePlanetIndex && playerOnPlanetIndex == -1)
        {
            ResetPlanetOrbit(activePlanetIndex);
            activePlanetIndex = -1;
        }
        // If the player has entered a new planet, move the solar system accordingly
        else if (playerOnPlanetIndex != activePlanetIndex)
        {
            MovePlanets(playerOnPlanetIndex);
            activePlanetIndex = playerOnPlanetIndex;
        }
    }

    void LateUpdate()
    {
        if (rotateSolarSystem)
        {
            RotateSolarSystem();
        }
        /*
        else
        {
            RotateAroundAxis();
        }
        */
    }

    private void CheckIfPlayerOnAnyPlanet()
    {
        // Loops over all planets and checks if the player is on it or has left it
        for (int i = 0; i < spawnPlanets.bodies.Count; i++)
        {
            Planet planet = spawnPlanets.bodies[i];
            float distance = (player.transform.position - planet.transform.GetChild(0).position).magnitude;

            // Check if the player has entered a new planet
            if (distance <= (planet.diameter * 2) && i != activePlanetIndex)
            {
                player.transform.parent = planet.transform;
                playerOnPlanetIndex = i;
                break;
            }

            // Check if the player has left the cuurrent planet
            if (playerOnPlanetIndex >= 0 && i == playerOnPlanetIndex)
            {
                if (distance > (planet.diameter * 2))
                {
                    playerOnPlanetIndex = -1; // -1 means the player is not on any planet
                    break;
                }
            }
        }
    }

    private void RotateSolarSystem()
    {
        sun.GetComponent<KeplerOrbitMover>().LockOrbitEditing = false;
        sun.GetComponent<KeplerOrbitMover>().SetAutoCircleOrbit();
        Vector3 planetPosition = spawnPlanets.bodies[activePlanetIndex].transform.position;
        transform.RotateAround(planetPosition, -spawnPlanets.bodies[activePlanetIndex].GetComponent<Planet>().rotationAxis, Time.deltaTime);
    }

    private void ResetPlanetOrbit(int planetIndex)
    {
        if (planetIndex >= 0)
        {
            rotateSolarSystem = false;
            // Turn on orbit again for the planet the player left
            Planet planet = spawnPlanets.bodies[planetIndex];
            TurnOnOrbit(planet.gameObject);

            //Turn of orbitig on the sun
            KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
            sunOrbitMover.enabled = false;

            // Center the solar system at origo again and remove player as a child of planet
            sun.transform.position = Vector3.zero;
            planet.transform.parent = planetsParent.transform;
            player.transform.parent = null;
        }

    }

    private void MovePlanets(int planetIndex)
    {
        Planet planet = spawnPlanets.bodies[planetIndex];
        planet.gameObject.GetComponent<KeplerOrbitMover>().enabled = false;

        // Calculate the distance from the planet that should be centered and origo
        // Move the solar system by that distance to place planet in origo
        Vector3 distanceFromOrigin = planet.transform.GetChild(0).transform.position - Vector3.zero;
        sun.transform.position -= distanceFromOrigin;
        planet.gameObject.transform.parent = null;
        planet.gameObject.transform.position = Vector3.zero;
        fakeOrbitObject.transform.position = Vector3.zero;

        // Activate orbit on the sun to fake the movement of the planet
        ActivateSunOrbit(planet.gameObject);
        rotateSolarSystem = true;
    }

    private void RotateAroundAxis()
    {
        spawnPlanets.bodies[activePlanetIndex].transform.RotateAround(spawnPlanets.bodies[activePlanetIndex].transform.position, spawnPlanets.bodies[activePlanetIndex].GetComponent<Planet>().rotationAxis, 1f * Time.deltaTime);
    }

    private void ActivateSunOrbit(GameObject planetToOrbit)
    {
        // Activate orbit with attraction to planet
        KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        sunOrbitMover.AttractorSettings.AttractorObject = fakeOrbitObject.transform;

        // AttractorMass is set to be the same mass as the sun to get the same velocity the planet had.
        sunOrbitMover.AttractorSettings.AttractorMass = sun.GetComponent<Sun>().mass;
        sunOrbitMover.AttractorSettings.GravityConstant = 2;
        TurnOnOrbit(sun);

        // Not nessecarry, used for debug. Sets up orbit display
        KeplerOrbitLineDisplay sunOrbitDisplay = sun.GetComponent<KeplerOrbitLineDisplay>();
        sunOrbitDisplay.MaxOrbitWorldUnitsDistance = (fakeOrbitObject.transform.position - sunOrbitMover.gameObject.transform.position).magnitude * 1.2f;
        sunOrbitDisplay.LineRendererReference = sunOrbitMover.gameObject.GetComponent<LineRenderer>();
        sunOrbitDisplay.enabled = true;
    }

    private void TurnOnOrbit(GameObject planet)
    {
        // Turns on orbit for the given planet
        KeplerOrbitMover orbitMover = planet.GetComponent<KeplerOrbitMover>();
        orbitMover.LockOrbitEditing = false;
        orbitMover.SetUp();
        orbitMover.SetAutoCircleOrbit();
        orbitMover.ForceUpdateOrbitData();
        orbitMover.enabled = true;
        orbitMover.LockOrbitEditing = true;
    }
}
