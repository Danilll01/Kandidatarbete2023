using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Chunk : MonoBehaviour
{

    int index;
    int resolution;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    Mesh mesh;
    MarchingCubes marchingCubes;
    PillPlayerController player;
    MinMaxTerrainLevel terrainLevel;

    /// <summary>
    /// Initialize
    /// </summary>
    /// <param name="index"></param>
    /// <param name="resolution"></param>
    /// <param name="marchingCubes"></param>
    /// <param name="player"></param>
    public void Initialize(int index, int resolution, MarchingCubes marchingCubes, PillPlayerController player, MinMaxTerrainLevel terrainLevel)
    {
        this.index = index;
        this.resolution = resolution;
        this.marchingCubes = marchingCubes;
        this.player = player;
        this.terrainLevel = terrainLevel;

        meshFilter = transform.GetComponent<MeshFilter>();
        meshCollider = transform.GetComponent<MeshCollider>();

        updateMesh(resolution);
    }

    private void Update()
    {
        // TODO: Update the size of the mesh according to the distance to the player.
    }

    private void updateMesh(int resolution)
    {
        mesh = new Mesh();
        marchingCubes.generateMesh(terrainLevel, index, resolution, mesh);

        meshFilter.sharedMesh = mesh;

        meshCollider.sharedMesh = mesh;
    }
}
