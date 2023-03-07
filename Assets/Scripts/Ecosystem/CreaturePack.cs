using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CreaturePack
{
    public GameObject prefab;
    [Range(0, 20)]
    public float ratio;
    [Range(1, 100)]
    public int minPackSize;
    [Range(1, 100)]
    public int maxPackSize;
    [Range(1, 100)]
    public float packRadius;

}
