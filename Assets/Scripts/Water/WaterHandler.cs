using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WaterHandler : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField, Range(1, 8)] private int resolution = 1;
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Material material;
    [SerializeField] private float frequency;
    [SerializeField] private float amplitude;

    readonly private Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
    private MeshFilter[] meshFilters;
    private Water[] waterfaces;
    private GameObject waterHandler;
    private GameObject player;
    private Planet planet;
    private float waterRadius;
    private bool init = false;

    void Update()
    {
        if (init)
        {

            GenerateMesh();
        }
    }

    public void Initialize(Planet planet, float waterRadius)
    {
        GameObject[] cameras = GameObject.FindGameObjectsWithTag("MainCamera");
        player = cameras[0];

        this.planet = planet;

        waterHandler = new GameObject("Water");
        waterHandler.transform.parent = planet.transform;
        waterHandler.transform.localPosition = new Vector3(0, 0, 0);

        this.waterRadius = Mathf.Abs(waterRadius / 2);

        if (meshFilters == null || meshFilters.Length == 0)
            meshFilters = new MeshFilter[6];
        
        waterfaces = new Water[6];

        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("waterMesh");
                meshObj.transform.parent = waterHandler.transform;
                meshObj.transform.localPosition = new Vector3(0, 0, 0);

                meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }
            waterfaces[i] = new Water(computeShader, meshFilters[i], resolution * 32, this.waterRadius, directions[i]);
        }

        GenerateMesh();
        GenerateColour();
        init = true;
    }

    private void GenerateMesh()
    {
        foreach (Water waterface in waterfaces)
        {
            waterface.ConstructMesh(frequency, amplitude);
        }
    }

    private void GenerateColour()
    {
        foreach (MeshFilter meshFilter in meshFilters)
        {
            meshFilter.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
