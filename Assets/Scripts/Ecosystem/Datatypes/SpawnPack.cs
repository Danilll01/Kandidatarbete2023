using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPack
{
    public Vector3 rayOrigin;
    public Quaternion rotation;
    public CreaturePack creature;

    public SpawnPack(Vector3 rayOrigin, Quaternion rotation, CreaturePack creature)
    {
        this.rayOrigin = rayOrigin;
        this.rotation = rotation;
        this.creature = creature;
    }
}
