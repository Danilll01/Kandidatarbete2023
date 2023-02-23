using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceFoliage : MonoBehaviour
{
    // List of planes that make up the cameras view
    private Plane[] planes;
    [SerializeField] private Material foliageMaterial;

    private MaterialPropertyBlock block;
    private List<Matrix4x4[]> renderedPositionsTrees = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> renderedPositionsRocks = new List<Matrix4x4[]>();

    [SerializeField] private Mesh[] prefabMeshes;
    private Mesh treeMeshes;
    private Mesh rockMeshes;
    private bool isDirty;
    private Camera lastUsedCamera;
    private Vector3 lastUsedCameraPosition;
    private Quaternion lastUsedCameraRotation;

    private bool instanceFoliage;



    // Start is called before the first frame update
    void Start()
    {
        block = new MaterialPropertyBlock();
        planes = new Plane[6];
        treeMeshes = prefabMeshes[0];
        rockMeshes = prefabMeshes[1];
    }

    // Update is called once per frame
    void Update()
    {
        if (!instanceFoliage)
        {
            return;
        }

        DrawInstances();

    }

    private void DrawInstances()
    {
        for (int i = 0; i < renderedPositionsTrees.Count; i++)
        {
            Graphics.DrawMeshInstanced(treeMeshes, 0, foliageMaterial, renderedPositionsTrees[i], renderedPositionsTrees[i].Length, block);
        }
        for (int i = 0; i < renderedPositionsRocks.Count; i++)
        {
            Graphics.DrawMeshInstanced(rockMeshes, 0, foliageMaterial, renderedPositionsRocks[i], renderedPositionsRocks[i].Length, block);
        }
    }

    public void CalculateMatrices(List<Vector3> positionsTrees, List<Quaternion> rotationsTrees, List<Vector3> positionsRocks, List<Quaternion> rotationsRocks)
    {
        instanceFoliage = false;
        List<Matrix4x4> treesMatrices = new List<Matrix4x4>();
        List<Matrix4x4> rocksMatrices = new List<Matrix4x4>();

        for (int i = 0; i < positionsTrees.Count; i++)
        {
            treesMatrices.Add(Matrix4x4.TRS(positionsTrees[i], rotationsTrees[i], Vector3.one));
        }
        for (int i = 0; i < positionsRocks.Count; i++)
        {
            rocksMatrices.Add(Matrix4x4.TRS(positionsRocks[i], rotationsRocks[i], Vector3.one));
        }

        GroupMatrices(treesMatrices, rocksMatrices);
    }

    private void GroupMatrices(List<Matrix4x4> treesMatrices, List<Matrix4x4> rocksMatrices)
    {
        List<Matrix4x4> treesGroup = new List<Matrix4x4>();
        List<Matrix4x4> rocksGroup = new List<Matrix4x4>();

        int treesCount = 0;
        int rocksCount = 0;

        for (int i = 0; i < treesMatrices.Count; i++)
        {
            if ((i % 1023) == 0 && treesCount > 0)
            {
                renderedPositionsTrees.Add(treesGroup.ToArray());
                treesGroup.Clear();
            }
            else if (i == treesMatrices.Count - 1)
            {
                renderedPositionsTrees.Add(treesGroup.ToArray());
            }

            treesGroup.Add(treesMatrices[i]);
            treesCount++;
        }
        for (int i = 0; i < rocksMatrices.Count; i++)
        {
            if ((i % 1023) == 0 && rocksCount > 0)
            {
                renderedPositionsRocks.Add(rocksGroup.ToArray());
                rocksGroup.Clear();
            }
            else if (i == rocksMatrices.Count - 1)
            {
                renderedPositionsRocks.Add(rocksGroup.ToArray());
            }
            rocksGroup.Add(rocksMatrices[i]);
            rocksCount++;
        }

        instanceFoliage = true;
    }
}
