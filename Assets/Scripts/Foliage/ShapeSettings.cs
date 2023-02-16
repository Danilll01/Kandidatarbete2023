using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ShapeSettings : ScriptableObject
{
    [HideInInspector]
    public bool foldOut = false;

    public float planetRadius = 1;

    public string seed = "";

    [Range(0, 20)]
    public float frequency = 1;

    [Range(0, 2)]
    public float magnitude = .5f;

    [Range(0, 4)]
    public float seperation = 1;
}
