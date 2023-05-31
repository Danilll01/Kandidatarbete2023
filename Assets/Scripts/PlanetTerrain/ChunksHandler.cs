using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExtendedRandom;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ChunksHandler : MonoBehaviour
{
    private Planet planet;
    private PillPlayerController player;
    private Vector3 playerLastPosition;
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

    // The amount of chunks
    [SerializeField] public int lowChunkRes = 1;
    [SerializeField] public int highChunkRes = 4;

    // The resolution of the chunk
    [SerializeField] public ResolutionSetting highRes;
    [SerializeField] public ResolutionSetting mediumRes;
    [SerializeField] public ResolutionSetting lowRes;

    // Used for chunk culling
    private int index = 0;
    [SerializeField] private int maxChunkChecksPerFrame = 50;
    private ChunkGenerator chunkGenerator;
    private Vector3 lastChunkUpdatePlayerPosition = Vector3.zero;

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

    /// <summary>
    /// Initialize the values
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="player"></param>
    public void Initialize(Planet planet, MinMaxTerrainLevel terrainLevel, bool spawn, int seed)
    {
        rand = new RandomX(seed);

        this.planet = planet;
        player = Universe.player;
        marchingCubes = planet.marchingCubes;
        planetRadius = planet.radius;
        this.terrainLevel = terrainLevel;

        playerOnPlanet = spawn;

        SetupChunks(lowChunkRes, ref chunksLowRes, ref chunksParentLowRes, ChunkResolution.Low);
        CreateMeshes(terrainLevel, ref chunksLowRes);

        SetupChunks(highChunkRes, ref chunksHighRes, ref chunksParentHighRes, ChunkResolution.High);
        CreateMeshes(terrainLevel, ref chunksHighRes);

        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, rand.Next(), planet.biomeSettings);

        SetChunksMaterials(chunksLowRes);
        SetChunksMaterials(chunksHighRes);

        if (!playerOnPlanet)
            chunksParentHighRes.SetActive(false);
        else
        {
            chunksParentLowRes.SetActive(false);
            UpdateChunksVisibility();
        }

        chunkGenerator = new ChunkGenerator(chunksHighRes, this, marchingCubes, terrainLevel);
    }

    // Update is called once per frame
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

        // Check if the chunks needs updating
        if (updateChunks)
        {
            // Check if player is on planet or not
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

        // Only update the chunks if the player is close to the planet
        if (!playerOnPlanet)
        {
            return;
        }

        UpdateChunksVisibility();

        // Only update chunks if player has moved a certain distance
        Vector3 playerPos = player.boarded ? Universe.spaceShip.localPosition : player.transform.localPosition;
        if (Vector3.Magnitude(playerPos - lastChunkUpdatePlayerPosition) < 1.8f)
            return;
        lastChunkUpdatePlayerPosition = playerPos;

        chunkGenerator.Update();

        //Foliage & creatures
        foreach (Chunk chunk in chunksHighRes)
        {
            if (!chunk.initialized)
            {
                continue;
            }
            if (chunk.creatures.initialized && !chunk.creatures.FinishedSpawning)
            {
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
        // Don't create new ones if they are to be the same as old ones.
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

        // Create all chunks
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

    private void CreateMeshes(MinMaxTerrainLevel terrainLevel, ref List<Chunk> chunksList)
    {
        for (int i = chunksList.Count - 1; i != -1; i--)
        {
            // Remove chunks without vertices
            if (chunksList[i].Initialize(planet, terrainLevel, this, rand.Next()) == 0)
            {
                Destroy(chunksList[i].gameObject);
                chunksList.RemoveAt(i);
            }
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

        int count = 0;
        while (count < maxChunkChecksPerFrame)
        {
            bool isBelowHalfWayPoint = CheckIfPointBIsBelowPointA(cutoffPoint, chunksHighRes[index].position, cutoffPoint.normalized);
            if (isBelowHalfWayPoint)
            {
                chunksHighRes[index].gameObject.SetActive(false);
            }
            else
            {
                chunksHighRes[index].gameObject.SetActive(true);
                if (foliageInitialized == 0)
                {
                    chunksHighRes[index].foliage.SpawnFoliageOnChunk();
                    CreatureSpawning creatureSpawning = chunksHighRes[index].creatures;
                    if (creatureSpawning.initialized) creatureSpawning.GeneratePackSpawns();
                }
                    
            }
            count++;
            index = index == 0 ? chunksHighRes.Count - 1 : index - 1;
        }
    }

    private bool CheckIfPointBIsBelowPointA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0);
    }
}

public class ChunkGenerator
{

    //Chunk work
    private List<(Chunk, Mesh, int)> chunkJobs;
    private Queue<int> physicsBakeQueue = new Queue<int>();
    private PillPlayerController player;
    private List<Chunk> chunks;

    ChunksHandler handler;
    MarchingCubes generator;
    MinMaxTerrainLevel terrainLevel;

    Thread worker;
    Semaphore physicsWorkerSemaphore = new Semaphore(0, 1);
    bool chunkPhysicsActive = false;
    bool chunkPhysicsComplete = false;

    public ChunkGenerator(List<Chunk> chunks, ChunksHandler handler, MarchingCubes generator, MinMaxTerrainLevel terrainLevel)
    {
        this.chunks = chunks;
        this.handler = handler;
        this.generator = generator;
        this.terrainLevel = terrainLevel;
        player = Universe.player;

        worker = new Thread(Work);
        worker.IsBackground = true;
        worker.Start();
    }

    public void Update()
    {
        //Update new chunk meshes with precalculated physics from worker
        if (chunkPhysicsComplete)
        {
            foreach ((Chunk chunk, Mesh mesh, int resolution) in chunkJobs)
            {
                chunk.UpdateMesh(mesh, resolution);
            }
            chunkPhysicsActive = chunkPhysicsComplete = false;
        }
        //Start new work
        if (!chunkPhysicsActive)
        {
            chunkJobs = new List<(Chunk, Mesh, int)>();
            Vector3 playerPos = player.boarded ? Universe.spaceShip.localPosition : player.transform.localPosition;
            foreach (Chunk chunk in chunks)
            {
                if (!chunk.initialized) continue;

                //MBY only check sometimes or only so many, idk, save 5%

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
                //No work to be done
                else
                {
                    continue;
                }
                if (!chunk.HasResolution(resolution))
                {
                    chunk.SetActivated(true);
                    Mesh mesh = new Mesh();
                    generator.generateMesh(terrainLevel, chunk.Index, resolution, mesh);
                    physicsBakeQueue.Enqueue(mesh.GetInstanceID());
                    chunkJobs.Add((chunk, mesh, resolution));
                }
            }
            if (physicsBakeQueue.Count != 0)
            {
                chunkPhysicsActive = true;
                physicsWorkerSemaphore.Release();
            }
        }
    }

    private void Work()
    {
        while (true)
        {
            //Wait for work to become available
            physicsWorkerSemaphore.WaitOne();

            //Calculate physics
            while (physicsBakeQueue.TryDequeue(out int meshID))
            {
                Physics.BakeMesh(meshID, false);
            }

            //Inform that work is done
            chunkPhysicsComplete = true;
        }
    }
}
