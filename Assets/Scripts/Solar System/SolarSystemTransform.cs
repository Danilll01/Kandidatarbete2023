using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleKeplerOrbits;

public class SolarSystemTransform : MonoBehaviour
{
    public int activePlanetIndex = 1;
    [SerializeField] private SpawnPlanets spawnPlanets;
    private GameObject sun;
    private GameObject planetsParent;
    [SerializeField] private GameObject player;
    private float timeToReachTarget = 5f;
    private float t = 0;
    private int playerOnPlanetIndex = 1;

    // For testing
    public bool movePlayerToNextPlanet;
    public bool resetOrbit;

    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.bodies[0].gameObject;
        }
        planetsParent = this.gameObject;
    }


    // Update is called once per frame
    void Update()
    {
        if (sun == null && spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.bodies[0].gameObject;
        }

        if (resetOrbit)
        {
            ResetPlanetOrbit(activePlanetIndex);
        }

        // For testing
        int nextPlanetIndex = 0;
        if (movePlayerToNextPlanet)
        {
            nextPlanetIndex = (activePlanetIndex + 1) % Universe.nrOfPlanets;
            if (nextPlanetIndex == 0) nextPlanetIndex++;
            Planet nextplanet = spawnPlanets.bodies[nextPlanetIndex];
            t += Time.deltaTime / timeToReachTarget;
            player.transform.position = Vector3.Lerp(player.transform.position, nextplanet.transform.GetChild(0).position, t);
        }

        if (t == timeToReachTarget)
        {
            movePlayerToNextPlanet = false;
            t = 0;
        }

        for (int i = 1; i < spawnPlanets.bodies.Count; i++)
        {
            Planet planet = spawnPlanets.bodies[i];
            float distance = (player.transform.position - planet.transform.GetChild(0).position).magnitude;
            
            if (distance <= (planet.radius * 2) && i != activePlanetIndex)
            {
                player.transform.parent = planet.transform;
                playerOnPlanetIndex = i;
                break;
            }

            if (playerOnPlanetIndex >= 0 && i == playerOnPlanetIndex)
            {
                if (distance > (planet.radius * 2))
                {
                    playerOnPlanetIndex = -1;
                    break;
                }
            }
        }

        if (playerOnPlanetIndex != activePlanetIndex && playerOnPlanetIndex == -1)
        {
            ResetPlanetOrbit(activePlanetIndex);
            activePlanetIndex = -1;
        }
        else if (playerOnPlanetIndex != activePlanetIndex)
        {
            MovePlanets(playerOnPlanetIndex);
            activePlanetIndex = playerOnPlanetIndex;
        }

        /*
        if (movePlayerToNextPlanet && t == timeToReachTarget)
        {
            movePlayerToNextPlanet = false;
            activePlanetIndex = nextPlanetIndex;
        }
        if (previousMovedPlanet != activePlanetIndex)
        {
            MovePlanets(activePlanetIndex);
            previousMovedPlanet = activePlanetIndex;
        }
        */
    }

    private void ResetPlanetOrbit(int planetIndex)
    {
        if (planetIndex >= 0)
        {
            Planet planet = spawnPlanets.bodies[planetIndex];
            TurnOnOrbit(planet.gameObject);

            KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
            sunOrbitMover.enabled = false;

            planetsParent.transform.position = Vector3.zero;
            player.transform.parent = null;
        }

    }

    private void MovePlanets(int planetIndex)
    {
        Planet planet = spawnPlanets.bodies[planetIndex];
        planet.gameObject.GetComponent<KeplerOrbitMover>().enabled = false;
        Vector3 distanceFromOrigin = planet.transform.GetChild(0).transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;
        ActivateSunOrbit(planet.gameObject);
    }

    private void ActivateSunOrbit(GameObject planetToOrbit)
    {
        KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        sunOrbitMover.AttractorSettings.AttractorObject = planetToOrbit.transform;
        sunOrbitMover.AttractorSettings.AttractorMass = sun.GetComponent<Planet>().mass;
        sunOrbitMover.AttractorSettings.GravityConstant = 2;
        TurnOnOrbit(sun);

        // Not nessecarry, used for debug
        KeplerOrbitLineDisplay sunOrbitDisplay = sun.GetComponent<KeplerOrbitLineDisplay>();
        sunOrbitDisplay.MaxOrbitWorldUnitsDistance = (planetToOrbit.transform.position - sunOrbitMover.gameObject.transform.position).magnitude * 1.2f;
        sunOrbitDisplay.LineRendererReference = sunOrbitMover.gameObject.GetComponent<LineRenderer>();

        sunOrbitDisplay.enabled = true;
    }

    private void TurnOnOrbit(GameObject planet)
    {
        KeplerOrbitMover orbitMover = planet.GetComponent<KeplerOrbitMover>();
        orbitMover.SetUp();
        orbitMover.SetAutoCircleOrbit();
        orbitMover.ForceUpdateOrbitData();
        orbitMover.enabled = true;
    }
}
