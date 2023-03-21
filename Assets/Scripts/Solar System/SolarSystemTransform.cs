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
    private bool rotateSolarSystem = false;
    private Quaternion playerRotation;
    private GameObject fakeOrbitObject;
    private Vector3[] relativePlanetSunDistances;

    public bool ResetSolarSystem = false;
    private bool reset = false;
    private bool releasePlayer = false;
    private Vector3 directionPlayerToPlanet;

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
        if (!ResetSolarSystem && !reset)
        {
            if (sun == null && spawnPlanets.bodies != null)
            {
                sun = spawnPlanets.sun;
            }
            if (!spawnPlanets.solarySystemGenerated)
            {
                return;
            }
            else
            {
                InitializeValues();
            }

            CheckIfPlayerOnAnyPlanet();

            // If the player is not on any planet, reset the solar system
            if (playerOnPlanetIndex != activePlanetIndex && playerOnPlanetIndex == -1)
            {
                Debug.Log("Reset");
                ResetPlanetOrbit(activePlanetIndex);
                activePlanetIndex = -1;
                reset = true;
            }
            // If the player has entered a new planet, move the solar system accordingly
            else if (playerOnPlanetIndex != activePlanetIndex)
            {
                MovePlanets(playerOnPlanetIndex);
                activePlanetIndex = playerOnPlanetIndex;
            }
        }
        else if(!reset)
        {
            ResetPlanetOrbit(activePlanetIndex);
            releasePlayer = true;
            reset = true;
            activePlanetIndex = -1;
        }
        else if (!ResetSolarSystem && reset)
        {
            playerOnPlanetIndex = 0;
            if (playerOnPlanetIndex != activePlanetIndex)
            {
                MovePlanets(playerOnPlanetIndex);
                activePlanetIndex = playerOnPlanetIndex;
            }
        }

        if (releasePlayer)
        {
            CheckWhenToReleasePlayer();
        }
    }

    private void CheckWhenToReleasePlayer()
    {
        Vector3 distance = spawnPlanets.bodies[playerOnPlanetIndex].transform.position;
        if (distance.magnitude > 100f)
        {
            player.transform.SetParent(null,true);
            player.transform.position = spawnPlanets.bodies[playerOnPlanetIndex].transform.position + directionPlayerToPlanet;
            player.transform.rotation = playerRotation;
            releasePlayer = false;
        }
    }

    private void InitializeValues()
    {
        if (relativePlanetSunDistances == null)
        {
            relativePlanetSunDistances = new Vector3[spawnPlanets.bodies.Count];
            for (int i = 0; i < spawnPlanets.bodies.Count; i++)
            {
                Planet planet = spawnPlanets.bodies[i];
                relativePlanetSunDistances[i] =  sun.transform.position - planet.transform.position;
            }
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
            if (!planet.bodyName.Contains("Moon"))
            {
                float distance = (player.transform.position - planet.transform.GetChild(0).position).magnitude;

                // Check if the player has entered a new planet
                if (distance <= (planet.diameter * 2) && i != activePlanetIndex)
                {
                    player.transform.parent = planet.transform;
                    playerOnPlanetIndex = i;
                    break;
                }

                // Check if the player has left the current planet
                if (playerOnPlanetIndex >= 0 && i == playerOnPlanetIndex)
                {
                    if (distance > (planet.diameter * 2.25f))
                    {
                        playerOnPlanetIndex = -1; // -1 means the player is not on any planet
                        break;
                    }
                }
            }
            
        }
    }

    private void RotateSolarSystem()
    {
        sun.GetComponent<KeplerOrbitMover>().LockOrbitEditing = false;
        sun.GetComponent<KeplerOrbitMover>().SetAutoCircleOrbit();
        Vector3 planetPosition = spawnPlanets.bodies[activePlanetIndex].transform.position;
        Vector3 direction = sun.transform.position - planetPosition;
        transform.RotateAround(planetPosition, -spawnPlanets.bodies[activePlanetIndex].GetComponent<Planet>().rotationAxis, Time.deltaTime);
        sun.transform.position = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;
    }

    private void ResetPlanetOrbit(int planetIndex)
    {
        if (planetIndex >= 0)
        {
            rotateSolarSystem = false;
            playerRotation = player.transform.rotation;
            Planet planet = spawnPlanets.bodies[planetIndex];
            directionPlayerToPlanet = planet.transform.position - player.transform.position;

            //Turn off orbitig on the sun
            KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
            sunOrbitMover.enabled = false;

            // Set the rotation and position of planet
            planet.ResetMoons();
            planet.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            Vector3 direction = sun.transform.position - planet.transform.position;
            direction.y = 0;
            planet.transform.position = -direction.normalized * relativePlanetSunDistances[planetIndex].magnitude;
            planet.gameObject.GetComponent<KeplerOrbitMover>().VelocityHandle.localPosition = new Vector3(100, 0, 0);

            sun.transform.position = Vector3.zero;

            
            // Turn on orbit again for the planet the player left
            TurnOnOrbit(planet.gameObject);


            // Center the solar system at origo again and remove player as a child of planet
            planet.transform.parent = planetsParent.transform;
            
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
