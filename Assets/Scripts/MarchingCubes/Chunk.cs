using ExtendedRandom;
using UnityEngine;
using static ChunksHandler;

public class Chunk : MonoBehaviour
{
    [SerializeField] public CreatureSpawning creatures;
    [SerializeField] public Foliage foliage;

    [SerializeField] private GameObject foliageGameObject;
    [SerializeField] public GameObject creatureGameObject;

    private int index;

    // Resolutions of chunk
    private int currentRes;
    private int highRes;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    public MarchingCubes marchingCubes;
    private Planet planet;
    [HideInInspector] public float chunkSize;

    private RandomX random;

    [HideInInspector] public Vector3 position;
    [HideInInspector] public bool initialized = false;

    public void Setup(int index, MarchingCubes marchingCubes)
    {
        this.index = index;
        this.marchingCubes = marchingCubes;

        CalculateChunkPosition();
    }

    public void Initialize(Planet planet, ChunksHandler chunkHandler, int seed)
    {
        this.planet = planet;
        highRes = chunkHandler.highRes.resolution;
        random = new RandomX(seed);

        chunkSize = (2 * chunkHandler.planetRadius) / (1 << marchingCubes.chunkResolution);

        meshFilter = transform.GetComponent<MeshFilter>();
        meshRenderer = transform.GetComponent<MeshRenderer>();
        if(marchingCubes.chunkResolution == 1)
        {
            GetComponent<MeshCollider>().enabled = false;
        }
        else
        {
            meshCollider = transform.GetComponent<MeshCollider>();
            meshCollider.enabled = false; // Disabled until highest res is loaded
        }

        initialized = true;
    }

    public void SetMaterial(Material material) {
        meshRenderer.sharedMaterial = material;
    }

    private void CalculateChunkPosition()
    {
        int mask = 0;
        for (int i = 0; i < marchingCubes.chunkResolution; i++)
        {
            mask += 1 << i;
        }

        Vector3 chunkIndex =
            new Vector3(
                (index & (mask << (marchingCubes.chunkResolution * 0))) >> (marchingCubes.chunkResolution * 0),
                (index & (mask << (marchingCubes.chunkResolution * 1))) >> (marchingCubes.chunkResolution * 1),
                (index & (mask << (marchingCubes.chunkResolution * 2))) >> (marchingCubes.chunkResolution * 2)
            );

        position = -(chunkIndex - (Mathf.Pow(2, marchingCubes.chunkResolution) - 1) / 2 * Vector3.one) * (marchingCubes.radius * 2 / (1 << (marchingCubes.chunkResolution)));
    }

    public void UpdateMesh(Mesh mesh, int resolution)
    {
        currentRes = resolution;

        Destroy(meshFilter.sharedMesh);

        meshFilter.sharedMesh = mesh;

        // Only enable collider and assign physics mesh at highest resolution
        if (meshCollider != null)
        {
            if (resolution == highRes)
            {
                meshCollider.enabled = true;
                meshCollider.sharedMesh = mesh;
            }
            else
            {
                meshCollider.enabled = false;
            }
        }

        int numVerts = mesh.vertexCount;

        if (resolution == highRes && planet.willGeneratePlanetLife)
        {
            if (!foliage.initialized)
            {
                if (numVerts > 500)
                    foliage.Initialize(numVerts, position, random.Next(), planet);
            }
            if (!creatures.initialized)
            {
                if (numVerts > 500)
                    creatures.Initialize(numVerts, position, random.Next());
            }
        }
    }

    public void SetActivated(bool state)
    {
        // Don't enable collider here — UpdateMesh handles it at highest resolution only
        foliageGameObject.SetActive(state);
        creatureGameObject.SetActive(state);
    }

    public void MarkResolution(int resolution)
    {
        currentRes = resolution;
    }

    public bool HasResolution(int resolution)
    {
        return currentRes == resolution;
    }

    public int Index
    {
        get { return index; }
    }
}
