using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageSpawnData
{
    public Vector3 position;
    public Quaternion rotation;
    public GameObject prefab;
    public Material material;
    public string biome;

    public FoliageSpawnData(Vector3 position, Quaternion rotation, GameObject prefab, Material material, string biome)
    {
        this.position = position;
        this.rotation = rotation;
        this.prefab = prefab;
        this.material = material;
        this.biome = biome;
    }
}
