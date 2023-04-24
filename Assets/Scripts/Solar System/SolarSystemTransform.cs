using UnityEngine;


public class SolarSystemTransform : MonoBehaviour
{
    [SerializeField] private SpawnPlanets spawnPlanets;
    private Planet activePlanet;
    private Planet oldActivePlanet;
    private GameObject sun;
    private GameObject planetsParent;
    [SerializeField] private PillPlayerController player;
    private bool rotateSolarSystem;
    private bool setUpSolarSystemRotation;
    private Vector3[] relativePlanetSunDistances;
    private Vector3 rotationAxis;
    private float rotationSpeed;
    private float orbitSpeed;
    private bool releasePlayer;
    private bool moonIsActivePlanet;
    private int activePlanetIndex;
    private Planet activeMoonParentPlanet;

    

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

        Universe.player.attractor = activePlanet;
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

            if (releasePlayer) return;

            if (rotateSolarSystem)
            {
                RotateSolarSystem();
            }
            else if (relativePlanetSunDistances != null)
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

        Universe.player.attractor = activePlanet;
    }

    private void UpdateClosestPlanet()
    {
        // Loops over all planets and checks if the player is on it or has left it
        foreach (Planet planet in spawnPlanets.bodies)
        {
            if (CheckIfNewActivePlanet(planet)) break;

            for (int i = 0; i < planet.moons.Count; i++)
            {
                Planet moon = planet.moons[i];
                if (CheckIfNewActivePlanet(moon))
                {
                    moonIsActivePlanet = true;
                    activeMoonParentPlanet = planet;
                    planet.playerIsOnMoon = true;
                    planet.activeMoonIndex = i;
                    break;
                }
            }
        }
    }

    private bool CheckIfNewActivePlanet(Planet planet)
    {
        float distance = (player.transform.position - planet.transform.position).magnitude;
        if (distance <= (planet.radius * 1.26) && planet != activePlanet)
        {
            activePlanet = planet;
            player.transform.parent = activePlanet.transform;
            return true;
        }

        if (planet == activePlanet && distance > (planet.radius * 1.4))
        {
            activePlanet = null;
            return true;
        }

        return false;
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
            //activePlanet.rotateMoons = true;
            oldActivePlanet = activePlanet;
        }
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

        Vector3 newSunPos = sun.transform.position;
        Vector3 direction;
        
        if (moonIsActivePlanet)
        {
            direction = newSunPos - activeMoonParentPlanet.transform.position;
            newSunPos = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;
        }
        else
        {
            direction = newSunPos - Vector3.zero;
            newSunPos = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;
        }
        
        

        newSunPos = ClosestPointOnPlane(Vector3.zero, sun.transform.TransformDirection(Vector3.up), newSunPos);
        sun.transform.position = newSunPos;

        foreach (var planetBody in spawnPlanets.bodies)
        {
            planetBody.Run();
        }
    }

    private void MovePlanets()
    {
        // Calculate the distance from the planet that should be centered at origo
        // Move the solar system by that distance to place planet in origo
        Transform planetTransform = activePlanet.transform;
        Vector3 distanceFromOrigin = planetTransform.transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;
        
        player.attractor = activePlanet;

        if (!moonIsActivePlanet)
        {
            planetTransform.parent.SetParent(null, true);
            activePlanetIndex = spawnPlanets.bodies.IndexOf(activePlanet);
        }
        else
        {
            planetTransform.parent.parent.parent.SetParent(null, true);
            activePlanetIndex = spawnPlanets.bodies.IndexOf(activeMoonParentPlanet);
        }

        rotateSolarSystem = true;
    }

    private void ResetPlanetOrbit()
    {
        foreach (Planet body in spawnPlanets.bodies)
        {
            body.transform.parent.SetParent(sun.transform);
        }

        sun.transform.rotation = Quaternion.Euler(0, sun.transform.rotation.y, 0);
        sun.transform.position = Vector3.zero;

        foreach (Planet body in spawnPlanets.bodies)
        {
            body.transform.parent.SetParent(planetsParent.transform);
            body.ResetOrbitComponents();
        }
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

    private Vector3 ClosestPointOnPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return point + DistanceFromPlane(planeOffset, planeNormal, point) * planeNormal;
    }

    private float DistanceFromPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return Vector3.Dot(planeOffset - point, planeNormal);
    }

    private void OnDrawGizmos()
    {
        if (!spawnPlanets.solarSystemGenerated)
        {
            return;
        }

        float radius = (sun.transform.position - Vector3.zero).magnitude;
        Universe.DrawGizmosCircle(Vector3.zero, sun.transform.up, radius, 32);
    }
}