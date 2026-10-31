using UnityEngine;


public class SolarSystemTransform : MonoBehaviour
{
    [SerializeField] private SpawnPlanets spawnPlanets;
    private TintFoliageTextures tintFoliageTexturesHandler;
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
    private bool releasedPlayer;

    // Used to keep the ship's speed relative to a planet when it becomes the centre of the solar system
    private System.Collections.Generic.Dictionary<Planet, Vector3> currentShipOffsets = new System.Collections.Generic.Dictionary<Planet, Vector3>();
    private System.Collections.Generic.Dictionary<Planet, Vector3> previousShipOffsets = new System.Collections.Generic.Dictionary<Planet, Vector3>();
    private Vector3 pendingRelativeVelocity;
    private bool hasPendingRelativeVelocity;
    private SpaceShipController shipController;




    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.sun;
        }

        planetsParent = gameObject;

        player = Universe.player;

        tintFoliageTexturesHandler = GetComponent<TintFoliageTextures>();
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
        CaptureShipOffsets();

        if (!releasePlayer)
        {
            UpdateClosestPlanet();
            HandleUpdatedActivePlanet();
            MatchShipToNewActivePlanet();

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

            // Interpolated rigidbodies would otherwise snap back to their old position
            Physics.SyncTransforms();
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
            // The planet stops moving when it becomes the centre, so remember how fast the ship moved relative to it
            if (previousShipOffsets.TryGetValue(planet, out Vector3 previousOffset) && currentShipOffsets.TryGetValue(planet, out Vector3 currentOffset))
            {
                pendingRelativeVelocity = (currentOffset - previousOffset) / Time.fixedDeltaTime;
                hasPendingRelativeVelocity = true;
            }

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

    // Records where the ship (or player) is relative to every body at the start of each physics step.
    // Relative offsets aren't affected when the whole solar system is moved to keep the player near the origin.
    private void CaptureShipOffsets()
    {
        (previousShipOffsets, currentShipOffsets) = (currentShipOffsets, previousShipOffsets);
        currentShipOffsets.Clear();

        Vector3 moverPosition = Universe.player.boarded ? Universe.spaceShip.position : player.transform.position;
        foreach (Planet planet in spawnPlanets.bodies)
        {
            currentShipOffsets[planet] = moverPosition - planet.transform.position;
            foreach (Planet moon in planet.moons)
            {
                currentShipOffsets[moon] = moverPosition - moon.transform.position;
            }
        }
    }

    private void MatchShipToNewActivePlanet()
    {
        if (!hasPendingRelativeVelocity) return;
        hasPendingRelativeVelocity = false;

        if (!Universe.player.boarded) return;

        if (shipController == null) shipController = Universe.spaceShip.GetComponent<SpaceShipController>();

        // The kept speed fades over time, so the ship travels about speed * fade time. Limit the speed towards the planet
        // so that distance is at most half the way down to the highest terrain, otherwise small planets could still be hit.
        Vector3 toPlanet = activePlanet.transform.position - Universe.spaceShip.position;
        float terrainTop = activePlanet.terrainLevel.GetMax();
        if (float.IsInfinity(terrainTop)) terrainTop = activePlanet.radius; // Not measured yet
        float heightAboveTerrain = toPlanet.magnitude - terrainTop;
        float maxInwardSpeed = Mathf.Max(0, heightAboveTerrain) * 0.5f / shipController.FrameVelocityFadeTime;
        Vector3 inwardDirection = toPlanet.normalized;
        float inwardSpeed = Vector3.Dot(pendingRelativeVelocity, inwardDirection);
        if (inwardSpeed > maxInwardSpeed)
        {
            pendingRelativeVelocity -= inwardDirection * (inwardSpeed - maxInwardSpeed);
        }

        shipController.MatchReferenceFrame(pendingRelativeVelocity);
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
            tintFoliageTexturesHandler.biomeColors = activePlanet.biomeColors;
            tintFoliageTexturesHandler.TintTextures();
            AssignNewBiomeMaterialsToCollections();
        }
    }

    private void AssignNewBiomeMaterialsToCollections()
    {
        FoliageCollection[] foliageCollections = activePlanet.foliageHandler.foliageCollections;

        foliageCollections[0].biomeMaterials[0] = tintFoliageTexturesHandler.biomeMaterials[0];
        foliageCollections[0].biomeMaterials[1] = tintFoliageTexturesHandler.biomeMaterials[5];

        foliageCollections[1].biomeMaterials[0] = tintFoliageTexturesHandler.biomeMaterials[2];
        foliageCollections[1].biomeMaterials[1] = tintFoliageTexturesHandler.biomeMaterials[7];

        foliageCollections[2].biomeMaterials[0] = tintFoliageTexturesHandler.biomeMaterials[4];
        foliageCollections[2].biomeMaterials[1] = tintFoliageTexturesHandler.biomeMaterials[9];

        foliageCollections[3].biomeMaterials[0] = tintFoliageTexturesHandler.biomeMaterials[1];
        foliageCollections[3].biomeMaterials[1] = tintFoliageTexturesHandler.biomeMaterials[6];

        foliageCollections[4].biomeMaterials[0] = tintFoliageTexturesHandler.biomeMaterials[3];
        foliageCollections[4].biomeMaterials[1] = tintFoliageTexturesHandler.biomeMaterials[8];

        foliageCollections[5].biomeMaterials[0] = tintFoliageTexturesHandler.biomeMaterials[4];
        foliageCollections[5].biomeMaterials[1] = tintFoliageTexturesHandler.biomeMaterials[9];

        foliageCollections[6].biomeMaterials[0] = tintFoliageTexturesHandler.biomeMaterials[0];
        foliageCollections[6].biomeMaterials[1] = tintFoliageTexturesHandler.biomeMaterials[5];
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


        // Rotates skybox the have the same rotation as the sun
        RotateSkyBox();
        
        
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

    // Rotates skybox after sun rotation
    private void RotateSkyBox()
    {
        sun.transform.rotation.ToAngleAxis(out float sunAngle, out Vector3 sunAxis);
        RenderSettings.skybox.SetVector(RotationAxis, sunAxis);
        RenderSettings.skybox.SetFloat(Rotation, sunAngle);
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

        // Rotate back the skybox after the suns rotation
        RotateSkyBox();
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