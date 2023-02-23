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

    // Start is called before the first frame update
    public void Initialize(int index, int resolution, MarchingCubes marchingCubes, PillPlayerController player)
    {
        this.index = index;
        this.resolution = resolution;
        this.marchingCubes = marchingCubes;
        this.player = player;

        meshFilter = transform.GetChild(0).GetComponent<MeshFilter>();
        meshCollider = transform.GetChild(0).GetComponent<MeshCollider>();

        updateMesh(resolution);
    }

    private void Update()
    {
        // TODO: Update the size of the mesh according to the distance to the player.
    }

    private void updateMesh(int resolution)
    {
        mesh = new Mesh();
        marchingCubes.generateMesh(index, resolution, mesh);

        meshFilter.sharedMesh = mesh;

        meshCollider.sharedMesh = mesh;
    }
}
