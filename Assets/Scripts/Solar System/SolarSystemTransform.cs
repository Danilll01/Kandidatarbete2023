using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleKeplerOrbits;

public class SolarSystemTransform : MonoBehaviour
{
    [SerializeField] private SpawnPlanets spawnPlanets;
    [SerializeField] private int activePlanetIndex = 1;
    private GameObject sun;
    private GameObject planetsParent;
    [SerializeField] private GameObject player;
    private int playerOnPlanetIndex = 1;


    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.bodies[0].gameObject;
        }
        planetsParent = this.gameObject;
    }


    
    void Update()
    {
        if (sun == null && spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.bodies[0].gameObject;
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

    private void CheckIfPlayerOnAnyPlanet()
    {
        // Loops over all planets and checks if the player is on it or has left it
        for (int i = 1; i < spawnPlanets.bodies.Count; i++)
        {
            Planet planet = spawnPlanets.bodies[i];
            float distance = (player.transform.position - planet.transform.GetChild(0).position).magnitude;

            // Check if the player has entered a new planet
            if (distance <= (planet.radius * 2) && i != activePlanetIndex)
            {
                player.transform.parent = planet.transform;
                playerOnPlanetIndex = i;
                break;
            }

            // Check if the player has left the cuurrent planet
            if (playerOnPlanetIndex >= 0 && i == playerOnPlanetIndex)
            {
                if (distance > (planet.radius * 2))
                {
                    playerOnPlanetIndex = -1; // -1 means the player is not on any planet
                    break;
                }
            }
        }
    }

    private void ResetPlanetOrbit(int planetIndex)
    {
        if (planetIndex >= 0)
        {
            // Turn on orbit again for the planet the player left
            Planet planet = spawnPlanets.bodies[planetIndex];
            TurnOnOrbit(planet.gameObject);

            //Turn of orbitig on the sun
            KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
            sunOrbitMover.enabled = false;
            
            // Center the solar system at origo again and remove player as a child of planet
            planetsParent.transform.position = Vector3.zero;
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
        planetsParent.transform.position -= distanceFromOrigin;

        // Activate orbit on the sun to fake the movement of the planet
        ActivateSunOrbit(planet.gameObject);
    }

    private void ActivateSunOrbit(GameObject planetToOrbit)
    {
        // Activate orbit with attraction to planet
        KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        sunOrbitMover.AttractorSettings.AttractorObject = planetToOrbit.transform;

        // AttractorMass is set to be the same mass as the sun to get the same velocity the planet had.
        sunOrbitMover.AttractorSettings.AttractorMass = sun.GetComponent<Planet>().mass;
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
