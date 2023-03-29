using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TerrainLayer
{
    [SerializeField] public float strength, baseRoughness, roughness, persistance;
    [SerializeField] public Vector3 centre;
    [Range(0, 8)] public int numLayers;
}

