using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class Sun : MonoBehaviour
{
    public float radius;
    public float surfaceGravity = 10;
    public string bodyName = "TBT";
    public float mass;

    /// <summary>
    /// Set up the values for the sun
    /// </summary>
    public void SetUpPlanetValues()
    {
        mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        gameObject.name = bodyName;
    }
}
