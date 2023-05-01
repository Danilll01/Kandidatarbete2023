using UnityEngine;


public class SolarSystemTransform : MonoBehaviour
{
    [SerializeField] private SpawnPlanets spawnPlanets;
    private Planet activePlanet;
    private Planet oldActivePlanet;
    private GameObject sun;
    private GameObject planetsParent;
    private bool rotateSolarSystem;
    private bool setUpSolarSystemRotation;
    private Vector3[] relativePlanetSunDistances;
    private Vector3 rotationAxis;
    private float rotationSpeed;
    private float orbitSpeed;
    private bool releasePlayer;
    private PillPlayerController player;

    private bool moonIsActivePlanet;
    private int activePlanetIndex;
    private Planet activeMoonParentPlanet;
    private static readonly int RotationAxis = Shader.PropertyToID("_RotationAxis");
    private static readonly int Rotation = Shader.PropertyToID("_Rotation");
    private float skyboxRotationAngle = 0;
    private bool releasedPlayer;


    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }

        planetsParent = gameObject;

        player = Universe.player;
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

        player.attractor = activePlanet;
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
                CheckWhenToReleasePlayer();
            }
            else
            {
                FloatingPointTeleportationCheck();

                if (rotateSolarSystem)
                {
                    RotateSolarSystem();
                }
                else if (relativePlanetSunDistances != null)
                {
                    foreach (Planet planetBody in spawnPlanets.bodies)
                    {
                        planetBody.Run();
                    }
                }
            }
        }
        
        
        player.attractor = activePlanet;
    }

    private void FloatingPointTeleportationCheck()
    {
        Vector3 currentPlayerControlPos = Universe.player.boarded ? Universe.spaceShip.position : player.transform.position;
        if (activePlanet == null && currentPlayerControlPos.magnitude >= 2500f)
        {
            Vector3 distanceFromOrigin = currentPlayerControlPos - Vector3.zero;
            planetsParent.transform.position -= distanceFromOrigin;

            if (!player.boarded)
            {
                player.transform.position -= distanceFromOrigin;
            }
            else
            {
                Universe.spaceShip.position -= distanceFromOrigin;
            }

            //player.transform.position -= distanceFromOrigin;
        }
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
        if (distance <= (planet.radius * 1.3) && planet != activePlanet)
        {
            activePlanet = planet;
                
            if (!player.boarded)
            {
                player.transform.parent = activePlanet.transform;
            }
            else
            {
                Universe.spaceShip.parent = activePlanet.transform;
            }
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
            oldActivePlanet = activePlanet;
            releasePlayer = true;
        }

        // If the player has entered a new planet, move the solar system accordingly
        if (activePlanet != oldActivePlanet)
        {
            MovePlanets();
            activePlanet.rotateMoons = true;
            oldActivePlanet = activePlanet;
        }
    }

    private void CheckWhenToReleasePlayer()
    {
        // Check if sun has moved to Vector3.zero
        if (sun.transform.position.magnitude < 5f)
        {

            player.attractor = null;
            
            if (!Universe.player.boarded)
            {
                player.transform.SetParent(null, true);
            }
            else
            {
                Universe.spaceShip.SetParent(null, true);
            }
            
            
            setUpSolarSystemRotation = false;
            releasePlayer = false;
        }
    }

    private void RotateSolarSystem()
    {
        SetUpRotation();

        if (moonIsActivePlanet)
        {
            sun.transform.RotateAround(activeMoonParentPlanet.transform.position, sun.transform.TransformDirection(Vector3.up), orbitSpeed * Time.deltaTime * 2f);
            planetsParent.transform.RotateAround(activeMoonParentPlanet.transform.position, -rotationAxis, rotationSpeed * Time.deltaTime);
        }
        else
        {
            sun.transform.RotateAround(Vector3.zero, sun.transform.TransformDirection(Vector3.up), orbitSpeed * Time.deltaTime * 2f);
            planetsParent.transform.RotateAround(Vector3.zero, -rotationAxis, rotationSpeed * Time.deltaTime);
        }
        

        skyboxRotationAngle += Time.deltaTime * rotationSpeed;
        RenderSettings.skybox.SetVector(RotationAxis, -rotationAxis);
        RenderSettings.skybox.SetFloat(Rotation, skyboxRotationAngle);

        Vector3 newSunPos = sun.transform.position;
        Vector3 direction;

        if (moonIsActivePlanet)
        {
            direction = newSunPos - activeMoonParentPlanet.transform.position;
            newSunPos = (direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude) + activeMoonParentPlanet.transform.position;
            newSunPos = ClosestPointOnPlane(activeMoonParentPlanet.transform.position, sun.transform.TransformDirection(Vector3.up), newSunPos);
        }
        else
        {
            direction = newSunPos - Vector3.zero;
            newSunPos = direction.normalized * relativePlanetSunDistances[activePlanetIndex].magnitude;
            newSunPos = ClosestPointOnPlane(Vector3.zero, sun.transform.TransformDirection(Vector3.up), newSunPos);
        }

        sun.transform.position = newSunPos;

        foreach (Planet planetBody in spawnPlanets.bodies)
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
        if (moonIsActivePlanet)
        {
            oldActivePlanet.transform.parent.SetParent(activeMoonParentPlanet.moonsParent.transform, true);
            activeMoonParentPlanet.playerIsOnMoon = false;
        }
        
        foreach (Planet body in spawnPlanets.bodies)
        {
            body.transform.parent.SetParent(sun.transform);
        }

        sun.transform.rotation = Quaternion.Euler(0, sun.transform.rotation.y, 0);
        sun.transform.position = Vector3.zero;
        
        foreach (Planet planet in spawnPlanets.bodies)
        {
            planet.transform.parent.SetParent(planetsParent.transform);
            planet.ResetOrbitComponents();
        }
        moonIsActivePlanet = false;
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

    private static Vector3 ClosestPointOnPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return point + DistanceFromPlane(planeOffset, planeNormal, point) * planeNormal;
    }

    private static float DistanceFromPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return Vector3.Dot(planeOffset - point, planeNormal);
    }

    private void OnDrawGizmos()
    {
        if (!spawnPlanets.solarSystemGenerated)
        {
            return;
        }

        if (moonIsActivePlanet)
        {
            float radius = (sun.transform.position - activeMoonParentPlanet.transform.position).magnitude;
            Universe.DrawGizmosCircle(activeMoonParentPlanet.transform.position, sun.transform.up, radius, 32);
        }
        else if(rotateSolarSystem)
        {
            float radius = (sun.transform.position - Vector3.zero).magnitude;
            Universe.DrawGizmosCircle(Vector3.zero, sun.transform.up, radius, 32);
        }
        
    }
}