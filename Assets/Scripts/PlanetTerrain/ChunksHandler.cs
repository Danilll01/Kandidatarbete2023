using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;

public class ChunksHandler : MonoBehaviour
{
    // Mesh jobs in flight for every planet together, so the GPU and the readbacks are never flooded
    private const int maxJobsInFlight = 8;
    // Planets the player is only approaching get a smaller share
    private const int maxPrefetchJobsInFlight = 4;
    private static int jobsInFlight;

    private enum SlotState : byte
    {
        Unknown,
        Empty,
        Live
    }

    private Planet planet;
    private PillPlayerController player;
    private int foliageInitialized = 10;
    private MarchingCubes marchingCubes;
    private Material planetMaterial;
    [HideInInspector] public float planetRadius;
    private MinMaxTerrainLevel terrainLevel;
    private RandomX rand;
    private int chunkSeedBase;

    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] private GameObject chunksParentLowRes;
    [SerializeField] private GameObject chunksParentHighRes;
    private List<Chunk> chunksLowRes = new List<Chunk>();
    private readonly List<Chunk> chunksHighRes = new List<Chunk>();
    [SerializeField] public TerrainColor terrainColor;

    // The amount of chunks
    [SerializeField] public int lowChunkRes = 1;
    [SerializeField] public int highChunkRes = 4;

    // The resolution of the chunk
    [SerializeField] public ResolutionSetting highRes;
    [SerializeField] public ResolutionSetting mediumRes;
    [SerializeField] public ResolutionSetting lowRes;

    [Header("Streaming")]
    [SerializeField, Tooltip("Start building the detailed chunks when the player is this many planet radii from the planet centre")]
    private float prefetchRadiusMultiplier = 2.5f;
    [SerializeField, Tooltip("How far past a level of detail boundary a chunk may be before it changes resolution")]
    private float lodHysteresis = 50f;
    [SerializeField, Tooltip("Seconds between checks of the terrain height under the player")]
    private float groundCheckInterval = 0.1f;
    [SerializeField, Tooltip("Foliage closer than this to the player gets colliders")]
    private float foliageColliderRadius = 80f;
    [SerializeField, Tooltip("Frames between updates of which foliage has colliders")]
    private int foliageColliderInterval = 10;

    // The detailed grid, one slot per possible chunk. Chunk objects are only created for slots with terrain.
    private SlotState[] slotStates;
    private Chunk[] slotChunks;
    private Vector3[] slotPositions;
    private bool[] slotPending;
    private int unresolvedSlots;
    private int initialUnresolvedSlots;
    private bool highGridCreated;
    private int jobsInFlightHere;
    private int lowJobsPending;
    private float chunkHalfDiagonal;
    private int nextFoliageColliderUpdate;

    // Best slots to generate this frame, sorted by distance
    private readonly int[] candidateSlots = new int[maxJobsInFlight];
    private readonly int[] candidateResolutions = new int[maxJobsInFlight];
    private readonly float[] candidateDistances = new float[maxJobsInFlight];

    private bool playerOnPlanet;
    private bool showingHighRes;
    private int chunkVisibilityIndex = 0;

    // Ground safety net
    private float surfaceRadius = -1;
    private Vector3 surfaceDirection;
    private float nextGroundCheck;
    private bool groundCheckPending;

    [System.Serializable]
    public struct ResolutionSetting
    {
        public float lowerRadius, upperRadius;
        public int resolution;

        public ResolutionSetting(float lowerRadius, float upperRadius, int resolution)
        {
            this.lowerRadius = lowerRadius;
            this.upperRadius = upperRadius;
            this.resolution = resolution;
        }
    }

    /// <summary>
    /// True when every chunk of the detailed grid has a mesh or is known to be empty
    /// </summary>
    public bool HighGridReady => highGridCreated && unresolvedSlots == 0;

    /// <summary>
    /// Chunks close to the player that are missing the resolution they should have
    /// </summary>
    public int OutstandingNearbyChunks { get; private set; } = int.MaxValue;

    /// <summary>
    /// How much of the detailed grid has been generated, between 0 and 1
    /// </summary>
    public float HighGridProgress => !highGridCreated ? 0 : initialUnresolvedSlots == 0 ? 1 : 1 - unresolvedSlots / (float)initialUnresolvedSlots;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        jobsInFlight = 0;
    }

    public void Initialize(Planet planet, MinMaxTerrainLevel terrainLevel, bool spawn, int seed)
    {
        rand = new RandomX(seed);
        chunkSeedBase = rand.Next();

        this.planet = planet;
        this.terrainLevel = terrainLevel;
        player = Universe.player;
        marchingCubes = planet.marchingCubes;
        planetRadius = planet.radius;

        playerOnPlanet = spawn;

        CreateLowResChunks();

        // The planet the player spawns on is built straight away, the others when the player gets close
        if (spawn)
            CreateHighResGrid();
    }

    public void SetupMaterial()
    {
        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, rand.Next(), planet.biomeSettings);

        SetChunksMaterials(chunksLowRes);
        SetChunksMaterials(chunksHighRes);
    }

    void Update()
    {
        Transform playerMover = player.boarded ? Universe.spaceShip : player.transform;
        playerOnPlanet = ReferenceEquals(transform, playerMover.parent);
        Vector3 playerPos = transform.InverseTransformPoint(playerMover.position);

        if (foliageInitialized != 0)
        {
            foliageInitialized--;
        }

        if (!highGridCreated && (playerOnPlanet || playerPos.magnitude < planetRadius * prefetchRadiusMultiplier))
        {
            CreateHighResGrid();
        }

        if (highGridCreated)
        {
            ScheduleChunkJobs(playerPos);
        }

        // Keep showing the low resolution planet until the detailed one is complete, so it never has holes
        bool showHighRes = playerOnPlanet && HighGridReady;
        if (showHighRes != showingHighRes)
        {
            chunksParentHighRes.SetActive(showHighRes);
            chunksParentLowRes.SetActive(!showHighRes);
            showingHighRes = showHighRes;
        }

        if (playerOnPlanet)
        {
            KeepAboveGround(playerMover, playerPos);
        }

        if (!showingHighRes)
        {
            return;
        }

        UpdateChunksVisibility(playerPos);

        if (Time.frameCount >= nextFoliageColliderUpdate)
        {
            nextFoliageColliderUpdate = Time.frameCount + foliageColliderInterval;
            UpdateFoliageColliders(playerMover.position, playerPos);
        }

        //Foliage & creatures
        bool spawnedAnimals = false;
        foreach (Chunk chunk in chunksHighRes)
        {
            if (chunk == null || !chunk.initialized)
            {
                continue;
            }
            if (!spawnedAnimals && chunk.creatures.initialized && !chunk.creatures.FinishedSpawning)
            {
                spawnedAnimals = true;
                chunk.creatures.BatchedSpawning();
            }
            if (chunk.foliage.initialized && !chunk.foliage.FinishedSpawning)
            {
                chunk.foliage.BatchedSpawning();
            }
        }
    }

    private void CreateLowResChunks()
    {
        chunksParentLowRes = CreateChunksParent("chunksLowRes", chunksParentLowRes);

        int noChunks = 1 << (lowChunkRes * 3);
        for (int i = 0; i < noChunks; i++)
        {
            Chunk chunk = CreateChunk(i, lowChunkRes, chunksParentLowRes);
            chunksLowRes.Add(chunk);

            lowJobsPending++;
            marchingCubes.GenerateMesh(i, lowRes.resolution, lowChunkRes, (mesh, vertices) =>
            {
                lowJobsPending--;
                if (this == null)
                {
                    if (mesh != null) Destroy(mesh);
                    return;
                }

                if (mesh == null)
                {
                    // Remove chunks without vertices
                    Destroy(chunk.gameObject);
                }
                else
                {
                    UpdateTerrainLevel(vertices);
                    chunk.UpdateMesh(mesh, lowRes.resolution);
                }

                if (lowJobsPending == 0)
                {
                    chunksLowRes.RemoveAll(c => c == null);
                    terrainColor.UpdateTerrainLevel(terrainLevel);
                }
            });
        }
    }

    private void CreateHighResGrid()
    {
        highGridCreated = true;

        chunksParentHighRes = CreateChunksParent("chunksHighRes", chunksParentHighRes);
        chunksParentHighRes.SetActive(false);

        int noChunks = 1 << (highChunkRes * 3);
        slotStates = new SlotState[noChunks];
        slotChunks = new Chunk[noChunks];
        slotPositions = new Vector3[noChunks];
        slotPending = new bool[noChunks];

        // Chunks entirely inside the lowest or outside the highest possible surface can't have any terrain
        marchingCubes.GetSurfaceBounds(out float minSurface, out float maxSurface);
        chunkHalfDiagonal = planetRadius / (1 << highChunkRes) * Mathf.Sqrt(3) * 1.05f;

        unresolvedSlots = 0;
        for (int i = 0; i < noChunks; i++)
        {
            Vector3 position = Chunk.CalculatePosition(i, highChunkRes, planetRadius);
            slotPositions[i] = position;

            float distance = position.magnitude;
            if (distance + chunkHalfDiagonal < minSurface || distance - chunkHalfDiagonal > maxSurface)
            {
                slotStates[i] = SlotState.Empty;
            }
            else
            {
                slotStates[i] = SlotState.Unknown;
                unresolvedSlots++;
            }
        }
        initialUnresolvedSlots = unresolvedSlots;
    }

    private GameObject CreateChunksParent(string parentName, GameObject oldParent)
    {
        if (oldParent != null) Destroy(oldParent);

        GameObject parent = new GameObject(parentName);
        parent.transform.SetParent(transform, false);
        return parent;
    }

    private Chunk CreateChunk(int index, int chunkResolution, GameObject parent)
    {
        Chunk chunk = Instantiate(chunkPrefab, parent.transform, false);
        chunk.transform.localPosition = Vector3.zero;
        chunk.transform.localRotation = Quaternion.identity;
        chunk.name = "chunk" + index;
        chunk.Setup(index, chunkResolution, planetRadius);
        // Seeded by index so foliage and creatures don't depend on the order chunks are loaded in
        chunk.Initialize(planet, this, chunkSeedBase + index * 7919 + chunkResolution);
        if (planetMaterial != null) chunk.SetMaterial(planetMaterial);
        return chunk;
    }

    private void SetChunksMaterials(List<Chunk> chunksList)
    {
        foreach (Chunk chunk in chunksList)
            if (chunk != null) chunk.SetMaterial(planetMaterial);
    }

    private void UpdateTerrainLevel(Vector3[] vertices)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            terrainLevel.UpdateMinMax(vertices[i]);
        }
    }

    /// <summary>
    /// Starts mesh jobs for the closest chunks that don't have the resolution they should have
    /// </summary>
    private void ScheduleChunkJobs(Vector3 playerPos)
    {
        int ownLimit = playerOnPlanet ? maxJobsInFlight : maxPrefetchJobsInFlight;
        int budget = Mathf.Min(maxJobsInFlight - jobsInFlight, ownLimit - jobsInFlightHere);

        int found = 0;
        int outstandingNearby = 0;
        for (int i = 0; i < slotStates.Length; i++)
        {
            SlotState state = slotStates[i];
            if (state == SlotState.Empty) continue;

            float distance = Vector3.Distance(playerPos, slotPositions[i]);
            int current = state == SlotState.Live ? slotChunks[i].CurrentResolution : 0;
            int target = TargetResolution(distance, current);
            if (target == current) continue;

            if (distance < mediumRes.upperRadius) outstandingNearby++;
            if (slotPending[i] || budget <= 0) continue;

            // Insert into the sorted list of the closest candidates
            int position = found < budget ? found : budget;
            while (position > 0 && candidateDistances[position - 1] > distance)
            {
                if (position < budget)
                {
                    candidateSlots[position] = candidateSlots[position - 1];
                    candidateResolutions[position] = candidateResolutions[position - 1];
                    candidateDistances[position] = candidateDistances[position - 1];
                }
                position--;
            }
            if (position < budget)
            {
                candidateSlots[position] = i;
                candidateResolutions[position] = target;
                candidateDistances[position] = distance;
                if (found < budget) found++;
            }
        }
        OutstandingNearbyChunks = outstandingNearby;

        for (int i = 0; i < found; i++)
        {
            DispatchChunkJob(candidateSlots[i], candidateResolutions[i]);
        }
    }

    private int TargetResolution(float distance, int current)
    {
        // Stay at the current resolution until the chunk is clearly inside another band, so chunks on a boundary don't flip back and forth
        if (current == highRes.resolution && distance < highRes.upperRadius + lodHysteresis) return current;
        if (current == mediumRes.resolution && distance < mediumRes.upperRadius + lodHysteresis && distance > highRes.upperRadius - lodHysteresis) return current;
        if (current == lowRes.resolution && distance > mediumRes.upperRadius - lodHysteresis) return current;

        if (distance < highRes.upperRadius) return highRes.resolution;
        if (distance < mediumRes.upperRadius) return mediumRes.resolution;
        return lowRes.resolution;
    }

    private void DispatchChunkJob(int slot, int resolution)
    {
        slotPending[slot] = true;
        jobsInFlight++;
        jobsInFlightHere++;

        marchingCubes.GenerateMesh(slot, resolution, highChunkRes, (mesh, vertices) =>
        {
            if (this == null)
            {
                if (mesh != null) Destroy(mesh);
                return;
            }

            jobsInFlight--;
            jobsInFlightHere--;
            slotPending[slot] = false;

            if (slotStates[slot] == SlotState.Unknown)
            {
                unresolvedSlots--;
                if (unresolvedSlots == 0) terrainColor.UpdateTerrainLevel(terrainLevel);
            }

            Chunk chunk = slotChunks[slot];
            if (mesh == null)
            {
                // Remember the resolution even without a mesh so it is not generated again
                if (chunk == null) slotStates[slot] = SlotState.Empty;
                else chunk.MarkResolution(resolution);
                return;
            }

            if (resolution == lowRes.resolution) UpdateTerrainLevel(vertices);

            if (chunk == null)
            {
                chunk = CreateChunk(slot, highChunkRes, chunksParentHighRes);
                slotChunks[slot] = chunk;
                slotStates[slot] = SlotState.Live;
                chunksHighRes.Add(chunk);
            }

            if (resolution >= mediumRes.resolution)
            {
                Physics.BakeMesh(mesh.GetEntityId(), false);
            }
            chunk.SetActivated(true);
            chunk.UpdateMesh(mesh, resolution);
        });
    }

    /// <summary>
    /// Pushes the player back out if they somehow end up below the terrain, for example before a collider has loaded
    /// </summary>
    private void KeepAboveGround(Transform mover, Vector3 localPos)
    {
        if (!groundCheckPending && Time.unscaledTime >= nextGroundCheck)
        {
            groundCheckPending = true;
            nextGroundCheck = Time.unscaledTime + groundCheckInterval;
            Vector3 direction = localPos.normalized;
            marchingCubes.GetSurfaceRadius(direction, height =>
            {
                groundCheckPending = false;
                surfaceRadius = height;
                surfaceDirection = direction;
            });
        }

        // Only trust the height close to where it was measured
        if (surfaceRadius <= 0 || Vector3.Angle(localPos, surfaceDirection) * Mathf.Deg2Rad * surfaceRadius > 20f)
            return;

        // The mesh only approximates the analytic surface (by up to a voxel on medium resolution), so leave room before stepping in
        const float allowedDepth = 10f;
        if (localPos.magnitude >= surfaceRadius - allowedDepth)
            return;

        Vector3 up = transform.TransformDirection(localPos.normalized);
        Vector3 worldPos = transform.TransformPoint(localPos.normalized * (surfaceRadius + 1f));
        mover.position = worldPos;
        if (mover.TryGetComponent(out Rigidbody body))
        {
            body.position = worldPos;
            if (!body.isKinematic)
            {
                float inwardSpeed = Vector3.Dot(body.linearVelocity, up);
                if (inwardSpeed < 0) body.linearVelocity -= up * inwardSpeed;
            }
        }
    }

    // Foliage is drawn with instancing and only gets colliders near the player, including chunks the player just left
    private void UpdateFoliageColliders(Vector3 playerWorldPos, Vector3 playerPos)
    {
        float reach = foliageColliderRadius + chunkHalfDiagonal;
        foreach (Chunk chunk in chunksHighRes)
        {
            Foliage foliage = chunk.foliage;
            if (!foliage.initialized) continue;

            if ((chunk.position - playerPos).sqrMagnitude < reach * reach || foliage.HasActiveColliders)
            {
                foliage.UpdateColliders(playerWorldPos, foliageColliderRadius);
            }
        }
    }

    private void UpdateChunksVisibility(Vector3 playerPos)
    {
        if (chunksHighRes.Count == 0) return;

        Vector3 cutoffPoint;
        if (playerPos.magnitude > (planetRadius + 30f))
            cutoffPoint = playerPos / 10000f;
        else
            cutoffPoint = playerPos / 1.5f;

        for (int i = 0; i < 50; i++)
        {
            chunkVisibilityIndex = (chunkVisibilityIndex + 1) % chunksHighRes.Count;
            Chunk chunk = chunksHighRes[chunkVisibilityIndex];

            bool isBelowHalfWayPoint = CheckIfPointBIsBelowPointA(cutoffPoint, chunk.position, cutoffPoint.normalized);
            if (isBelowHalfWayPoint)
            {
                chunk.gameObject.SetActive(false);
            }
            else
            {
                chunk.gameObject.SetActive(true);
                if (foliageInitialized == 0)
                {
                    chunk.foliage.SpawnFoliageOnChunk();
                    CreatureSpawning creatureSpawning = chunk.creatures;
                    if (creatureSpawning.initialized) creatureSpawning.GeneratePackSpawns();
                }
            }
        }
    }

    private bool CheckIfPointBIsBelowPointA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0);
    }

    private void OnDestroy()
    {
        // Jobs that finish after this are ignored, so give their share of the budget back now
        jobsInFlight -= jobsInFlightHere;
        jobsInFlightHere = 0;
    }
}
