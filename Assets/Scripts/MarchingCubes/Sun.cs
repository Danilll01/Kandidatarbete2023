using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using ExtendedRandom;
using UnityEngine;

public class Sun : MonoBehaviour
{
    public float diameter;
    public string bodyName = "Sun";
    public float temperature;
    private static readonly int Temperature = Shader.PropertyToID("_temperature");

    [Header("Orbits")]
    [SerializeField] public string attractorName = "";
    [SerializeField] public float distanceToAttractor = 0;

    /// <summary>
    /// Initialize mesh for marching cubes
    /// </summary>
    public void Initialize(int randomSeed)
    {
        RandomX rand = new RandomX(randomSeed);

        temperature = rand.Value(3000, 8000);
        transform.GetChild(0).GetComponent<MeshRenderer>().material.SetFloat(Temperature, temperature);
    }

    /// <summary>
    /// Set up the values for the planets
    /// </summary>
    public void SetUpPlanetValues()
    {
        gameObject.name = bodyName;
    }
}
