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
    private int playerOnPlanetIndex;
    private bool rotateSolarSystem;
    private Quaternion playerRotation;
    private GameObject fakeOrbitObject;
    private Vector3[] relativePlanetSunDistances;
    private bool valuesInitialized = false;
    
    private bool releasePlayer = false;
    private int planetIndexToReleasePlayerFrom;
    private bool setUpSolarSystemRotation = false;
    private KeplerOrbitMover sunKeplerOrbitMover;
    private Vector3 rotationAxis;

    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }
        planetsParent = this.gameObject;
        
        // Create a fake orbit object the sun can orbit around while solar system is rotating
        fakeOrbitObject = new GameObject("fake orbit object")
        {
            transform =
            {
                parent = planetsParent.transform
            }
        };
    }
    
    void Update()
    {
        if (!spawnPlanets.solarySystemGenerated)
        {
            return;
        }
        
        if (!valuesInitialized)
        {
            InitializeValues();
            valuesInitialized = true;
        }
        
        if (!releasePlayer)
        {
            CheckIfPlayerOnAnyPlanet();

            // If the player is not on any planet, reset the solar system
            if (playerOnPlanetIndex != activePlanetIndex && playerOnPlanetIndex == -1)
            {
                ResetPlanetOrbit(activePlanetIndex);
                releasePlayer = true;
                planetIndexToReleasePlayerFrom = activePlanetIndex;
                activePlanetIndex = playerOnPlanetIndex;
                setUpSolarSystemRotation = true;
            }
            // If the player has entered a new planet, move the solar system accordingly
            else if (playerOnPlanetIndex != activePlanetIndex)
            {
                MovePlanets(playerOnPlanetIndex);
                activePlanetIndex = playerOnPlanetIndex;
            }
        }
        
        // Check when to release the player when resetting the solar system
        if (releasePlayer)
        {
            CheckWhenToReleasePlayer();
        }
    }

    // Make sure that the planet has moved away from the center before releasing the player
    private void CheckWhenToReleasePlayer()
    {
        Vector3 distance = spawnPlanets.bodies[planetIndexToReleasePlayerFrom].transform.position;
        if (distance.magnitude > 100f)
        {
            player.transform.SetParent(null,true);
            releasePlayer = false;
        }
    }

    // Initialize values for distances between planets and sun
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
            SetUpRotation();
            RotateSolarSystem();
        }
    }

    // Setup components for system rotation
    private void SetUpRotation()
    {
        if (setUpSolarSystemRotation) return;

        sunKeplerOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        rotationAxis = spawnPlanets.bodies[activePlanetIndex].GetComponent<Planet>().rotationAxis;
        sunKeplerOrbitMover.LockOrbitEditing = false;
        setUpSolarSystemRotation = true;
    }

    private void CheckIfPlayerOnAnyPlanet()
    {
        Transform playerTransform = player.transform;
        
        // Loops over all planets and checks if the player is on it or has left it
        for (int i = 0; i < spawnPlanets.bodies.Count; i++)
        {
            Planet planet = spawnPlanets.bodies[i];
            
            // Turned of the check for moons for now. This will be fixed in another task
            if (!planet.bodyName.Contains("Moon"))
            {
                float distance = (playerTransform.position - planet.transform.GetChild(0).position).magnitude;

                // Check if the player has entered a new planet
                if (distance <= (planet.radius + 300f) && i != activePlanetIndex)
                {
                    playerTransform.parent = planet.transform;
                    playerOnPlanetIndex = i;
                    break;
                }

                // Check if the player has left the current planet
                if (playerOnPlanetIndex >= 0 && i == playerOnPlanetIndex)
                {
                    if (distance > (planet.radius + 400f))
                    {
                        playerOnPlanetIndex = -1; // -1 means the player is not on any planet
                        break;
                    }
                }
            }
            
        }
    }

    // Rotate the solar system around the active planet
    private void RotateSolarSystem()
    {
        // Need to adjust the solar system at every step to prevent floating point errors that makes
        // the sun slowly go further and further away
        sunKeplerOrbitMover.SetAutoCircleOrbit();
        Vector3 planetPosition = spawnPlanets.bodies[activePlanetIndex].transform.position;
        Vector3 direction = sun.transform.position - planetPosition;
        
        transform.RotateAround(planetPosition, -rotationAxis, Time.deltaTime);
        sun.transform.position = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;
    }

    private void ResetPlanetOrbit(int planetIndex)
    {
        if (planetIndex >= 0)
        {
            rotateSolarSystem = false;
            Planet planet = spawnPlanets.bodies[planetIndex];
            Transform planetTransform = planet.transform;
            KeplerOrbitMover planetOrbitMover = planet.GetComponent<KeplerOrbitMover>();

            //Turn off orbiting on the sun
            KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
            sunOrbitMover.enabled = false;

            // Set the rotation and position of planet depending on sun position

            Vector3 sunToPlanetDir = sun.transform.position - planetTransform.position;
            Vector3 sunToPlanetRelativeDir = sunToPlanetDir;
            sunToPlanetRelativeDir.y = 0;
            float angleBetweenPlanetAndSun = Vector3.Angle(sunToPlanetDir, sunToPlanetRelativeDir);
            planetTransform.rotation = Quaternion.AngleAxis(angleBetweenPlanetAndSun, rotationAxis);
            planet.ResetMoons();
            Vector3 direction = sun.transform.position - planetTransform.position;
            direction.y = 0;
            planetTransform.position = -direction.normalized * relativePlanetSunDistances[planetIndex].magnitude;
            planetOrbitMover.VelocityHandle.localPosition = new Vector3(100, 0, 0);

            // Reset the sun at the systems origo
            sun.transform.position = Vector3.zero;

            // Turn on orbit again for the planet the player left
            TurnOnOrbit(planetOrbitMover);


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
        Transform planetTransform;
        Vector3 distanceFromOrigin = (planetTransform = planet.transform).GetChild(0).transform.position - Vector3.zero;
        sun.transform.position -= distanceFromOrigin;
        planetTransform.parent = null;
        planetTransform.position = Vector3.zero;
        fakeOrbitObject.transform.position = Vector3.zero;

        // Activate orbit on the sun to fake the movement of the planet
        ActivateSunOrbit(planet.gameObject);
        rotateSolarSystem = true;
    }

    private void ActivateSunOrbit(GameObject planetToOrbit)
    {
        // Activate orbit with attraction to planet
        KeplerOrbitMover sunOrbitMover = sun.GetComponent<KeplerOrbitMover>();
        sunOrbitMover.AttractorSettings.AttractorObject = fakeOrbitObject.transform;

        // AttractorMass is set to be the same mass as the sun to get the same velocity the planet had.
        sunOrbitMover.AttractorSettings.AttractorMass = sun.GetComponent<Sun>().mass;
        sunOrbitMover.AttractorSettings.GravityConstant = 2;
        TurnOnOrbit(sunOrbitMover);

        // Not necessary, used for debug. Sets up orbit display
        KeplerOrbitLineDisplay sunOrbitDisplay = sun.GetComponent<KeplerOrbitLineDisplay>();
        sunOrbitDisplay.MaxOrbitWorldUnitsDistance = (fakeOrbitObject.transform.position - sunOrbitMover.gameObject.transform.position).magnitude * 1.2f;
        sunOrbitDisplay.LineRendererReference = sunOrbitMover.gameObject.GetComponent<LineRenderer>();
        sunOrbitDisplay.enabled = true;
    }
    
    private void TurnOnOrbit(KeplerOrbitMover planetOrbitMover)
    {
        // Turns on orbit for the given planet
        planetOrbitMover.LockOrbitEditing = false;
        planetOrbitMover.SetUp();
        planetOrbitMover.SetAutoCircleOrbit();
        planetOrbitMover.ForceUpdateOrbitData();
        planetOrbitMover.enabled = true;
        planetOrbitMover.LockOrbitEditing = true;
    }
}
