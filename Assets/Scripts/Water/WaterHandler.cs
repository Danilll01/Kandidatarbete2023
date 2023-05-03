using System.Collections;
using System.Collections.Generic;
using ExtendedRandom;
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
    private readonly Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
    private Water[] waterFaces;
    private Planet planet = null;
    private float waterRadius;

    private static readonly Color[] seaColors = new Color[] {
        new Color (219f/255, 144f/255, 101f/255),
        new Color (125f/255, 219f/255, 102f/255),
        new Color (102/255,  219f/255, 195f/255),
        new Color (102f/255, 183f/255, 219f/255),
        new Color (102f/255, 219f/255, 144f/255),
        new Color (102f/255, 105f/255, 219f/255),
        new Color (207f/255, 102f/255, 219f/255),
        new Color (219f/255, 102f/255, 142f/255),
        new Color (219f/255, 102f/255, 102f/255),
        new Color (251f/255, 70f/255, 47f/255),
        new Color (46f/255,  250f/255, 198f/255),
        new Color (47f/255,  233f/255, 250f/255),
        new Color (47f/255,  186f/255, 250f/255),
        new Color (47f/255,  137f/255, 250f/255),
        new Color (163f/255, 47f/255, 250f/255),
        new Color (0f/255,   44f/255, 147f/255),
        new Color (0f/255,   147f/255, 135f/255),
        new Color (0f/255,   147f/255, 136f/255),
        new Color (146f/255, 255f/255, 247f/255)
    };

    private Color seaColor;


    /// <summary>
    /// Initializes the water handler and all meshes
    /// </summary>
    /// <param name="planet">The planet connected to this waterhandler</param>
    /// <param name="waterDiameter">The current water diameter</param>
    /// <param name="randSeed">The rand seed to be used to calculate water color</param>
    public void Initialize(Planet planet, float waterDiameter, int randSeed)
    {
        this.planet = planet;
        waterRadius = Mathf.Abs(waterDiameter / 2) - 1;

        RandomX rand = new RandomX(randSeed);
        seaColor = seaColors[rand.Next(seaColors.Length)];

        GenerateMaterial(seaColor);
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

    public Color GetWaterColor => seaColor;

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
        waterFaces = new Water[6 * sideResolution * sideResolution];

        for (int i = 0; i < 6 * sideResolution * sideResolution; i++)
        {
            GameObject meshObj = new GameObject("waterMesh");
            meshObj.transform.parent = transform;
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.AddComponent<MeshRenderer>().sharedMaterial = material;
            MeshFilter meshFilter = meshObj.AddComponent<MeshFilter>();

            waterFaces[i] = new Water(computeShader, meshFilter, 32 * resolution, waterRadius, directions[i / (sideResolution * sideResolution)], i % (sideResolution * sideResolution), sideResolution);
        }
    }

    /// <summary>
    /// Constructs the meshes
    /// </summary>
    private void GenerateMesh()
    {
        foreach (Water waterface in waterFaces)
        {
            waterface.ConstructMesh();
        }
    }

    private void UpdateWater()
    {
        if (planet == Universe.player.attractor)
        {
            Vector3 playerPos = Universe.player.transform.position;
            foreach (Water waterface in waterFaces)
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
