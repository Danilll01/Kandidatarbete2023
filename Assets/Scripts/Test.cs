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

        InstanceFoliage.SetInstancingData(trees, rocks, material, shuffleGOList(positions), rotations, new List<Vector3>(), new List<Quaternion>());
        InstanceFoliage.instanceFoliage = true;
        //instanceFoliage.CalculateMatrices(positions, rotations, new List<Vector3>(), new List<Quaternion>());
    }

    void Update()
    {
        DrawBox(new Vector3(0,5,0), Quaternion.identity, new Vector3(4, 10, 4), Color.red);
        InstanceFoliage.Run();
        //InstanceFoliage.CalculateMatrices(positions, rotations, new List<Vector3>(), new List<Quaternion>());
        //Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
    }

    private List<Vector3> shuffleGOList(List<Vector3> inputList)
    {    //take any list of GameObjects and return it with Fischer-Yates shuffle
        int i = 0;
        int t = inputList.Count;
        int r = 0;
        Vector3 p = Vector3.zero;
        List<Vector3> tempList = new List<Vector3>();
        tempList.AddRange(inputList);

        while (i < t)
        {
            r = Random.Range(i, tempList.Count);
            p = tempList[i];
            tempList[i] = tempList[r];
            tempList[r] = p;
            i++;
        }

        return tempList;
    }

    public void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c)
        {
            // create matrix
            Matrix4x4 m = new Matrix4x4();
            m.SetTRS(pos, rot, scale);
 
            var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
            var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
            var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
            var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));
 
            var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
            var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
            var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
            var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));
 
            Debug.DrawLine(point1, point2, c);
            Debug.DrawLine(point2, point3, c);
            Debug.DrawLine(point3, point4, c);
            Debug.DrawLine(point4, point1, c);
 
            Debug.DrawLine(point5, point6, c);
            Debug.DrawLine(point6, point7, c);
            Debug.DrawLine(point7, point8, c);
            Debug.DrawLine(point8, point5, c);
 
            Debug.DrawLine(point1, point5, c);
            Debug.DrawLine(point2, point6, c);
            Debug.DrawLine(point3, point7, c);
            Debug.DrawLine(point4, point8, c);
        }
}

