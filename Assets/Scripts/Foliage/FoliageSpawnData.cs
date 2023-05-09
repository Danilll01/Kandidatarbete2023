using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoliageSpawnData
{
    public Vector3 position;
    public Quaternion rotation;
    public GameObject prefab;
    public string biome;

    public FoliageSpawnData(Vector3 position, Quaternion rotation, GameObject prefab, string biome)
    {
        this.position = position;
        this.rotation = rotation;
        this.prefab = prefab;
        this.biome = biome;
    }
}
