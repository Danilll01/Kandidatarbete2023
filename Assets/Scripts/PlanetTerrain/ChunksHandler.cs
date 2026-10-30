using System;
using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;

public class ChunksHandler : MonoBehaviour
{
    private Planet planet;
    private PillPlayerController player;
    private int foliageInitialized = 10;
    private int chunkResolution; //This is 2^chunkResolution
    private MarchingCubes marchingCubes;
    private Material planetMaterial;
    [HideInInspector] public float planetRadius;
    private MinMaxTerrainLevel terrainLevel;
    private RandomX rand;

    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] private GameObject chunksParentLowRes;
    [SerializeField] private GameObject chunksParentHighRes;
    [HideInInspector] private List<Chunk> chunksLowRes;
    [HideInInspector] private List<Chunk> chunksHighRes;
    [SerializeField] public TerrainColor terrainColor;

    private bool playerOnPlanet;
    private bool updateChunks = false;
    private int chunkVisibilityIndex = 0;

    // The amount of chunks
    [SerializeField] public int lowChunkRes = 1;
    [SerializeField] public int highChunkRes = 4;

    // The resolution of the chunk
    [SerializeField] public ResolutionSetting highRes;
    [SerializeField] public ResolutionSetting mediumRes;
    [SerializeField] public ResolutionSetting lowRes;

    // Used for chunk generation
    private ChunkGenerator chunkGenerator;

    // Init queue: sequential batched async mesh generation
    private Queue<InitMeshJob> initQueue = new Queue<InitMeshJob>();
    private int initPendingCount = 0;
    private bool initComplete = false;
    private const int initBatchSize = 16;

    struct InitMeshJob
    {
        public Chunk chunk;
        public int resolution;
        public int chunkRes;
        public MinMaxTerrainLevel terrainLevel;
    }

    enum ChunkResolution
    {
        High,
        Low
    }

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

    public void Initialize(Planet planet, MinMaxTerrainLevel terrainLevel, bool spawn, int seed)
    {
        rand = new RandomX(seed);

        this.planet = planet;
        this.terrainLevel = terrainLevel;
        player = Universe.player;
        marchingCubes = planet.marchingCubes;
        planetRadius = planet.radius;

        playerOnPlanet = spawn;

        // Setup and queue low-res chunks
        SetupChunks(lowChunkRes, ref chunksLowRes, ref chunksParentLowRes, ChunkResolution.Low);
        QueueMeshes(terrainLevel, chunksLowRes, lowChunkRes);

        // Setup and queue high-res chunks
        SetupChunks(highChunkRes, ref chunksHighRes, ref chunksParentHighRes, ChunkResolution.High);
        QueueMeshes(terrainLevel, chunksHighRes, highChunkRes);

        if (!playerOnPlanet)
            chunksParentHighRes.SetActive(false);
        else
            chunksParentLowRes.SetActive(false);

        chunkGenerator = new ChunkGenerator(chunksHighRes, this, marchingCubes, highChunkRes);
    }

    public void SetupMaterial()
    {
        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, rand.Next(), planet.biomeSettings);

        SetChunksMaterials(chunksLowRes);
        SetChunksMaterials(chunksHighRes);
    }

    void Update()
    {
        Transform currentPlayerMoverParent = player.boarded ? Universe.spaceShip.parent : player.transform.parent;
        if (playerOnPlanet != ReferenceEquals(transform, currentPlayerMoverParent))
        {
            updateChunks = true;
            playerOnPlanet = ReferenceEquals(transform, currentPlayerMoverParent);
        }

        if (foliageInitialized != 0)
        {
            foliageInitialized--;
        }

        if (updateChunks)
        {
            if (!playerOnPlanet)
            {
                chunksParentHighRes.SetActive(false);
                chunksParentLowRes.SetActive(true);
            }
            else
            {
                chunksParentLowRes.SetActive(false);
                chunksParentHighRes.SetActive(true);
            }
            updateChunks = false;
        }

        // Process init queue first — nothing else runs until init is done
        if (!initComplete)
        {
            ProcessInitQueue();
            return;
        }

        if (!playerOnPlanet)
        {
            return;
        }

        UpdateChunksVisibility();

        chunkGenerator.Update();

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

    private void SetupChunks(int chunkResolution, ref List<Chunk> chunksList, ref GameObject chunksParent, ChunkResolution res)
    {
        if (chunkResolution == this.chunkResolution)
            return;

        this.chunkResolution = chunkResolution;

        Destroy(chunksParent);

        chunksParent = new GameObject
        {
            name = (res == ChunkResolution.High) ? "chunksHighRes" : "chunksLowRes",
            transform =
            {
                parent = transform,
                localPosition = Vector3.zero
            }
        };

        marchingCubes.chunkResolution = chunkResolution;

        chunksList = new List<Chunk>();
        int noChunks = (1 << chunkResolution) * (1 << chunkResolution) * (1 << chunkResolution);
        for (int i = 0; i < noChunks; i++)
        {
            Chunk chunk = Instantiate(chunkPrefab, chunksParent.transform, true);
            chunk.transform.localPosition = Vector3.zero;
            chunk.name = "chunk" + i;
            chunk.Setup(i, marchingCubes);
            chunksList.Add(chunk);
        }
    }

    private void SetChunksMaterials(List<Chunk> chunksList)
    {
        foreach (Chunk chunk in chunksList)
            chunk.SetMaterial(planetMaterial);
    }

    /// <summary>
    /// Queues chunks for batched async init. Saves chunkRes per job so dispatch is correct.
    /// </summary>
    private void QueueMeshes(MinMaxTerrainLevel terrainLevel, List<Chunk> chunksList, int chunkRes)
    {
        for (int i = chunksList.Count - 1; i != -1; i--)
        {
            chunksList[i].Initialize(planet, this, rand.Next());

            initQueue.Enqueue(new InitMeshJob
            {
                chunk = chunksList[i],
                resolution = lowRes.resolution,
                chunkRes = chunkRes,
                terrainLevel = terrainLevel
            });
        }
    }

    /// <summary>
    /// Processes a batch of init jobs per frame. Waits for batch to complete before starting next.
    /// </summary>
    private void ProcessInitQueue()
    {
        // Wait for current batch to finish
        if (initPendingCount > 0) return;

        if (initQueue.Count == 0)
        {
            initComplete = true;
            // Drop chunks destroyed for having no vertices, otherwise ChunkGenerator retries them forever
            chunksLowRes.RemoveAll(c => c == null);
            chunksHighRes.RemoveAll(c => c == null);
            chunkVisibilityIndex = 0;
            // Ensure chunkResolution is correct for runtime LOD
            marchingCubes.chunkResolution = highChunkRes;
            if (playerOnPlanet) UpdateChunksVisibility();
            return;
        }

        // Dispatch a batch
        int batchCount = Mathf.Min(initBatchSize, initQueue.Count);
        for (int b = 0; b < batchCount; b++)
        {
            InitMeshJob job = initQueue.Dequeue();
            Chunk chunk = job.chunk;
            int res = job.resolution;
            int chunkRes = job.chunkRes;

            initPendingCount++;
            marchingCubes.generateMesh(job.terrainLevel, chunk.Index, res, chunkRes, (mesh, vertexCount) =>
            {
                initPendingCount--;
                if (mesh != null && vertexCount > 0)
                {
                    chunk.UpdateMesh(mesh, res);
                }
                else
                {
                    // Remove chunks without vertices
                    Destroy(chunk.gameObject);
                }
            });
        }
    }

    private void UpdateChunksVisibility()
    {
        Vector3 playerPos = player.boarded ? Universe.spaceShip.localPosition : player.transform.localPosition;

        Vector3 cutoffPoint;
        if (playerPos.magnitude > (planetRadius + 30f))
            cutoffPoint = playerPos / 10000f;
        else
            cutoffPoint = playerPos / 1.5f;

        if (chunksHighRes.Count == 0) return;

        for (int i = 0; i < 50; i++)
        {
            Chunk chunk = chunksHighRes[chunkVisibilityIndex];
            chunkVisibilityIndex = (chunkVisibilityIndex + 1) % chunksHighRes.Count;

            if (chunk == null) continue;

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
}

public class ChunkGenerator
{
    const int chunksPerFrame = 3;

    private PillPlayerController player;
    private List<Chunk> chunks;

    ChunksHandler handler;
    MarchingCubes generator;
    int chunkRes;

    // Track pending async work to avoid re-dispatching
    private int pendingCallbacks = 0;

    public ChunkGenerator(List<Chunk> chunks, ChunksHandler handler, MarchingCubes generator, int chunkRes)
    {
        this.chunks = chunks;
        this.handler = handler;
        this.generator = generator;
        this.chunkRes = chunkRes;
        player = Universe.player;
    }

    public void Update()
    {
        if (pendingCallbacks > 0) return;

        Vector3 playerPos = player.boarded ? Universe.spaceShip.localPosition : player.transform.localPosition;
        int jobCount = 0;

        foreach (Chunk chunk in chunks)
        {
            if (chunk == null || !chunk.initialized) continue;

            if (jobCount >= chunksPerFrame)
            {
                break;
            }

            float playerDistance = Vector3.Distance(playerPos, chunk.position);
            int resolution;
            if (playerDistance < handler.highRes.upperRadius)
            {
                resolution = handler.highRes.resolution;
            }
            else if (handler.mediumRes.lowerRadius < playerDistance && playerDistance < handler.mediumRes.upperRadius)
            {
                resolution = handler.mediumRes.resolution;
            }
            else if (handler.lowRes.lowerRadius < playerDistance)
            {
                resolution = handler.lowRes.resolution;
            }
            else
            {
                continue;
            }
            if (!chunk.HasResolution(resolution))
            {
                chunk.SetActivated(true);
                int res = resolution;
                Chunk targetChunk = chunk;
                pendingCallbacks++;
                generator.generateMesh(targetChunk.Index, res, chunkRes, (mesh, vertexCount) =>
                {
                    pendingCallbacks--;
                    if (mesh != null && vertexCount > 0)
                    {
                        // Only bake physics for highest resolution (creatures/foliage need colliders)
                        if (res == handler.highRes.resolution)
                        {
                            Physics.BakeMesh(mesh.GetEntityId(), false);
                        }
                        targetChunk.UpdateMesh(mesh, res);
                    }
                    else if (targetChunk != null)
                    {
                        // Remember the resolution even without a mesh so it is not regenerated every frame
                        targetChunk.MarkResolution(res);
                    }
                });
                jobCount++;
            }
        }
    }
}
