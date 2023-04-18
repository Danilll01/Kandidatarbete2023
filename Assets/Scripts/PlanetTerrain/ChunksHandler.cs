using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExtendedRandom;
using UnityEditor;
using UnityEngine;

public class ChunksHandler : MonoBehaviour
{
    private Planet planet;
    private Transform player;
    private Vector3 playerLastPosition;
    private int foliageInitialized = 10;
    private int chunkResolution; //This is 2^chunkResolution
    private MarchingCubes marchingCubes;
    private Material planetMaterial;
    [HideInInspector] public float planetRadius;
    private MinMaxTerrainLevel terrainLevel;
    private List<Vector3> waterPoints = new List<Vector3>();
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
    [SerializeField] public ResolutionSetting highRes = new ResolutionSetting( 0f, 1.3f, 3);
    [SerializeField] public ResolutionSetting mediumRes = new ResolutionSetting(1.5f, 2.0f, 2);
    [SerializeField] public ResolutionSetting lowRes = new ResolutionSetting(2.3f, 0xFFFFFFFFf, 1);

    // Used for chunk culling
    private int index = 0;
    [SerializeField] private int maxChunkChecksPerFrame = 50;

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
        player = planet.player;
        marchingCubes = planet.marchingCubes;
        planetRadius = planet.radius;
        this.terrainLevel = terrainLevel;

        playerOnPlanet = spawn;

        SetupChunks(lowChunkRes, ref chunksLowRes, ref chunksParentLowRes, ChunkResolution.Low);
        CreateMeshes(terrainLevel, ref chunksLowRes);

        SetupChunks(highChunkRes, ref chunksHighRes, ref chunksParentHighRes, ChunkResolution.High);
        CreateMeshes(terrainLevel, ref chunksHighRes);

        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, rand.Next());

        SetChunksMaterials(chunksLowRes);
        SetChunksMaterials(chunksHighRes);

        if (!playerOnPlanet)
            chunksParentHighRes.SetActive(false);
        else
        {
            chunksParentLowRes.SetActive(false);
            UpdateChunksVisibility();
        }
        GatherWaterPoints();
    }

    // Update is called once per frame
    void Update()
    {
        
        if (foliageInitialized != 0)
        {
            foliageInitialized--;
        }

        if (Universe.player.attractor == null)
        {
            return;
        }
        
        if (playerOnPlanet != ReferenceEquals(transform, Universe.player.attractor.transform))
        {
            updateChunks = true;
            playerOnPlanet = ReferenceEquals(transform, Universe.player.attractor.transform);
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
        if (playerOnPlanet)
            UpdateChunksVisibility();
    }

    private void SetupChunks(int chunkResolution, ref List<Chunk> chunksList, ref GameObject chunksParent, ChunkResolution res)
    {
        // Don't create new ones if they are to be the same as old ones.
        if (chunkResolution == this.chunkResolution)
            return;

        this.chunkResolution = chunkResolution;

        Destroy(chunksParent);

        chunksParent = new GameObject();
        chunksParent.name = (res == ChunkResolution.High) ? "chunksHighRes" : "chunksLowRes";
        chunksParent.transform.parent = transform;
        chunksParent.transform.localPosition = Vector3.zero;

        marchingCubes.chunkResolution = chunkResolution;

        // Create all chunks
        chunksList = new List<Chunk>();
        int noChunks = (1 << chunkResolution) * (1 << chunkResolution) * (1 << chunkResolution);
        for (int i = 0; i < noChunks; i++)
        {
            Chunk chunk = Instantiate(chunkPrefab);
            chunk.transform.parent = chunksParent.transform;
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
        Vector3 playerPos = player.localPosition;

        // Only update chunks if player has moved a certain distance
        if (Vector3.Magnitude(playerPos - playerLastPosition) < 3)
            return;
            
        playerLastPosition = playerPos;

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
                    CreatureSpawning cretureSpawning = chunksHighRes[index].creatures;
                    if (cretureSpawning.initialized) cretureSpawning.GeneratePackSpawns();
                }
                    
            }
            count++;
            index = index == 0 ? chunksHighRes.Count - 1 : index - 1;
        }
    }


    private void GatherWaterPoints()
    {
        float rayOffset = 1f;
        float minRayDist = 1.3f;
        float maxRayDist = 2f;

        float maxRayDistance = (planet.radius - Mathf.Abs(planet.waterDiameter) / 2) + rayOffset;

        Vector3 rayOrigin;
        Vector3 planetCenter = planet.transform.position;
        RaycastHit hit;

        for (int i = 0; i < 150000; i++)
        {
            rayOrigin = planetCenter + rand.OnUnitSphere() * planet.radius;
            Ray ray = new Ray(rayOrigin, planetCenter - rayOrigin);

            if (Physics.Raycast(ray, out hit, maxRayDistance + maxRayDist))
            {
                if (hit.distance > maxRayDistance + minRayDist)
                {
                    //if (DEBUG) Debug.DrawLine(rayOrigin, hit.point, Color.blue, 10);
                    waterPoints.Add(hit.point - planetCenter);
                }
            }
        }

        planet.waterPoints = waterPoints;
    }


    private bool CheckIfPointBIsBelowPointA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0);
    }
}
