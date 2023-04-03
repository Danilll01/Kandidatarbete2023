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
    private Quaternion planetsParentRotation;
    private Vector3 playerToSunDirection;
    public bool resetSolarSystem;
    private bool reset;
    private bool updateDebugOrbits;
    private float timePassed;

    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }
        
        planetsParent = this.gameObject;
        planetToReleasePlayerFrom = null;
        timePassed = 0;
        
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

        if (!resetSolarSystem)
        {
            if (!releasePlayer)
            {
                UpdateClosestPlanet();

                // If the player is not on any planet, reset the solar system
                if (activePlanet != oldActivePlanet && activePlanet == null)
                {
                    rotateSolarSystem = false;
                    releasePlayer = true;
                    ResetPlanetOrbit(oldActivePlanet);
                    oldActivePlanet.ResetMoons();
                    planetToReleasePlayerFrom = oldActivePlanet;
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
        else if(!reset)
        {
            rotateSolarSystem = false;
            ResetPlanetOrbit(oldActivePlanet);
            oldActivePlanet.ResetMoons();
            planetToReleasePlayerFrom = oldActivePlanet;
            releasePlayer = true;
            oldActivePlanet = activePlanet;
            reset = true;
        }

        if (releasePlayer && reset)
        {
            CheckWhenToReleasePlayer();
        }
    }

    private void CheckWhenToReleasePlayer()
    {
        Vector3 distance = planetToReleasePlayerFrom.transform.parent.position;
        if (distance.magnitude > 100f)
        {
            planetToReleasePlayerFrom.ResetMoons();
            ResetPlanets();
            //player.transform.SetParent(null, true);
            //player.attractor = null;
            releasePlayer = false;
        }
    }

    private void FixedUpdate()
    {
        if (releasePlayer) return;

        timePassed += Time.deltaTime;
        updateDebugOrbits = false;

        if (updateDebugOrbits) timePassed = 0;

        if (rotateSolarSystem)
        {
            SetUpRotation();

            sun.transform.RotateAround(fakeOrbitObject.transform.position, Vector3.up, 2f * Time.deltaTime);
            
            planetsParent.transform.RotateAround(fakeOrbitObject.transform.position, -rotationAxis, rotationspeed * Time.deltaTime);

            int activePlanetIndex = spawnPlanets.bodies.IndexOf(activePlanet);
            Vector3 direction = sun.transform.position - fakeOrbitObject.transform.position;
            sun.transform.position = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;

            sun.transform.position = ClosestPointOnPlane(Vector3.zero, sun.transform.TransformDirection(Vector3.up), sun.transform.position);

            sun.GetComponent<Sun>().distanceToAttractor = (sun.transform.position - fakeOrbitObject.transform.position).magnitude;

            foreach (var planetBody in spawnPlanets.bodies)
            {
                planetBody.Run();
            }
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

    private void OnDrawGizmos()
    {
        if(!spawnPlanets.solarSystemGenerated)
        {
            return;
        }
        float radius = (sun.transform.position - fakeOrbitObject.transform.position).magnitude;
        Universe.DrawGizmosCircle(fakeOrbitObject.transform.position, sun.transform.up, radius, 32);
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
            Vector3 parentPos = planet.transform.parent.position;
            //planet.transform.parent.rotation = Quaternion.identity;
            planet.transform.parent.position = new Vector3(parentPos.x, 0, parentPos.z);
            planet.ResetMoons();
        }

        //planetToReleasePlayerFrom.transform.rotation = Quaternion.Inverse(planetsParentRotation);
        planetToReleasePlayerFrom.transform.rotation *= Quaternion.Inverse(planetsParentRotation);
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

        planetsParentRotation = planetsParent.transform.rotation;

        playerToSunDirection = player.transform.position - sun.transform.position;

        // Reset solar system and planet rotations
        planetsParent.transform.rotation = Quaternion.identity;


        //planetTransform.rotation = Quaternion.Inverse(planetRotation);
        planetTransform.parent.SetParent(planetsParent.transform, true);
        
        // Place the sun back at origo
        sun.transform.rotation = Quaternion.identity;
        Vector3 distanceFromOrigin = sun.transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;
        planetTransform.parent.position = fakeOrbitObject.transform.position;
        //planetTransform.parent.rotation = Quaternion.identity;
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
