using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ChunksHandler : MonoBehaviour
{
    private Planet planet;
    private Transform player;
    private Vector3 playerLastPosition;
    private bool initialized = false;
    private bool resetchunks = false;
    private List<Vector3> chunkPositions;
    private int chunkResolution; //This is 2^chunkResolution
    private MarchingCubes marchingCubes;
    private Material planetMaterial;
    private int resolution;

    [HideInInspector] public bool chunksGenerated;
    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] private GameObject chunksParent;
    [HideInInspector] private List<Chunk> chunks;
    [SerializeField] private TerrainColor terrainColor;
    

    /// <summary>
    /// Initialize the values
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="player"></param>
    public void Initialize(Transform player, Planet planet, MinMaxTerrainLevel terrainLevel, bool spawn)
    {
        this.planet = planet;
        this.player = planet.player;
        playerLastPosition = Vector3.zero;
        marchingCubes = planet.marchingCubes;
        resolution = planet.resolution;

        // If this is the current planet, generate in high res
        if (spawn)
        {
            CreateMeshes(3, terrainLevel);
        }
        else
        {
            CreateMeshes(1, terrainLevel);
        }

        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, 1); //change to random

        // Sets the material of all chuncks
        foreach (Chunk chunk in chunks)
        {
            chunk.SetMaterial(planetMaterial);
        }

        chunkPositions = new List<Vector3>();
        InitializeChunkPositions();
        UpdateChunksVisibility();
        initialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Only update the chunks if the player is close to the planet
        if (initialized && (player.position - planet.transform.position).magnitude < 3000)
        {
            UpdateChunksVisibility();
            resetchunks = false;
        }
        else if (initialized && !resetchunks && (player.position - planet.transform.position).magnitude >= 3000)
        {
            Resetchunks();
        }

        /*
        // Check if player is on the planet
        if (!ReferenceEquals(transform, player.transform.parent))
        {
            MinMaxTerrainLevel needed = new MinMaxTerrainLevel();
            CreateMeshes(1, needed);
            chunksGenerated = false;
        } 
        else
        {
            MinMaxTerrainLevel needed = new MinMaxTerrainLevel();
            CreateMeshes(3, needed);
            chunksGenerated = true;
        }*/
    }

    public void CreateMeshes(int chunkResolution, MinMaxTerrainLevel terrainLevel)
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
        for (int i = 0; i < noChunks; i++)
        {
            Chunk chunk = Instantiate(chunkPrefab);
            chunk.transform.parent = chunksParent.transform;
            chunk.transform.localPosition = Vector3.zero;
            chunk.name = "chunk" + i;
            chunk.Initialize(i, resolution, marchingCubes, player, terrainLevel);
            chunks.Add(chunk);
        }
    }

    private void InitializeChunkPositions()
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            chunkPositions.Add(chunks[i].transform.GetComponent<MeshRenderer>().bounds.center);
        }
    }

    private void Resetchunks()
    {
        for (int i = 0; i < chunkPositions.Count; i++)
        {
            chunks[i].gameObject.SetActive(true);
        }
        resetchunks = true;
    }

    private void UpdateChunksVisibility()
    {
        Vector3 playerPos = player.position;
        Vector3 planetCenter = Vector3.zero;
        Vector3 playerToPlanetCenter = playerPos - planetCenter;
        float planetRadius = planet.radius;

        // Only update chunks if player has moved a certain distance
        if ((Mathf.Abs(Vector3.Distance(playerPos, playerLastPosition)) < 50 || !initialized) && playerToPlanetCenter.magnitude > (planetRadius + 30f))
        {
            return;
        }

        playerLastPosition = playerPos;


        Vector3 cutoffPoint = new Vector3();
        if (playerToPlanetCenter.magnitude > (planetRadius + 30f))
        {
            cutoffPoint = new Vector3(playerToPlanetCenter.x / 10000f, playerToPlanetCenter.y / 10000f, playerToPlanetCenter.z / 10000f);
        }
        else
        {
            cutoffPoint = new Vector3(playerToPlanetCenter.x / 1.5f, playerToPlanetCenter.y / 1.5f, playerToPlanetCenter.z / 1.5f);
        }


        for (int i = 0; i < chunkPositions.Count; i++)
        {
            bool isBelowHalfWayPoint = CheckIfPointBIsBelowPointA(cutoffPoint, chunkPositions[i], cutoffPoint.normalized);
            if (isBelowHalfWayPoint)
            {
                chunks[i].gameObject.SetActive(false);
            }
            else
            {
                chunks[i].gameObject.SetActive(true);
            }
        }
    }

    private bool CheckIfPointBIsBelowPointA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0) ? true : false;
    }
}
