using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WaterHandler : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField, Range(1, 8)] private int resolution = 1;
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Shader waterShader;
    [SerializeField] private float frequency = 1;
    [SerializeField] private float amplitude = 1;
    [SerializeField] private Texture2D normal1;
    [SerializeField] private Texture2D normal2;

    private Material material;

    readonly private Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
    private MeshFilter[] meshFilters;
    private Water[] waterfaces;
    private PlayerWater playerWater;
    private Planet planet = null;
    private float waterRadius;

    public void Initialize(Planet planet, float waterDiameter, Color color)
    {
        playerWater = Camera.main.gameObject.transform.parent.GetComponent<PlayerWater>();

        this.planet = planet;
        waterRadius = Mathf.Abs(waterDiameter / 2);

        GenerateMaterial(color);
        GenerateWater();
        GenerateMesh();
        GenerateColour();
    }

    private void GenerateMaterial(Color color)
    {
        material = new Material(waterShader);
        material.SetTexture("_Normal1", normal1);
        material.SetTexture("_Normal2", normal2);
        material.SetColor("_ShallowWaterColor", color);
        material.SetColor("_DeepWaterColor", color);
        material.SetInt("_IsUnderWater", 0);
    }

    private void GenerateWater()
    {
        if (meshFilters == null || meshFilters.Length == 0)
            meshFilters = new MeshFilter[6];

        waterfaces = new Water[6];

        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("waterMesh");
                meshObj.transform.parent = transform;
                meshObj.transform.localPosition = new Vector3(0, 0, 0);

                meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }
            waterfaces[i] = new Water(computeShader, meshFilters[i], resolution * 32, waterRadius, directions[i]);
        }
    }

    private void GenerateMesh()
    {
        foreach (Water waterface in waterfaces)
        {
            waterface.ConstructMesh();
        }
    }

    private void GenerateColour()
    {
        foreach (MeshFilter meshFilter in meshFilters)
        {
            meshFilter.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    void Update()
    {

        if(playerWater != null)
        {
            // Vet inte om det är best att köra denna här.
            if (playerWater.underWater)
            {
                material.SetInt("_IsUnderWater", 1);
            }
            else
            {
                material.SetInt("_IsUnderWater", 0);
            }
        }
        
    }
}
