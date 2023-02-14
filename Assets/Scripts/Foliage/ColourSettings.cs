using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ColourSettings : ScriptableObject
{
    [HideInInspector]
    public bool foldOut = false;

    public Color planetColor;
}
