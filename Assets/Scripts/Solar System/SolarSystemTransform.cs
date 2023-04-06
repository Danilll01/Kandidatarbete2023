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
    private Vector3 startingPos;
    private Quaternion startingRotation;
    private bool releasePlayer = false;
    private Planet planetToReleasePlayerFrom;
    private Quaternion planetsParentRotation;
    public bool stopSolarSystem;
    private bool reset;
    private GameObject fakeOrbitObject;
    private float angleForHeightDiff;
    private Vector3 planetToSunDirection;
    private Transform planetTransform;
    private Quaternion planetRotationBefore;
    public bool resetSolarSystemPosAndRotation;
    private Quaternion rotationBefore;
    public bool resetSunRotation;
    public bool placeSunAtOrigo;
    public bool rotatePlanetWithSolarSystemRotationBefore;
    public bool rotatePlanetWithSunRotationBefore;
    private float heightDiffFromSunToOrigo;
    public bool rotateToFixHeightChangeOfSun;

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
            planetToReleasePlayerFrom.ResetMoons();
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
        else if (!reset)
        {
            /*
            rotateSolarSystem = false;
            ResetPlanetOrbit(oldActivePlanet);
            oldActivePlanet.ResetMoons();
            planetToReleasePlayerFrom = oldActivePlanet;
            releasePlayer = true;
            oldActivePlanet = activePlanet;
            */
            rotateSolarSystem = false;
            planetTransform = activePlanet.transform;
            planetRotationBefore = planetsParent.transform.rotation;
            reset = true;
        }

        if (releasePlayer && reset)
        {
            CheckWhenToReleasePlayer();
        }

        if (releasePlayer) return;

        if (rotateSolarSystem)
        {
            SetUpRotation();

            sun.transform.RotateAround(Vector3.zero, Vector3.up, 2f * Time.deltaTime);
            
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
        /*
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
        */

        if (!rotateSolarSystem && reset && stopSolarSystem)
        {
            if (resetSolarSystemPosAndRotation)
            {
                Debug.Log("Reset solar system to original position and rotation");
                planetsParent.transform.position = startingPos;
                planetsParent.transform.rotation = startingRotation;
                rotationBefore = sun.transform.rotation;
                rotationBefore = Quaternion.Euler(sun.transform.rotation.x, 0, sun.transform.rotation.z);
                resetSolarSystemPosAndRotation = false;
            }



            if (resetSunRotation)
            {
                Debug.Log("Reset sun rotation");
                sun.transform.rotation = Quaternion.identity;
                foreach (var planetBody in spawnPlanets.bodies)
                {
                    planetBody.Run();
                }
                heightDiffFromSunToOrigo = sun.transform.position.y;
                planetsParent.transform.position -= new Vector3(0, heightDiffFromSunToOrigo, 0);
                planetTransform.parent.SetParent(planetsParent.transform, true);
                resetSunRotation = false;

            }

            if (placeSunAtOrigo)
            {
                Debug.Log("Moved solar system to place sun at origo");
                

                // Place the sun back at origo
                Vector3 distanceFromOrigin = sun.transform.position - Vector3.zero;
                planetsParent.transform.position -= distanceFromOrigin;
                placeSunAtOrigo = false;
            }


            if (rotatePlanetWithSolarSystemRotationBefore)
            {
                Debug.Log("Rotate planet by the earlier solar system rotation: " + planetRotationBefore);
                planetTransform.rotation *= Quaternion.Inverse(planetRotationBefore);
                rotatePlanetWithSolarSystemRotationBefore = false;
            }

            if (rotatePlanetWithSunRotationBefore)
            {
                Debug.Log("Rotate planet by the earlier sun rotation: " + rotationBefore);
                planetTransform.rotation *= Quaternion.Inverse(rotationBefore);
                rotatePlanetWithSunRotationBefore = false;
            }

            if (rotateToFixHeightChangeOfSun)
            {
                Vector3 planetToSunWithHeight = sun.transform.position + new Vector3(0, heightDiffFromSunToOrigo, 0) - planetTransform.position;
                Vector3 planetToSunWithoutHeight = sun.transform.position - planetTransform.position;
                //float angle = Vector3.Angle(planetTransform.position + new Vector3(0, heightDiffFromSunToOrigo, 0), planetTransform.position);
                planetTransform.rotation *= Quaternion.FromToRotation(planetToSunWithHeight, planetToSunWithoutHeight);
                rotateToFixHeightChangeOfSun = false;
            }
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
        rotationspeed = activePlanet.rotationSpeed;
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
            Vector3 parentPos = planet.transform.parent.position;
            //planet.transform.parent.rotation = Quaternion.identity;
            //planet.transform.parent.position = new Vector3(parentPos.x, 0, parentPos.z);
            planet.ResetMoons();
        }

        //planetToReleasePlayerFrom.transform.rotation = Quaternion.Inverse(planetsParentRotation);
        //planetToReleasePlayerFrom.transform.rotation *= Quaternion.Inverse(planetsParentRotation);
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

        Quaternion planetRotationBefore = planetsParent.transform.rotation;

        planetsParent.transform.position = startingPos;
        planetsParent.transform.rotation = startingRotation;

        // Rotate the solar system to put sun at 0 on the y-plane
        Vector3 sunPos = sun.transform.position;
        Vector3 sunPosOnYPlane = sunPos;
        sunPosOnYPlane.y = 0;

        float angle = Vector3.Angle(sunPos, sunPosOnYPlane);
        //planetsParent.transform.rotation = Quaternion.AngleAxis(angle, -rotationAxis);

        

        Quaternion rotationBefore = sun.transform.rotation;
        rotationBefore = Quaternion.Euler(sun.transform.rotation.x, 0, sun.transform.rotation.z);

        sun.transform.rotation = Quaternion.identity;
        planetTransform.parent.SetParent(planetsParent.transform, true);
        
        // Place the sun back at origo
        Vector3 distanceFromOrigin = sun.transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;

        // Align the planet with the others
        Vector3 oldPos = planetTransform.parent.position;
        Vector3 newPlanetPos = planetTransform.parent.position;
        newPlanetPos.y = 0;
        planetTransform.parent.position = newPlanetPos;

        Vector3 SunToPlanetDirection = planet.transform.parent.position - sun.transform.position;
        Vector3 SunToOldPosDirection = oldPos - sun.transform.position;
        angleForHeightDiff = Vector3.Angle(SunToOldPosDirection, SunToPlanetDirection);

        planetToSunDirection = sun.transform.position - planet.transform.parent.position;


        // Rotate the planet 
        planet.transform.rotation *= Quaternion.Inverse(planetRotationBefore);
        //planet.transform.rotation *= Quaternion.AngleAxis(angle, -rotationAxis);
        planet.transform.rotation *= Quaternion.Inverse(rotationBefore);
        //planet.transform.rotation *= Quaternion.AngleAxis(angleForHeightDiff, planetToSunDirection);

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
