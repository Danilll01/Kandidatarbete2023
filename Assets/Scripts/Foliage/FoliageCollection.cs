using System;
using UnityEngine;

[Serializable]
public struct FoliageCollection
{
    public string name;
    public GameObject[] gameObjects;
    public BiomeRange biomeRange;
    public Material[] biomeMaterials;
    [Range(0f, 1f)] public float probabilityToSkip;
}
