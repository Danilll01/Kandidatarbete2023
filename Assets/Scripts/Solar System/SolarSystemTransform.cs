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
    private Vector3[] relativePlanetSunDistances;
    private Vector3 rotationAxis;
    private float rotationspeed;
    private float orbitSpeed;
    private Vector3 startingPos;
    private Quaternion startingRotation;
    private bool releasePlayer = false;
    private Planet planetToReleasePlayerFrom;
    private Quaternion planetsParentRotation;
    private Vector3 directionToPlanetBeforeReset;
    private GameObject fakeOrbitObject;
    private float angleForHeightDiff;
    private Vector3 planetToSunDirection;
    private Quaternion universeRotationBeforeReset;
    private Quaternion rotationBefore;
    private float heightDiffFromSunToOrigo;
    public bool stopSolarSystem;
    public bool resetSolarSystem;
    private bool reset;
    public bool testResetOnYPlane;

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

    private void CheckWhenToReleasePlayer()
    {
        Vector3 distance = planetToReleasePlayerFrom.transform.parent.position;
        if (distance.magnitude > 100f)
        {
            ResetPlanets();
            //player.transform.SetParent(null, true);
            //player.attractor = null;
            releasePlayer = false;
        }
    }

    private void FixedUpdate()
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

        if (!stopSolarSystem)
        {
            if (!releasePlayer)
            {
                UpdateClosestPlanet();
                HandleUpdatedActivePlanet();
            }
            else
            {
                CheckWhenToReleasePlayer();
            }

            Universe.player.Planet = activePlanet;

            if (releasePlayer)
            {
                CheckWhenToReleasePlayer();
            }

            if (releasePlayer) return;

            if (rotateSolarSystem)
            {
                RotateSolarSystem();
            }
            else
            {
                if (relativePlanetSunDistances != null)
                {
                    foreach (var planetBody in spawnPlanets.bodies)
                    {
                        planetBody.Run();
                    }
                }

            }
        }
        else if (resetSolarSystem && !reset)
        {
            rotateSolarSystem = false;
            planetToReleasePlayerFrom = oldActivePlanet;
            universeRotationBeforeReset = planetsParent.transform.rotation;
            directionToPlanetBeforeReset = sun.transform.position - oldActivePlanet.transform.position;
            foreach (var planetBody in spawnPlanets.bodies)
            {
                planetBody.SetUpResetComponents(universeRotationBeforeReset);
            }
            ResetPlanetOrbit(oldActivePlanet);
            releasePlayer = true;
            oldActivePlanet = activePlanet;
            reset = true;
        }

        
    }

    private void RotateSolarSystem()
    {
        SetUpRotation();

        sun.transform.RotateAround(Vector3.zero, Vector3.up, orbitSpeed * Time.deltaTime);

        planetsParent.transform.RotateAround(Vector3.zero, -rotationAxis, rotationspeed * Time.deltaTime);

        int activePlanetIndex = spawnPlanets.bodies.IndexOf(activePlanet);
        Vector3 direction = sun.transform.position - Vector3.zero;
        sun.transform.position = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;

        sun.transform.position = ClosestPointOnPlane(Vector3.zero, sun.transform.TransformDirection(Vector3.up), sun.transform.position);

        sun.GetComponent<Sun>().distanceToAttractor = (sun.transform.position - Vector3.zero).magnitude;

        foreach (var planetBody in spawnPlanets.bodies)
        {
            planetBody.Run();
        }
    }

    private void HandleUpdatedActivePlanet()
    {
        // If the player is not on any planet, reset the solar system
        if (activePlanet != oldActivePlanet && activePlanet == null)
        {
            rotateSolarSystem = false;
            planetToReleasePlayerFrom = oldActivePlanet;
            universeRotationBeforeReset = planetsParent.transform.rotation;
            directionToPlanetBeforeReset = sun.transform.position - oldActivePlanet.transform.position;
            foreach (var planetBody in spawnPlanets.bodies)
            {
                planetBody.SetUpResetComponents(universeRotationBeforeReset);
            }
            ResetPlanetOrbit(oldActivePlanet);
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

    private void OnDrawGizmos()
    {
        if(!spawnPlanets.solarSystemGenerated)
        {
            return;
        }
        float radius = (sun.transform.position - Vector3.zero).magnitude;
        Universe.DrawGizmosCircle(Vector3.zero, sun.transform.up, radius, 32);

        if (stopSolarSystem)
        {
            float angleBetweenSunUpAndTrueUp = Vector3.Angle(sun.transform.position + Vector3.up, sun.transform.up);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(sun.transform.position, sun.transform.position + Vector3.up * 10000f);



            foreach (var body in spawnPlanets.bodies)
            {
                Vector3 directionToSunBeforeReset = body.transform.position - Universe.sunPosition.position;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(sun.transform.position, sun.transform.position + directionToSunBeforeReset);
            }
            
            for (int i = 0; i < spawnPlanets.bodies.Count; i++)
            {
                Planet planet = spawnPlanets.bodies[i];
                Vector3 directionToSunBeforeReset =  planet.transform.parent.position - Universe.sunPosition.position;
                Vector3 directionToClosestPointOnYPlane = ClosestPointOnPlane(sun.transform.position, Vector3.up, directionToSunBeforeReset);
                
                Vector3 newPos = sun.transform.position + directionToClosestPointOnYPlane;
                newPos = new Vector3(newPos.x, sun.transform.position.y, newPos.z);

                Vector3 newPosDirection = newPos - sun.transform.position;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(sun.transform.position, sun.transform.position + newPosDirection.normalized * relativePlanetSunDistances[i].magnitude);

            }
        }
    }

    private Vector3 ClosestPointOnPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return point + DistanceFromPlane(planeOffset, planeNormal, point) * planeNormal;
    }

    private float DistanceFromPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return Vector3.Dot(planeOffset - point, planeNormal);
    }

    // Setup components for solar system rotation
    private void SetUpRotation()
    {
        if (setUpSolarSystemRotation) return;

        rotationAxis = activePlanet.rotationAxis;
        rotationspeed = activePlanet.rotationSpeed;
        orbitSpeed = activePlanet.orbitSpeed;
        startingPos = planetsParent.transform.position;
        startingRotation = planetsParent.transform.rotation;

        for (int i = 0; i < spawnPlanets.bodies.Count; i++)
        {
            Planet planet = spawnPlanets.bodies[i];
            planet.HandleSolarSystemOrbit(rotationAxis, rotationspeed);
        }

        setUpSolarSystemRotation = true;
    }

    private void ResetPlanets()
    {
        for (int i = 0; i < spawnPlanets.bodies.Count; i++)
        {
            Planet planet = spawnPlanets.bodies[i];
            planet.solarSystemRotationActive = false;
        }

        setUpSolarSystemRotation = false;
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

        for (int i = 0; i < spawnPlanets.bodies.Count; i++)
        {
            Planet body = spawnPlanets.bodies[i];
            body.transform.parent.SetParent(sun.transform);
        }
        sun.transform.rotation = Quaternion.Euler(0, sun.transform.rotation.y, 0);
        sun.transform.position = Vector3.zero;
        
        for (int i = 0; i < spawnPlanets.bodies.Count; i++)
        {
            Planet body = spawnPlanets.bodies[i];
            body.transform.parent.SetParent(planetsParent.transform);
            body.ResetPlanetAndMoons();
        }
        
        
        /*
        Transform planetTransform = planet.transform;
        planetsParent.transform.position = startingPos;

        Quaternion rotation = planetsParent.transform.rotation;
        directionToPlanetBeforeReset = Quaternion.Inverse(rotation) * directionToPlanetBeforeReset;
        Vector3 directionAtZeroY = directionToPlanetBeforeReset;
        directionAtZeroY.y = 0;
        directionToPlanetBeforeReset = Quaternion.FromToRotation(directionToPlanetBeforeReset, directionAtZeroY) * directionToPlanetBeforeReset;


        planetsParent.transform.rotation = Quaternion.Euler(startingRotation.x, sun.transform.rotation.y, startingRotation.z);
        rotationBefore = sun.transform.rotation;
        rotationBefore = Quaternion.Euler(sun.transform.rotation.x, 0, sun.transform.rotation.z);

        //sun.transform.rotation = Quaternion.identity;
        heightDiffFromSunToOrigo = sun.transform.position.y;
        planetsParent.transform.position -= new Vector3(0, heightDiffFromSunToOrigo, 0);

        foreach (var planetBody in spawnPlanets.bodies)
        {
            planetBody.ResetPlanetAndMoons();
        }

        //sun.transform.position = planetTransform.transform.position + directionToPlanetBeforeReset;
        planetTransform.parent.SetParent(planetsParent.transform, true);

        // Place the sun back at origo
        Vector3 distanceFromOrigin = sun.transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;

        planetTransform.rotation *= Quaternion.Inverse(universeRotationBeforeReset);


        planetTransform.rotation *= Quaternion.Inverse(rotationBefore);

        Vector3 planetToSunWithHeight = sun.transform.position + new Vector3(0, heightDiffFromSunToOrigo, 0) - planetTransform.position;
        Vector3 planetToSunWithoutHeight = sun.transform.position - planetTransform.position;
        //float angle = Vector3.Angle(planetTransform.position + new Vector3(0, heightDiffFromSunToOrigo, 0), planetTransform.position);
        planetTransform.rotation *= Quaternion.FromToRotation(planetToSunWithHeight, planetToSunWithoutHeight);
        */
    }

    private void MovePlanets()
    {
        // Calculate the distance from the planet that should be centered at origo
        // Move the solar system by that distance to place planet in origo
        Transform planetTransform = activePlanet.transform;
        Vector3 distanceFromOrigin = planetTransform.transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;
        planetTransform.parent.parent = null;
        fakeOrbitObject.transform.position = Vector3.zero;
        
        player.attractor = activePlanet;

        rotateSolarSystem = true;
    }
}
