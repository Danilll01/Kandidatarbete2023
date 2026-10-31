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
    private Vector3 enteringLocalVelocity;
    private bool hasEnteringVelocity;
    private Planet leftPlanet;
    private Quaternion previousSunRotation = Quaternion.identity;
    private Quaternion currentSunRotation = Quaternion.identity;
    private bool matchSkyNextStep;
    private const float maxTrackedSpeed = 20000f;
    private Vector3 leavingLocalVelocity;
    private int leavingSteps;
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
        MatchShipToLeftPlanet();
        MatchShipToSkyRotation();

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
            // The planet stops moving and spinning when it becomes the centre, so remember how the ship moved relative to its surface
            if (TryGetVelocityRelativeTo(planet, out Vector3 localVelocity))
            {
                enteringLocalVelocity = localVelocity;
                hasEnteringVelocity = true;
                matchSkyNextStep = true;
            }
            leftPlanet = null;

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
            // The planet starts moving again, so the ship gets its speed once it does (see MatchShipToLeftPlanet)
            if (TryGetVelocityRelativeTo(planet, out Vector3 localVelocity))
            {
                leftPlanet = planet;
                leavingLocalVelocity = localVelocity;
                leavingSteps = 0;
            }

            // The sky stops turning now, so the ship takes over that rotation and slows it down gradually
            if (Universe.player.boarded) ShipController.AddFrameAngularVelocity(-SkyAngularVelocity());
            matchSkyNextStep = false;

            activePlanet = null;
            return true;
        }

        return false;
    }

    // Records where the ship (or player) is in every body's own rotating space at the start of each physics step.
    // These offsets aren't affected when the whole solar system is moved to keep the player near the origin.
    private void CaptureShipOffsets()
    {
        (previousShipOffsets, currentShipOffsets) = (currentShipOffsets, previousShipOffsets);
        currentShipOffsets.Clear();

        // The skybox follows the sun's rotation, so this is also how the sky turns
        previousSunRotation = currentSunRotation;
        currentSunRotation = sun.transform.rotation;

        Vector3 moverPosition = Universe.player.boarded ? Universe.spaceShip.position : player.transform.position;
        foreach (Planet planet in spawnPlanets.bodies)
        {
            currentShipOffsets[planet] = planet.transform.InverseTransformPoint(moverPosition);
            foreach (Planet moon in planet.moons)
            {
                currentShipOffsets[moon] = moon.transform.InverseTransformPoint(moverPosition);
            }
        }
    }

    // Velocity of the ship relative to a body's surface over the last physics step, in the body's local space
    private bool TryGetVelocityRelativeTo(Planet planet, out Vector3 localVelocity)
    {
        localVelocity = Vector3.zero;
        if (!previousShipOffsets.TryGetValue(planet, out Vector3 previousOffset) || !currentShipOffsets.TryGetValue(planet, out Vector3 currentOffset))
            return false;

        localVelocity = (currentOffset - previousOffset) / Time.fixedDeltaTime;

        // Far faster than anything can fly means the ship was teleported, so there is no real velocity to keep
        return localVelocity.sqrMagnitude < maxTrackedSpeed * maxTrackedSpeed;
    }

    // How fast the sky turned during the last physics step, as a world axis times radians per second
    private Vector3 SkyAngularVelocity()
    {
        Quaternion delta = currentSunRotation * Quaternion.Inverse(previousSunRotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180) angle -= 360;
        if (Mathf.Abs(angle) < 1e-5f || float.IsInfinity(axis.x)) return Vector3.zero;
        return axis * (angle * Mathf.Deg2Rad / Time.fixedDeltaTime);
    }

    // When entering a planet the sky starts turning around it. After it has turned for a step, let the ship turn the
    // same way so the view doesn't jump, and fade that out so the sky's rotation eases in.
    private void MatchShipToSkyRotation()
    {
        if (!matchSkyNextStep) return;
        matchSkyNextStep = false;

        if (Universe.player.boarded) ShipController.AddFrameAngularVelocity(SkyAngularVelocity());
    }

    private SpaceShipController ShipController
    {
        get
        {
            if (shipController == null) shipController = Universe.spaceShip.GetComponent<SpaceShipController>();
            return shipController;
        }
    }

    // The new centre doesn't move or spin, so the ship's velocity becomes the one it had relative to its surface
    private void MatchShipToNewActivePlanet()
    {
        if (!hasEnteringVelocity) return;
        hasEnteringVelocity = false;

        if (!Universe.player.boarded || activePlanet == null) return;

        ShipController.MatchReferenceFrame(activePlanet.transform.TransformDirection(enteringLocalVelocity));
    }

    // After leaving a planet it starts orbiting and spinning again. Once it has moved for a full physics step, measure the
    // ship's velocity relative to it again and add the difference, so the ship keeps moving the same way relative to the planet.
    private void MatchShipToLeftPlanet()
    {
        if (leftPlanet == null) return;

        // Wait until the ship is released and the planet has moved for a full step
        if (releasePlayer || Universe.spaceShip.parent == leftPlanet.transform || ++leavingSteps < 2) return;

        if (Universe.player.boarded && TryGetVelocityRelativeTo(leftPlanet, out Vector3 localVelocity))
        {
            ShipController.AddLeavingFrameVelocity(leftPlanet.transform.TransformDirection(leavingLocalVelocity - localVelocity));
        }

        leftPlanet = null;
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