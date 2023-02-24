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

    private static List<Vector3> oldCulledPositionsTrees = new List<Vector3>();
    private static List<Vector3> oldCulledPositionsRocks = new List<Vector3>();

    private static List<Vector3> culledPositionsForTrees = new List<Vector3>();
    private static List<Vector3> culledPositionsForRocks = new List<Vector3>();

    private static List<Vector3> positionsForTrees = new List<Vector3>();
    private static List<Vector3> positionsForRocks = new List<Vector3>();

    private static List<Quaternion> culledRotationsForTrees = new List<Quaternion>();
    private static List<Quaternion> culledRotationsForRocks = new List<Quaternion>();

    private static List<Quaternion> rotationsForTrees = new List<Quaternion>();
    private static List<Quaternion> rotationsForRocks = new List<Quaternion>();

    private static Mesh[] treeMeshes;
    private static Mesh[] rockMeshes;
    private static Camera camera;
    private static Vector3 lastCameraPosition;
    private static Quaternion lastCameraRotation;

    public static bool instanceFoliage = false;

    private static List<int> meshesIndexforTreePosition = new List<int>();
    private static List<int> meshesIndexforRockPosition = new List<int>();

    private static List<int> indexSeperationBetweenMeshesTrees = new List<int>();
    private static List<int> indexSeperationBetweenMeshesRocks = new List<int>();
    private static Planet currentPlanet;


    // Update is called once per frame
    public static void Run()
    {
        if (!instanceFoliage)
        {
            return;
        }

        //CalculateFrustumPlanes();
        CalculatePositionsToRender();
        CalculateMatrices();
        DrawInstances();

    }

    public static void SetInstancingData(GameObject[] trees, GameObject[] rocks, Material material, Planet planet, List<Vector3> positionsTrees, List<Quaternion> rotationsTrees, List<Vector3> positionsRocks, List<Quaternion> rotationsRocks)
    {
        block = new MaterialPropertyBlock();
        planes = new Plane[6];

        treeMeshes = new Mesh[trees.Length];
        rockMeshes = new Mesh[rocks.Length];
        foliageMaterial = material;
        camera = Camera.main;

        currentPlanet = planet;

        renderedPositionsTrees.Clear();
        renderedPositionsRocks.Clear();
        oldCulledPositionsTrees.Clear();
        oldCulledPositionsRocks.Clear();
        culledPositionsForTrees.Clear();
        culledPositionsForRocks.Clear();
        culledRotationsForTrees.Clear();
        culledRotationsForRocks.Clear();
        positionsForTrees.Clear();
        positionsForRocks.Clear();
        rotationsForTrees.Clear();
        rotationsForRocks.Clear();
        meshesIndexforTreePosition.Clear();
        meshesIndexforRockPosition.Clear();

        positionsForTrees = positionsTrees;
        positionsForRocks = positionsRocks;
        rotationsForTrees = rotationsTrees;
        rotationsForRocks = rotationsRocks;

        for (int i = 0; i < treeMeshes.Length; i++)
        {
            treeMeshes[i] = trees[i].GetComponent<MeshFilter>().sharedMesh;
        }
        for (int i = 0; i < rockMeshes.Length; i++)
        {
            rockMeshes[i] = rocks[i].GetComponent<MeshFilter>().sharedMesh;
        }

        RandomizeMeshesForPositions();

    }

    private static void DrawInstances()
    {
        if (indexSeperationBetweenMeshesTrees.Count == 0)
        {
            for (int i = 0; i < renderedPositionsTrees.Count; i++)
            {
                Graphics.DrawMeshInstanced(treeMeshes[meshesIndexforTreePosition[i]], 0, foliageMaterial, renderedPositionsTrees[i], renderedPositionsTrees[i].Length, block);
            }
        }
        int startIndex = 0;
        for (int j = 0; j < indexSeperationBetweenMeshesTrees.Count; j++)
        {
            for (int i = startIndex; i < renderedPositionsTrees.Count; i++)
            {
                if (i == indexSeperationBetweenMeshesTrees[j])
                {
                    startIndex = i;
                    break;
                }
                else if (j == 0 && i < indexSeperationBetweenMeshesTrees[j])
                {
                    Graphics.DrawMeshInstanced(treeMeshes[j], 0, foliageMaterial, renderedPositionsTrees[i], renderedPositionsTrees[i].Length, block);
                }
                else if (i < indexSeperationBetweenMeshesTrees[j] && i >= indexSeperationBetweenMeshesTrees[j - 1])
                {
                    if (j >= treeMeshes.Length || i >= renderedPositionsTrees.Count)
                    {
                        Debug.Log("Error");
                    }
                    //Graphics.DrawMeshInstanced(treeMeshes[j], 0, foliageMaterial, renderedPositionsTrees[i], renderedPositionsTrees[i].Length, block);
                }
            }
        }

        /*
        if (indexSeperationBetweenMeshesRocks.Count == 0)
        {
            for (int i = 0; i < renderedPositionsRocks.Count; i++)
            {
                Graphics.DrawMeshInstanced(rockMeshes[meshesIndexforRockPosition[i]], 0, foliageMaterial, renderedPositionsRocks[i], renderedPositionsRocks[i].Length, block);
            }
        }
        for (int j = 0; j < indexSeperationBetweenMeshesRocks.Count; j++)
        {
            for (int i = 0; i < renderedPositionsRocks.Count; i++)
            {
                if (j == 0 && i < indexSeperationBetweenMeshesRocks[j])
                {
                    Graphics.DrawMeshInstanced(rockMeshes[j], 0, foliageMaterial, renderedPositionsRocks[i], renderedPositionsRocks[i].Length, block);
                }
                else if (i < indexSeperationBetweenMeshesRocks[j] && i >= indexSeperationBetweenMeshesRocks[j - 1])
                {
                    Graphics.DrawMeshInstanced(rockMeshes[j], 0, foliageMaterial, renderedPositionsRocks[i], renderedPositionsRocks[i].Length, block);
                }
            }
        }
        */
    }

    private static void CalculateFrustumPlanes()
    {
        var oldPlanes = planes;
        const float MinAbsTest = 0;

        var camPos = camera.transform.position;
        var camRot = camera.transform.rotation;

        const float precision = 0.0000004f;

        if (planes != null &&
            Mathf.Abs((camPos - lastCameraPosition).sqrMagnitude) <= precision &&
            Mathf.Abs(Quaternion.Dot(lastCameraRotation, camRot)) >= 1 - precision)
        {
            return;
        }

        planes = GeometryUtility.CalculateFrustumPlanes(camera);

        if (oldPlanes == null || oldPlanes.Length == 0)
        {
            MakeDirty();
            return;
        }

        for (var i = 0; i < planes.Length; i++)
        {
            var a = planes[i];
            var b = oldPlanes[i];

            if (Mathf.Abs(a.distance - b.distance) > MinAbsTest || Mathf.Abs((a.normal - b.normal).sqrMagnitude) > MinAbsTest)
            {
                MakeDirty();
                return;
            }
        }
    }

    private static void CalculatePositionsToRender()
    {
        planes = GeometryUtility.CalculateFrustumPlanes(camera);
        Vector3 playerPos = camera.transform.position;
        Vector3 planetCenter = currentPlanet.gameObject.transform.GetChild(0).position;
        Vector3 playerToPlanetCenter = playerPos - planetCenter;
        Vector3 halfWayPointNormal = new Vector3(playerToPlanetCenter.x/1.2f, playerToPlanetCenter.y/1.2f, playerToPlanetCenter.z/1.2f);

        List<Vector3> culledPositionsTrees = new List<Vector3>();
        List<Quaternion> culledRotationsTrees = new List<Quaternion>();
        for (int i = 0; i < positionsForTrees.Count; i++)
        {
            bool isBelowHalfWayPoint = CheckIfPointBIsBelowA(halfWayPointNormal,positionsForTrees[i],halfWayPointNormal.normalized);
            Bounds bound = new Bounds(positionsForTrees[i], new Vector3(2,2,2));
            if (GeometryUtility.TestPlanesAABB(planes, bound) && !isBelowHalfWayPoint)
            {
                culledPositionsTrees.Add(positionsForTrees[i]);
                culledRotationsTrees.Add(rotationsForTrees[i]);

            }
        }

        List<Vector3> culledPositionsRocks = new List<Vector3>();
        List<Quaternion> culledRotationsRocks = new List<Quaternion>();
        for (int i = 0; i < positionsForRocks.Count; i++)
        {
            Bounds bound = new Bounds(positionsForRocks[i], Vector3.one);
            if (GeometryUtility.TestPlanesAABB(planes, bound))
            {
                culledPositionsRocks.Add(positionsForRocks[i]);
                culledRotationsRocks.Add(rotationsForRocks[i]);
            }
        }

        bool reCalculateTrees = true;
        if (oldCulledPositionsTrees.Count > 0 && oldCulledPositionsTrees.Count == culledPositionsTrees.Count)
        {
            for (int i = 0; i < culledPositionsTrees.Count; i++)
            {
                if (culledPositionsTrees[i] != oldCulledPositionsTrees[i])
                {
                    culledPositionsForTrees = culledPositionsTrees;
                    culledRotationsForTrees = culledRotationsTrees;
                    break;
                }
            }
            reCalculateTrees = false;
        }

        bool reCalculateRocks = true;
        if (oldCulledPositionsRocks.Count > 0 && oldCulledPositionsRocks.Count == culledPositionsRocks.Count)
        {
            for (int i = 0; i < culledPositionsRocks.Count; i++)
            {
                if (culledPositionsRocks[i] != oldCulledPositionsRocks[i])
                {
                    culledPositionsForRocks = culledPositionsRocks;
                    culledRotationsForRocks = culledRotationsRocks;
                    break;
                }
            }
            reCalculateRocks = false;
        }

        if (reCalculateTrees)
        {
            culledPositionsForTrees = culledPositionsTrees;
            culledRotationsForTrees = culledRotationsTrees;
        }
        if (reCalculateRocks)
        {
            culledPositionsForRocks = culledPositionsRocks;
            culledRotationsForRocks = culledRotationsRocks;

        }
    }

    public static bool CheckIfPointBIsBelowA(Vector3 a, Vector3 b, Vector3 up)
    {
        return (Vector3.Dot(b - a, up) <= 0) ? true : false;
    }

    private static void MakeDirty()
    {
        lastCameraPosition = camera.transform.position;
        lastCameraRotation = camera.transform.rotation;
    }

    private static void CalculateMatrices()
    {
        List<Matrix4x4> treesMatrices = new List<Matrix4x4>();
        List<Matrix4x4> rocksMatrices = new List<Matrix4x4>();

        for (int i = 0; i < culledPositionsForTrees.Count; i++)
        {
            treesMatrices.Add(Matrix4x4.TRS(culledPositionsForTrees[i], culledRotationsForTrees[i], new Vector3(2, 2, 2)));
        }
        for (int i = 0; i < culledPositionsForRocks.Count; i++)
        {
            rocksMatrices.Add(Matrix4x4.TRS(culledPositionsForRocks[i], culledRotationsForRocks[i], new Vector3(2, 2, 2)));
        }

        
        GroupMatrices(treesMatrices, rocksMatrices);
    }

    private static void RandomizeMeshesForPositions()
    {
        int amountOfTreeMeshes = treeMeshes.Length;
        int amountOfRockMeshes = rockMeshes.Length;

        meshesIndexforTreePosition.Clear();
        meshesIndexforRockPosition.Clear();
        for (int i = 0; i < positionsForTrees.Count; i++)
        {
            int meshIndex = Universe.random.Next(0, amountOfTreeMeshes);
            meshesIndexforTreePosition.Add(meshIndex);
        }
        for (int i = 0; i < positionsForRocks.Count; i++)
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

        indexSeperationBetweenMeshesTrees.Clear();
        indexSeperationBetweenMeshesRocks.Clear();

        renderedPositionsTrees.Clear();
        renderedPositionsRocks.Clear();

        int[] mesheIndexesTrees = new int[treesMatrices.Count];
        for (int i = 0; i < treesMatrices.Count; i++)
        {
            meshesIndexforRockPosition[i] = meshesIndexforTreePosition[i];
        }

        float divideIndex = Mathf.Ceil((float)mesheIndexesTrees.Length / (float)numberOfTreemeshes);
        int divideCount = 1;

        for (int i = 0; i < mesheIndexesTrees.Length; i++)
        {
            if ((treesCount == divideIndex * divideCount) && treesCount > 0)
            {
                renderedPositionsTrees.Add(treesGroup.ToArray());
                treesGroup.Clear();
                indexSeperationBetweenMeshesTrees.Add(renderedPositionsTrees.Count);
                if (indexSeperationBetweenMeshesTrees.Count >= 6)
                {
                    Debug.Log("Too big!");
                }
                divideCount++;
            }
            else if ((treesCount % 1023) == 0 && treesCount > 0)
            {
                renderedPositionsTrees.Add(treesGroup.ToArray());
                treesGroup.Clear();
            }
            else if (treesCount == treesMatrices.Count - 1)
            {
                renderedPositionsTrees.Add(treesGroup.ToArray());
                treesGroup.Clear();
                break;
            }

            treesGroup.Add(treesMatrices[treesCount]);
            treesCount++;
        }

        if (treesCount == treesMatrices.Count - 1)
        {
            renderedPositionsTrees.Add(treesGroup.ToArray());
            treesGroup.Clear();
        }

        


        /*
        if (treesMatrices.Count > 0)
        {
            for (int j = 0; j < numberOfTreemeshes; j++)
            {
                
            }
        }

        if (rocksMatrices.Count > 0)
        {
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
                        else if (rocksCount == rocksMatrices.Count - 1)
                        {
                            renderedPositionsRocks.Add(rocksGroup.ToArray());
                            rocksGroup.Clear();
                            break;
                        }
                        rocksGroup.Add(rocksMatrices[rocksCount]);
                        rocksCount++;
                    }
                }
                if (rocksCount == rocksMatrices.Count - 1)
                {
                    renderedPositionsRocks.Add(rocksGroup.ToArray());
                    rocksGroup.Clear();
                    break;
                }
                indexSeperationBetweenMeshesRocks.Add(renderedPositionsRocks.Count);
            }
        }
        */
    }
}
