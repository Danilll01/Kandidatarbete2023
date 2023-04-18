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
    [SerializeField] private int sideResolution = 3;

    [SerializeField] private Material material;
    readonly private Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
    private Water[] waterfaces;
    private Planet planet = null;
    private float waterRadius;

    void Update()
    {
        //if (planet == null) return;
        //UpdateWater();
    }

    /// <summary>
    /// Initializes the water handler and all meshes
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="waterDiameter"></param>
    /// <param name="color"></param>
    public void Initialize(Planet planet, float waterDiameter, Color color)
    {
        this.planet = planet;
        waterRadius = Mathf.Abs(waterDiameter / 2) - 1;

        GenerateMaterial(color);
        GenerateWater();
        GenerateMesh();
    }

    public void InitializeTest(float waterDiameter, Color color)
    {
        waterRadius = Mathf.Abs(waterDiameter / 2) - 1;
        GenerateMaterial(color);
        GenerateWater();
        GenerateMesh();
    }

    private void GenerateMaterial(Color color)
    {
        material = new Material(waterShader);
        material.SetColor("_ShallowWaterColor", color);
        material.SetColor("_DeepWaterColor", color);
        material.renderQueue = 2800;
    }
    /// <summary>
    /// Generates separate game object for the different meshes
    /// </summary>
    private void GenerateWater()
    {
        waterfaces = new Water[6 * sideResolution * sideResolution];

        for (int i = 0; i < 6 * sideResolution * sideResolution; i++)
        {
            GameObject meshObj = new GameObject("waterMesh");
            meshObj.transform.parent = transform;
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.AddComponent<MeshRenderer>().sharedMaterial = material;
            MeshFilter meshFilter = meshObj.AddComponent<MeshFilter>();

            waterfaces[i] = new Water(computeShader, meshFilter, 32 * resolution, waterRadius, directions[i / (sideResolution * sideResolution)], i % (sideResolution * sideResolution), sideResolution);
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

    private void UpdateWater()
    {
        if (planet == Universe.player.attractor)
        {
            Vector3 playerPos = Universe.player.transform.position;
            foreach (Water waterface in waterfaces)
            {
                if (Vector3.Magnitude(playerPos - waterface.waterPos) > 1000)
                {
                    if (!waterface.highRes)
                    {
                        waterface.ApplyMesh(false);
                    }
                }
                else
                {
                    if (waterface.highRes)
                    {
                        waterface.ApplyMesh(true);
                    }
                }
            }
        }

    }
}
