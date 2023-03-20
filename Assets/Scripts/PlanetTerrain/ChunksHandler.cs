using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    private float planetRadius;
    private MinMaxTerrainLevel terrainLevel;

    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] private GameObject chunksParent;
    [HideInInspector] private List<Chunk> chunks;
    [SerializeField] public TerrainColor terrainColor;

    private bool playerOnPlanet;
    private bool updateChunks = false;

    /// <summary>
    /// Initialize the values
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="player"></param>
    public void Initialize(Planet planet, MinMaxTerrainLevel terrainLevel, bool spawn, int seed)
    {
        System.Random rand = new System.Random(seed);

        this.planet = planet;
        player = planet.player;
        playerLastPosition = Vector3.zero;
        marchingCubes = planet.marchingCubes;
        planetRadius = planet.radius;
        this.terrainLevel = terrainLevel;

        playerOnPlanet = spawn;

        // If this is not the spawn planet, all chunks can be created immediately
        if (!playerOnPlanet)
        {
            SetupChunks(1);
            CreateMeshes(1, terrainLevel);
        }
        else
        {
            SetupChunks(3);
            CreateMeshes(planet.resolution, terrainLevel);
        }

        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, rand.Next()); //change to random

        // Sets the material of all chuncks
        setChunksMaterials();
    }

    // Update is called once per frame
    void Update()
    {
        
        if(playerOnPlanet != ReferenceEquals(transform, player.transform.parent))
        {
            updateChunks = true;
            playerOnPlanet = ReferenceEquals(transform, player.transform.parent);
        }

        if (foliageInitialized != 0)
        {
            foliageInitialized--;
        }
        
        // Check if player is on the planet
        if(updateChunks)
        {
            if (!playerOnPlanet)
            {
                SetupChunks(1);
                CreateMeshes(1, terrainLevel);
                setChunksMaterials();
            }
            else
            {
                SetupChunks(3);
                setChunksMaterials();
            }
            updateChunks = false;
        }

        // Only update the chunks if the player is close to the planet
        if (playerOnPlanet)
        {
            UpdateChunksVisibility();
        }

    }

    
    private void SetupChunks(int chunkResolution)
    {
        // Don't create new ones if they are to be the same as old ones.
        if (chunkResolution == this.chunkResolution)
        {
            return;
        }
        this.chunkResolution = chunkResolution;

        Destroy(chunksParent);

        chunksParent = new GameObject();
        chunksParent.name = "chunks";
        chunksParent.transform.parent = transform;
        chunksParent.transform.localPosition = Vector3.zero;

        marchingCubes.chunkResolution = chunkResolution;

        // Create all chunks
        chunks = new List<Chunk>();
        int noChunks = (1 << chunkResolution) * (1 << chunkResolution) * (1 << chunkResolution);
        for (int i = 0; i < noChunks; i++)
        {
            Chunk chunk = Instantiate(chunkPrefab);
            chunk.transform.parent = chunksParent.transform;
            chunk.transform.localPosition = Vector3.zero;
            chunk.name = "chunk" + i;
            chunk.Setup(i, marchingCubes);
            chunks.Add(chunk);  
        }
    }

    private void setChunksMaterials()
    {
        foreach(Chunk chunk in chunks)
        {
            chunk.SetMaterial(planetMaterial);
        }
    }

    private void CreateMeshes(int resolution, MinMaxTerrainLevel terrainLevel)
    {
        for(int i = chunks.Count - 1; i != -1; i--)
        {
            if (chunks[i].Initialize(resolution, player, terrainLevel) == 0)
            {
                Destroy(chunks[i].gameObject);
                chunks.RemoveAt(i);
            }
        }
    }
    private void UpdateChunksVisibility()
    {
        Vector3 playerPos = player.position;
        Vector3 planetCenter = Vector3.zero;
        Vector3 playerToPlanetCenter = playerPos - planetCenter;

        // Only update chunks if player has moved a certain distance
        if ((Mathf.Abs(Vector3.Distance(playerPos, playerLastPosition)) < 50) && playerToPlanetCenter.magnitude > (planetRadius + 30f))
        {
            return;
        }

        playerLastPosition = playerPos;

        Vector3 cutoffPoint;
        if (playerToPlanetCenter.magnitude > (planetRadius + 30f))
        {
            cutoffPoint = new Vector3(playerToPlanetCenter.x / 10000f, playerToPlanetCenter.y / 10000f, playerToPlanetCenter.z / 10000f);
        }
        else
        {
            cutoffPoint = new Vector3(playerToPlanetCenter.x / 1.5f, playerToPlanetCenter.y / 1.5f, playerToPlanetCenter.z / 1.5f);
        }

        for(int i = chunks.Count - 1; i != -1; i--)
        {
            bool isBelowHalfWayPoint = CheckIfPointBIsBelowPointA(cutoffPoint, chunks[i].position, cutoffPoint.normalized);
            if (isBelowHalfWayPoint)
            {
                chunks[i].gameObject.SetActive(false);
            }
            else
            {
                chunks[i].gameObject.SetActive(true);
                if(!chunks[i].initialized)
                {
                    if(chunks[i].Initialize(planet.resolution, player, terrainLevel) == 0)
                    {
                        Destroy(chunks[i].gameObject);
                        chunks.RemoveAt(i);
                    }
                    else
                    {
                        if (foliageInitialized == 0)
                            chunks[i].foliage.SpawnFoliageOnChunk();
                    }
                }
                
            }
        }
    }

    private bool CheckIfPointBIsBelowPointA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0);
    }
}
