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
    private float rotationSpeed;
    private float orbitSpeed;
    private bool releasePlayer = false;
    public bool movePlanets;
    public bool resetPlanetOrbit;
    private bool started;

    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }
        planetsParent = this.gameObject;
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

        if (!releasePlayer)
        {
            UpdateClosestPlanet();
            HandleUpdatedActivePlanet();

            if (releasePlayer)
            {
                return;
            }

            if (rotateSolarSystem)
            {
                RotateSolarSystem();
            }
            else if(relativePlanetSunDistances != null)
            {
                foreach (var planetBody in spawnPlanets.bodies)
                {
                    planetBody.Run();
                }
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
        // Check if sun has moved to Vector3.zero
        if (sun.transform.position.magnitude <= 5f)
        {
            player.transform.SetParent(null, true);
            player.attractor = null;
            setUpSolarSystemRotation = false;
            releasePlayer = false;
        }
    }

    private void RotateSolarSystem()
    {
        SetUpRotation();

        sun.transform.RotateAround(Vector3.zero, Vector3.up, orbitSpeed * Time.deltaTime);

        planetsParent.transform.RotateAround(Vector3.zero, -rotationAxis, rotationSpeed * Time.deltaTime);

        int activePlanetIndex = spawnPlanets.bodies.IndexOf(activePlanet);
        Vector3 direction = sun.transform.position - Vector3.zero;
        sun.transform.position = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;

        sun.transform.position = ClosestPointOnPlane(Vector3.zero, sun.transform.TransformDirection(Vector3.up), sun.transform.position);

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
            ResetPlanetOrbit();
            oldActivePlanet.rotateMoons = false;
            releasePlayer = true;
            oldActivePlanet = activePlanet;
        }
        // If the player has entered a new planet, move the solar system accordingly
        if (activePlanet != oldActivePlanet)
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
        rotationSpeed = activePlanet.rotationSpeed;
        orbitSpeed = activePlanet.orbitSpeed;

        foreach (Planet planet in spawnPlanets.bodies)
        {
            planet.HandleSolarSystemOrbit(rotationAxis, rotationSpeed);
        }

        setUpSolarSystemRotation = true;
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

    private void ResetPlanetOrbit()
    {
        foreach (Planet body in spawnPlanets.bodies)
        {
            body.transform.parent.SetParent(sun.transform);
        }
        
        sun.transform.position = Vector3.zero;
        sun.transform.rotation = Quaternion.Euler(0, sun.transform.rotation.y, 0);
        
        foreach (Planet body in spawnPlanets.bodies)
        {
            body.transform.parent.SetParent(planetsParent.transform);
            body.ResetOrbitComponents();
        }
    }

    private void MovePlanets()
    {
        // Calculate the distance from the planet that should be centered at origo
        // Move the solar system by that distance to place planet in origo
        Transform planetTransform = activePlanet.transform;
        Vector3 distanceFromOrigin = planetTransform.transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;
        planetTransform.parent.SetParent(null, true);

        player.attractor = activePlanet;

        rotateSolarSystem = true;
    }
}
