using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [Tooltip("Rows and cols for the map grid")]
    public Vector2 myGrid;

    [Tooltip("Prefabs dimensions in Unity units")]
    public Vector3 tileDimensions;

    [Tooltip("Populate with all the prefabs used to generate map")]
    public GameObject[] prefabTiles;

    // storing the map tiles in a list could be useful
    List<GameObject> mapList = new List<GameObject>();
    List<Vector3> positions = new List<Vector3>();
    List<Quaternion> rotations = new List<Quaternion>();
    //public InstanceFoliage instanceFoliage;

    public GameObject[] trees;
    public GameObject[] rocks;

    public Mesh mesh;
    public Material material;
    Matrix4x4[] matrices;

    void Start()
    {
        for (int row = 1; row <= myGrid.y; row++)
        {
            for (int col = 1; col <= myGrid.x; col++)
            {
                // choose a random prefab tile
                int n = Random.Range(0, prefabTiles.Length);
                GameObject thePrefab = prefabTiles[n];

                // spawns the tile
                //GameObject theTile = Instantiate(thePrefab, transform);
                //theTile.name = "Tile_" + col + "_" + row;
                //theTile.transform.localPosition = new Vector3((col - 1) * tileDimensions.x, 0f, (row - 1) * tileDimensions.z);
                positions.Add(new Vector3((col - 1) * tileDimensions.x, 0f, (row - 1) * tileDimensions.z));
                // stores the tile in the List
                //mapList.Add(theTile);
            }
        }

        /*
        matrices = new Matrix4x4[1000]; // initialize array
        for (int i = 0; i < positions.Count; i++)
        {
            Quaternion rotation = Quaternion.identity;
            Vector3 scale = new Vector3(1f, 1f, 1f);
            matrices[i] = Matrix4x4.TRS(positions[i], rotation, scale);
        }*/
        
        for (int i = 0; i < positions.Count; i++)
        {
            rotations.Add(Quaternion.identity);
        }

        InstanceFoliage.SetInstancingData(trees, rocks, material);
    }

    void Update()
    {
        InstanceFoliage.CalculateMatrices(positions, rotations, new List<Vector3>(), new List<Quaternion>());
        //Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
    }
}

