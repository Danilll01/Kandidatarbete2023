using ExtendedRandom;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [SerializeField] public CreatureSpawning creatures;
    [SerializeField] public Foliage foliage;

    [SerializeField] private GameObject foliageGameObject;
    [SerializeField] public GameObject creatureGameObject;

    private int index;
    private int chunkResolution;

    // Resolutions of chunk
    private int currentRes;
    private int highRes;
    private int colliderRes;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Planet planet;
    [HideInInspector] public float chunkSize;

    private RandomX random;

    [HideInInspector] public Vector3 position;
    [HideInInspector] public bool initialized = false;

    public void Setup(int index, int chunkResolution, float planetRadius)
    {
        this.index = index;
        this.chunkResolution = chunkResolution;

        position = CalculatePosition(index, chunkResolution, planetRadius);
    }

    public void Initialize(Planet planet, ChunksHandler chunkHandler, int seed)
    {
        this.planet = planet;
        highRes = chunkHandler.highRes.resolution;
        colliderRes = chunkHandler.mediumRes.resolution;
        random = new RandomX(seed);

        chunkSize = (2 * chunkHandler.planetRadius) / (1 << chunkResolution);

        meshFilter = transform.GetComponent<MeshFilter>();
        meshRenderer = transform.GetComponent<MeshRenderer>();
        if (chunkResolution == 1)
        {
            GetComponent<MeshCollider>().enabled = false;
        }
        else
        {
            meshCollider = transform.GetComponent<MeshCollider>();
            meshCollider.enabled = false; // Disabled until a mesh detailed enough for collisions is loaded
        }

        initialized = true;
    }

    /// <summary>
    /// Local position of the centre of a chunk in a planet split into 2^chunkResolution chunks per axis
    /// </summary>
    public static Vector3 CalculatePosition(int index, int chunkResolution, float planetRadius)
    {
        int mask = (1 << chunkResolution) - 1;

        Vector3 chunkIndex = new Vector3(
            index & mask,
            (index >> chunkResolution) & mask,
            (index >> (chunkResolution * 2)) & mask
        );

        return -(chunkIndex - ((1 << chunkResolution) - 1) / 2f * Vector3.one) * (planetRadius * 2 / (1 << chunkResolution));
    }

    public void SetMaterial(Material material) {
        meshRenderer.sharedMaterial = material;
    }

    public void UpdateMesh(Mesh mesh, int resolution)
    {
        currentRes = resolution;

        Destroy(meshFilter.sharedMesh);

        meshFilter.sharedMesh = mesh;

        // Medium and high resolution meshes get colliders so the ground is solid before the most detailed mesh is loaded
        bool hasCollider = meshCollider != null && resolution >= colliderRes;
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = hasCollider ? mesh : null;
            meshCollider.enabled = hasCollider;
        }

        int numVerts = mesh.vertexCount;

        // Nothing reads the vertices of meshes without colliders again, so free their CPU copy
        if (!hasCollider) mesh.UploadMeshData(true);

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

    public void MarkResolution(int resolution)
    {
        currentRes = resolution;
    }

    public void SetActivated(bool state)
    {
        foliageGameObject.SetActive(state);
        creatureGameObject.SetActive(state);
    }

    public bool HasResolution(int resolution)
    {
        return currentRes == resolution;
    }

    public int CurrentResolution => currentRes;

    public int Index
    {
        get { return index; }
    }
}
