using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// What is needed to draw a foliage prefab with instancing, and a pool of collider-only copies of it
/// </summary>
public class FoliagePrefabInfo
{
    private static readonly Dictionary<GameObject, FoliagePrefabInfo> cache = new Dictionary<GameObject, FoliagePrefabInfo>();
    private static Transform poolRoot;

    // Prefabs with scripts (like food bushes) stay GameObjects since other code needs to find and change them
    public bool instanced;
    public Mesh mesh;
    public int layer;
    public ShadowCastingMode shadowCastingMode;
    public bool receiveShadows;
    // Replaces the prefab's LODGroup, which only culled the object when it got small on screen
    public float lodSize;
    public float cullScreenHeight;
    public bool hasColliders;

    private readonly GameObject prefab;
    private GameObject colliderTemplate;
    private readonly Stack<GameObject> pool = new Stack<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache.Clear();
        poolRoot = null;
    }

    public static FoliagePrefabInfo Get(GameObject prefab)
    {
        if (!cache.TryGetValue(prefab, out FoliagePrefabInfo info))
        {
            info = new FoliagePrefabInfo(prefab);
            cache[prefab] = info;
        }
        return info;
    }

    private FoliagePrefabInfo(GameObject prefab)
    {
        this.prefab = prefab;

        MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();
        bool singleRenderer = prefab.GetComponentsInChildren<Renderer>(true).Length == 1;
        bool hasScripts = prefab.GetComponentInChildren<MonoBehaviour>(true) != null;

        instanced = meshFilter != null && meshFilter.sharedMesh != null && meshRenderer != null && singleRenderer && !hasScripts;
        if (!instanced) return;

        mesh = meshFilter.sharedMesh;
        layer = prefab.layer;
        shadowCastingMode = meshRenderer.shadowCastingMode;
        receiveShadows = meshRenderer.receiveShadows;
        hasColliders = prefab.GetComponentInChildren<Collider>(true) != null;

        if (prefab.TryGetComponent(out LODGroup lodGroup))
        {
            LOD[] lods = lodGroup.GetLODs();
            lodSize = lodGroup.size;
            cullScreenHeight = lods.Length > 0 ? lods[lods.Length - 1].screenRelativeTransitionHeight : 0;
        }
    }

    /// <summary>
    /// Gets an active copy of the prefab that only has its colliders
    /// </summary>
    public GameObject RentCollider(Transform parent)
    {
        while (pool.Count > 0)
        {
            GameObject pooled = pool.Pop();
            if (pooled == null) continue;

            pooled.transform.SetParent(parent, false);
            pooled.SetActive(true);
            return pooled;
        }

        if (colliderTemplate == null) CreateColliderTemplate();

        GameObject copy = Object.Instantiate(colliderTemplate, parent, false);
        copy.SetActive(true);
        return copy;
    }

    public void ReturnCollider(GameObject copy)
    {
        if (copy == null) return;

        copy.SetActive(false);
        copy.transform.SetParent(GetPoolRoot(), false);
        pool.Push(copy);
    }

    private void CreateColliderTemplate()
    {
        colliderTemplate = Object.Instantiate(prefab, GetPoolRoot(), false);
        colliderTemplate.SetActive(false);
        colliderTemplate.name = prefab.name + " (Collider)";

        // Removed immediately so copies made this frame don't get them
        foreach (LODGroup lodGroup in colliderTemplate.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(lodGroup);
        foreach (Renderer renderer in colliderTemplate.GetComponentsInChildren<Renderer>(true)) Object.DestroyImmediate(renderer);
        foreach (MeshFilter filter in colliderTemplate.GetComponentsInChildren<MeshFilter>(true)) Object.DestroyImmediate(filter);
    }

    private static Transform GetPoolRoot()
    {
        if (poolRoot == null)
        {
            GameObject root = new GameObject("Foliage Collider Pool");
            root.SetActive(false);
            poolRoot = root.transform;
        }
        return poolRoot;
    }
}

/// <summary>
/// The foliage of one chunk, drawn with GPU instancing. Colliders are only created close to the player.
/// </summary>
public class FoliageInstances
{
    private class Batch
    {
        public FoliagePrefabInfo info;
        public Material material;
        public readonly List<Matrix4x4> localMatrices = new List<Matrix4x4>();
        public readonly List<Matrix4x4> worldMatrices = new List<Matrix4x4>();
        public Bounds worldBounds;
        public float maxScale;
    }

    private struct ColliderInstance
    {
        public FoliagePrefabInfo info;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public GameObject collider;
    }

    // How far beyond the collider radius a collider is kept, so they aren't swapped every frame on the edge
    private const float colliderHysteresis = 10f;

    private readonly Dictionary<(Mesh, Material), Batch> batchLookup = new Dictionary<(Mesh, Material), Batch>();
    private readonly List<Batch> batches = new List<Batch>();
    private readonly List<ColliderInstance> colliders = new List<ColliderInstance>();
    private Matrix4x4 lastLocalToWorld;
    private bool worldMatricesValid;
    private int activeColliders;

    public int Count { get; private set; }

    public bool HasActiveColliders => activeColliders > 0;

    public void Add(FoliagePrefabInfo info, Material material, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        if (!material.enableInstancing) material.enableInstancing = true;

        if (!batchLookup.TryGetValue((info.mesh, material), out Batch batch))
        {
            batch = new Batch { info = info, material = material };
            batchLookup[(info.mesh, material)] = batch;
            batches.Add(batch);
        }

        Matrix4x4 local = Matrix4x4.TRS(localPosition, localRotation, localScale);
        batch.localMatrices.Add(local);
        batch.maxScale = Mathf.Max(batch.maxScale, localScale.x, localScale.y, localScale.z);
        if (worldMatricesValid) AddWorldMatrix(batch, lastLocalToWorld * local);

        if (info.hasColliders)
        {
            colliders.Add(new ColliderInstance
            {
                info = info,
                localPosition = localPosition,
                localRotation = localRotation,
                localScale = localScale
            });
        }

        Count++;
    }

    /// <summary>
    /// Draws every batch that is close enough to the camera
    /// </summary>
    public void Draw(Transform parent, Camera camera)
    {
        if (batches.Count == 0) return;

        Matrix4x4 localToWorld = parent.localToWorldMatrix;
        if (!worldMatricesValid || localToWorld != lastLocalToWorld)
        {
            RecalculateWorldMatrices(localToWorld);
        }

        Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
        float lodScale = camera != null ? QualitySettings.lodBias / (2 * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) : 0;

        foreach (Batch batch in batches)
        {
            FoliagePrefabInfo info = batch.info;
            if (camera != null && info.cullScreenHeight > 0)
            {
                float cullDistance = info.lodSize * batch.maxScale * lodScale / info.cullScreenHeight;
                if (batch.worldBounds.SqrDistance(cameraPosition) > cullDistance * cullDistance) continue;
            }

            RenderParams renderParams = new RenderParams(batch.material)
            {
                layer = info.layer,
                shadowCastingMode = info.shadowCastingMode,
                receiveShadows = info.receiveShadows,
                worldBounds = batch.worldBounds
            };
            Graphics.RenderMeshInstanced(renderParams, info.mesh, 0, batch.worldMatrices);
        }
    }

    /// <summary>
    /// Creates colliders for the foliage within radius of a world position and removes the ones that are far away
    /// </summary>
    public void UpdateColliders(Transform parent, Vector3 worldPosition, float radius)
    {
        if (colliders.Count == 0) return;

        Vector3 localPosition = parent.InverseTransformPoint(worldPosition);
        float addDistanceSqr = radius * radius;
        float removeDistanceSqr = (radius + colliderHysteresis) * (radius + colliderHysteresis);

        for (int i = 0; i < colliders.Count; i++)
        {
            ColliderInstance instance = colliders[i];
            float distanceSqr = (instance.localPosition - localPosition).sqrMagnitude;

            if (instance.collider == null && distanceSqr < addDistanceSqr)
            {
                instance.collider = instance.info.RentCollider(parent);
                Transform colliderTransform = instance.collider.transform;
                colliderTransform.localPosition = instance.localPosition;
                colliderTransform.localRotation = instance.localRotation;
                colliderTransform.localScale = instance.localScale;
                colliders[i] = instance;
                activeColliders++;
            }
            else if (instance.collider != null && distanceSqr > removeDistanceSqr)
            {
                instance.info.ReturnCollider(instance.collider);
                instance.collider = null;
                colliders[i] = instance;
                activeColliders--;
            }
        }
    }

    private void RecalculateWorldMatrices(Matrix4x4 localToWorld)
    {
        lastLocalToWorld = localToWorld;
        worldMatricesValid = true;

        foreach (Batch batch in batches)
        {
            batch.worldMatrices.Clear();
            batch.worldBounds = new Bounds();
            foreach (Matrix4x4 local in batch.localMatrices)
            {
                AddWorldMatrix(batch, localToWorld * local);
            }
        }
    }

    private static void AddWorldMatrix(Batch batch, Matrix4x4 world)
    {
        // Conservative bounds of the instance: a sphere around the mesh bounds, scaled by the largest scale
        Bounds meshBounds = batch.info.mesh.bounds;
        Vector3 scale = world.lossyScale;
        float radius = meshBounds.extents.magnitude * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Bounds instanceBounds = new Bounds(world.MultiplyPoint3x4(meshBounds.center), Vector3.one * (radius * 2));

        if (batch.worldMatrices.Count == 0) batch.worldBounds = instanceBounds;
        else batch.worldBounds.Encapsulate(instanceBounds);

        batch.worldMatrices.Add(world);
    }
}
