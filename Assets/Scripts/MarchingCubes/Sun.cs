using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Sun : MonoBehaviour
{
    public float diameter;
    public float radius;
    public float surfaceGravity;
    public string bodyName = "Sun";
    public float mass;

    /// <summary>
    /// Initialize mesh for marching cubes
    /// </summary>
    public void Initialize(Transform player, int randomSeed)
    {
        System.Random rand = new System.Random(randomSeed);

        radius = diameter / 2;
    }

    /// <summary>
    /// Set up the values for the planets
    /// </summary>
    public void SetUpPlanetValues()
    {
        mass = surfaceGravity * diameter * diameter / Universe.gravitationalConstant;
        gameObject.name = bodyName;
    }
}
