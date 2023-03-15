using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtendedRandom;
using static UnityEngine.Rendering.PostProcessing.PostProcessResources;
using Unity.VisualScripting;

public class TestPlanet : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField, Range(0, 1)] private int update = 0;
    [SerializeField] private Chunk chunkPrefab;
    [SerializeField] ComputeShader meshGenerator;
    [SerializeField] ComputeShader waterCS;
    [SerializeField] Shader waterShader;
    [SerializeField] private int resolution = 4;
    [SerializeField] private TerrainColor terrainColor;
    [SerializeField] private Texture2D normal1;
    [SerializeField] private Texture2D normal2;

    private GameObject chunksParent;
    private GameObject waterParent;
    MeshFilter[] meshFilters;
    Water[] waterfaces;
    private List<Chunk> chunks;
    private MarchingCubes marchingCubes;
    private MinMaxTerrainLevel terrainLevel;
    private Material planetMaterial;
    private Material waterMaterial;
    
    private float radius = 500;
    [HideInInspector] public float waterDiameter;

    private void OnValidate()
    { 
        if(chunksParent == null)
        {
            chunks = null;
            chunksParent = new GameObject();
            chunksParent.transform.parent = transform;
            chunksParent.name = "chunks";
            chunksParent.transform.localPosition = Vector3.zero;
        }

        RandomX rand = new RandomX();

        terrainLevel = new MinMaxTerrainLevel();

        float threshold = 23 + rand.Value() * 4;
        int frequency = rand.Next(2) + 3;
        float amplitude = 1.2f + rand.Value() * 0.4f;
        marchingCubes = new MarchingCubes(1, meshGenerator, threshold, radius, frequency, amplitude);

        waterDiameter = -(threshold / 255 - 1) * radius * 2 * 0.93f;

        terrainLevel.SetMin(Mathf.Abs((waterDiameter + 1) / 2));

        CreateMeshes(3, resolution, terrainLevel);

        planetMaterial = terrainColor.GetPlanetMaterial(terrainLevel, rand.Next());

        foreach(Chunk chunk in chunks)
        {
            chunk.SetMaterial(planetMaterial);
        }

        GenerateMaterial(terrainColor.bottomColor);
        GenerateWater();
    }
    
    private void CreateMeshes(int chunkResolution, int resolution, MinMaxTerrainLevel terrainLevel)
    {
        marchingCubes.chunkResolution = chunkResolution;

        // Create all chunks
        if(chunks == null || chunks.Count == 0)
        {
            chunks = new List<Chunk>();
            int noChunks = (1 << chunkResolution) * (1 << chunkResolution) * (1 << chunkResolution);
            for (int i = 0; i < noChunks; i++)
            {
                Chunk chunk = Instantiate(chunkPrefab);
                chunk.transform.parent = chunksParent.transform;
                chunk.transform.localPosition = Vector3.zero;
                chunk.name = "chunk" + i;
                chunk.Initialize(i, resolution, marchingCubes, transform, terrainLevel);
                chunks.Add(chunk);
            }
        }
        else
        {
            foreach(Chunk chunk in chunks)
            {
                chunk.updateMesh(resolution);
            }
        }
    }

    private void GenerateMaterial(Color color)
    {
        waterMaterial = new Material(waterShader);
        waterMaterial.SetTexture("_Normal1", normal1);
        waterMaterial.SetTexture("_Normal2", normal2);
        waterMaterial.SetColor("_ShallowWaterColor", color);
        waterMaterial.SetColor("_DeepWaterColor", color);
        waterMaterial.SetInt("_IsUnderWater", 0);
    }

    private void GenerateWater()
    {
        if (waterParent == null)
        {
            waterParent = new GameObject();
            waterParent.transform.parent = transform;
            waterParent.name = "water";
            waterParent.transform.localPosition = Vector3.zero;

            meshFilters = new MeshFilter[6];
        }

        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down };

        waterfaces = new Water[6];

        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("waterMesh");
                meshObj.transform.parent = waterParent.transform;
                meshObj.transform.localPosition = new Vector3(0, 0, 0);

                meshObj.AddComponent<MeshRenderer>().sharedMaterial = waterMaterial;
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }
            waterfaces[i] = new Water(waterCS, meshFilters[i], resolution * 32, waterDiameter / 2, directions[i]);
            waterfaces[i].ConstructMesh();
        }
    }
#endif
}
