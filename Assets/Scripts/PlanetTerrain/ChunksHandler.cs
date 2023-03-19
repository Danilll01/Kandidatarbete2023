using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
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

    [HideInInspector] public bool chunksGenerated;
    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] private GameObject chunksParent;
    [HideInInspector] private List<Chunk> chunks;
    [SerializeField] public TerrainColor terrainColor;

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

        // If this is not the spawn planet, all chunks can be created immediately
        if (!spawn)
        {
            CreateMeshes(1, 1, terrainLevel);
            chunksGenerated = false;
        }
        else
        {

        }

        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, rand.Next()); //change to random

        // Sets the material of all chuncks
        setChunksMaterials();
    }

    // Update is called once per frame
    void Update()
    {
        bool playerOnPlanet = ReferenceEquals(transform, player.transform.parent);

        if (foliageInitialized != 0)
        {
            foliageInitialized--;
        }

        // Only update the chunks if the player is close to the planet
        if (playerOnPlanet)
        {
            UpdateChunksVisibility();
        }

        // Check if player is on the planet
        if (!playerOnPlanet)
        {
            CreateMeshes(1, 1, terrainLevel);
            setChunksMaterials();
            chunksGenerated = false;
        } 
        else if(!chunksGenerated)
        {
            CreateMeshes(3, planet.resolution, terrainLevel);
            setChunksMaterials();
            chunksGenerated = true;
        }
    }

    private void SetupChunks(int chunkResolution)
    {
        // Don't create new ones if they are to be the same as old ones.
        if (this.chunkResolution == chunkResolution)
            return;
        this.chunkResolution = chunkResolution;

        chunks = new List<Chunk>;

        for(int i = 0; i <chunkResolution; i++)
        {
            Chunk chunk = Instantiate(chunkPrefab);

        }
    }

    private void setChunksMaterials()
    {
        foreach(Chunk chunk in chunks)
        {
            chunk.SetMaterial(planetMaterial);
        }
    }

    private void CreateMeshes(int chunkResolution, int resolution, MinMaxTerrainLevel terrainLevel)
    {
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
        int chunkNumber = 0;
        for (int i = 0; i < noChunks; i++)
        {
            Chunk chunk = Instantiate(chunkPrefab);
            chunk.transform.parent = chunksParent.transform;
            chunk.transform.localPosition = Vector3.zero;
            chunk.name = "chunk" + chunkNumber;

            //Don't add chunk if it's empty
            if (chunk.Initialize(i, resolution, marchingCubes, player, terrainLevel) == 0)
            {
                Destroy(chunk.gameObject);
            }
            else
            {
                chunkNumber++;
                chunks.Add(chunk);
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


        foreach (Chunk chunk in chunks)
        {
            bool isBelowHalfWayPoint = CheckIfPointBIsBelowPointA(cutoffPoint, chunk.position, cutoffPoint.normalized);
            if (isBelowHalfWayPoint)
            {
                chunk.gameObject.SetActive(false);
            }
            else
            {
                chunk.gameObject.SetActive(true);
                if(foliageInitialized == 0)
                    chunk.foliage.SpawnFoliageOnChunk();
            }
        }
    }

    private bool CheckIfPointBIsBelowPointA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0);
    }
}
