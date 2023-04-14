using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WaterHandler : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField, Range(1, 12)] private int resolution = 1;
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private Shader waterShader;

    private Material material;
    readonly private Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
    private MeshFilter[] meshFilters;
    private Water[] waterfaces;
    private PlayerWater playerWater;
    private Planet planet = null;
    private float waterRadius;
    private bool underWaterState = false;
    public int sideResolution = 3;
    public GameObject testBoi;

    /// <summary>
    /// Checks if the plater is under water
    /// </summary>
    void Update()
    {
        if (planet == null) return;
        //UpdateWater();
        if (playerWater != null && underWaterState != playerWater.underWater) //&& ReferenceEquals(planet, playerWater.planet))
        {
            if (playerWater.underWater)
            {
                if(ReferenceEquals(planet, playerWater.planet)) SetWaterOnMesh(true);
                material.SetInt("_IsUnderWater", 1);
            }
            else
            {
                if (ReferenceEquals(planet, playerWater.planet)) SetWaterOnMesh(false);
                material.SetInt("_IsUnderWater", 0);
            }
            underWaterState = playerWater.underWater;
        }
    }

    /// <summary>
    /// Initializes the water handler and all meshes
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="waterDiameter"></param>
    /// <param name="color"></param>
    public void Initialize(Planet planet, float waterDiameter, Color color)
    {
        playerWater = Camera.main.gameObject.transform.parent.GetComponent<PlayerWater>();

        this.planet = planet;
        waterRadius = Mathf.Abs(waterDiameter / 2) - 1;

        GenerateMaterial(color);
        GenerateWater();
        GenerateMesh();
        GenerateColour();
    }

    public void InitializeTest(float waterDiameter, Color color)
    {
        waterRadius = Mathf.Abs(waterDiameter / 2) - 1;

        GenerateMaterial(color);
        GenerateWater();
        GenerateMesh();
        GenerateColour();
    }

    private void GenerateMaterial(Color color)
    {
        material = new Material(waterShader);
        material.SetColor("_ShallowWaterColor", color);
        material.SetColor("_DeepWaterColor", color);
        material.SetInt("_IsUnderWater", 0);
        material.renderQueue = 2800;
    }
    /// <summary>
    /// Generates separate game object for the different meshes
    /// </summary>
    private void GenerateWater()
    {
        if (meshFilters == null || meshFilters.Length == 0)
            meshFilters = new MeshFilter[6 * sideResolution * sideResolution];

        waterfaces = new Water[6 * sideResolution * sideResolution];

        for (int i = 0; i < 6 * sideResolution * sideResolution; i++)
        {
            
            GameObject meshObj = new GameObject("waterMesh");
            meshObj.transform.parent = transform;
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
            meshFilters[i] = meshObj.AddComponent<MeshFilter>();
            meshFilters[i].sharedMesh = new Mesh();
            
            waterfaces[i] = new Water(computeShader, meshFilters[i], 32, waterRadius, directions[i/ (sideResolution * sideResolution)], i % (sideResolution * sideResolution), sideResolution, meshObj);
        }
    }

    /// <summary>
    /// Constructs the meshes
    /// </summary>
    private void GenerateMesh()
    {
        foreach (Water waterface in waterfaces)
        {
            waterface.ConstructMesh();
        }
    }
    /// <summary>
    /// Applies the material to the meshes
    /// </summary>
    private void GenerateColour()
    {
        foreach (MeshFilter meshFilter in meshFilters)
        {
            meshFilter.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    /// <summary>
    /// Applies water to the meshes
    /// </summary>
    /// <param name="underWater"></param>
    private void SetWaterOnMesh(bool underWater)
    {
        foreach (Water waterface in waterfaces)
        {
            waterface.UnderWater(underWater);
        }
    }

    private void UpdateWater()
    {
        if (planet == Universe.player.attractor)
        {
            Vector3 playerPos = Universe.player.transform.position;
            foreach (Water waterface in waterfaces)
            {
                if(Vector3.Magnitude(playerPos - waterface.waterPos) > 1000)
                {
                    if(waterface.resolution != 1 * 32)
                    {
                        waterface.resolution = 1 * 32;
                        waterface.ConstructMesh();
                    }
                }
                else
                {
                    if (waterface.resolution != resolution * 32)
                    {
                        waterface.resolution = resolution * 32;
                        waterface.ConstructMesh();
                    }
                }
            }
        }
    }

}
