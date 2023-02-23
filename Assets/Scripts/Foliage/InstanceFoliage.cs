using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InstanceFoliage
{
    // List of planes that make up the cameras view
    private static Plane[] planes;
    private static Material foliageMaterial;

    private static MaterialPropertyBlock block;
    private static List<Matrix4x4[]> renderedPositionsTrees = new List<Matrix4x4[]>();
    private static List<Matrix4x4[]> renderedPositionsRocks = new List<Matrix4x4[]>();

    private static Mesh[] treeMeshes;
    private static Mesh[] rockMeshes;
    private static bool isDirty;
    private static Camera lastUsedCamera;
    private static Vector3 lastUsedCameraPosition;
    private static Quaternion lastUsedCameraRotation;

    private static bool instanceFoliage;

    private static List<int> meshesIndexforTreePosition = new List<int>();
    private static List<int> meshesIndexforRockPosition = new List<int>();

    private static List<int> indexSeperationBetweenMeshesTrees = new List<int>();
    private static List<int> indexSeperationBetweenMeshesRocks = new List<int>();

    // Start is called before the first frame update
    void Start()
    {
        block = new MaterialPropertyBlock();
        planes = new Plane[6];
        treeMeshes = prefabMeshes[0];
        rockMeshes = prefabMeshes[1];
    }

    // Update is called once per frame
    public static void Run()
    {
        if (!instanceFoliage)
        {
            return;
        }

        DrawInstances();

    }

    private static void DrawInstances()
    {
        

        for (int j = 0; j < indexSeperationBetweenMeshesTrees.Count; j++)
        {
            for (int i = 0; i < renderedPositionsTrees.Count; i++)
            {
                if (i < indexSeperationBetweenMeshesTrees[j])
                {
                    Graphics.DrawMeshInstanced(treeMeshes[j], 0, foliageMaterial, renderedPositionsTrees[i], renderedPositionsTrees[i].Length, block);
                }
            }
        }

        for (int j = 0; j < indexSeperationBetweenMeshesRocks.Count; j++)
        {
            for (int i = 0; i < renderedPositionsRocks.Count; i++)
            {
                if (i < indexSeperationBetweenMeshesRocks[j])
                {
                    Graphics.DrawMeshInstanced(rockMeshes[j], 0, foliageMaterial, renderedPositionsRocks[i], renderedPositionsRocks[i].Length, block);
                }
            }
        }

        /*
        for (int i = 0; i < renderedPositionsRocks.Count; i++)
        {
            for (int j = 0; j < indexSeperationBetweenMeshesRocks.Count; j++)
            {
                if (i < indexSeperationBetweenMeshesRocks[j])
                {
                    Graphics.DrawMeshInstanced(rockMeshes[j], 0, foliageMaterial, renderedPositionsRocks[j], renderedPositionsRocks[j].Length, block);
                }
            }
        }
        for (int i = 0; i < treeMeshes.Length; i++)
        {
            for (int j = 0; j < renderedPositionsTrees.Count; j++)
            {
                Graphics.DrawMeshInstanced(treeMeshes[i], 0, foliageMaterial, renderedPositionsTrees[j], renderedPositionsTrees[j].Length, block);
            }
        }
        for (int i = 0; i < rockMeshes.Length; i++)
        {
            for (int j = 0; j < renderedPositionsRocks.Count; j++)
            {
                Graphics.DrawMeshInstanced(rockMeshes[0], 0, foliageMaterial, renderedPositionsRocks[j], renderedPositionsRocks[j].Length, block);
            }
        }
        */
    }

    public static void SetInstancingData(GameObject[] trees, GameObject[] rocks, Material material)
    {
        block = new MaterialPropertyBlock();
        planes = new Plane[6];
        instanceFoliage = false;

        treeMeshes = new Mesh[trees.Length];
        rockMeshes = new Mesh[rocks.Length];
        foliageMaterial = material;

        for (int i = 0; i < treeMeshes.Length; i++)
        {
            treeMeshes[i] = trees[i].GetComponent<MeshFilter>().sharedMesh;
        }
        for (int i = 0; i < rockMeshes.Length; i++)
        {
            rockMeshes[i] = rocks[i].GetComponent<MeshFilter>().sharedMesh;
        }
    }
    public static void CalculateMatrices(List<Vector3> positionsTrees, List<Quaternion> rotationsTrees, List<Vector3> positionsRocks, List<Quaternion> rotationsRocks)
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

        RandomizeMeshesForPositions(positionsTrees.Count, positionsRocks.Count);
        GroupMatrices(treesMatrices, rocksMatrices);
    }

    private static void RandomizeMeshesForPositions(int treesCount, int rocksCount)
    {
        int amountOfTreeMeshes = treeMeshes.Length;
        int amountOfRockMeshes = rockMeshes.Length;

        for (int i = 0; i < treesCount; i++)
        {
            int meshIndex = Universe.random.Next(0, amountOfTreeMeshes);
            meshesIndexforTreePosition.Add(meshIndex);
        }
        for (int i = 0; i < rocksCount; i++)
        {
            int meshIndex = Universe.random.Next(0, amountOfRockMeshes);
            meshesIndexforRockPosition.Add(meshIndex);
        }
    }

    private static void GroupMatrices(List<Matrix4x4> treesMatrices, List<Matrix4x4> rocksMatrices)
    {
        List<Matrix4x4> treesGroup = new List<Matrix4x4>();
        List<Matrix4x4> rocksGroup = new List<Matrix4x4>();

        int treesCount = 0;
        int rocksCount = 0;

        int numberOfTreemeshes = treeMeshes.Length;
        int numberOfRockmeshes = rockMeshes.Length;
        

        for (int j = 0; j < numberOfTreemeshes; j++)
        {
            for (int i = 0; i < meshesIndexforTreePosition.Count; i++)
            {
                if (meshesIndexforTreePosition[i] == j)
                {
                    if ((treesCount % 1023) == 0 && treesCount > 0)
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
            }
            indexSeperationBetweenMeshesTrees.Add(renderedPositionsTrees.Count);
        }

        for (int j = 0; j < numberOfRockmeshes; j++)
        {
            for (int i = 0; i < meshesIndexforRockPosition.Count; i++)
            {
                if (meshesIndexforRockPosition[i] == j)
                {
                    if ((rocksCount % 1023) == 0 && rocksCount > 0)
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
            }
            indexSeperationBetweenMeshesRocks.Add(renderedPositionsRocks.Count);
        }

        instanceFoliage = true;
    }
}
