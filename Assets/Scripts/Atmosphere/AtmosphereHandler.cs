using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphereHandler : MonoBehaviour
{
    [SerializeField] private Shader atmosphereShader;
    private Material atmosphereMaterial;
    private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
    private static readonly int AtmosphereRadius = Shader.PropertyToID("_AtmosphereRadius");
    private static readonly int LightDirection = Shader.PropertyToID("_LightDirection");

    /// <summary>
    /// Sets up the planet atmosphere
    /// </summary>
    /// <param name="planetRadius">The radius of the current planet</param>
    /// <param name="waterLevel">The water level on that planet</param>
    public void Initialize(float planetRadius, float waterLevel)
    {
        transform.localScale = new Vector3((planetRadius*2) * 1.25f, (planetRadius*2) * 1.25f, (planetRadius*2) * 1.25f); // Set the size of material sphere around planet

        // Set up material
        atmosphereMaterial = new Material(atmosphereShader);
        atmosphereMaterial.SetFloat(PlanetRadius, waterLevel);
        atmosphereMaterial.SetFloat(AtmosphereRadius, planetRadius * 1.25f);
        GetComponent<MeshRenderer>().material = atmosphereMaterial;
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector4 lightDirection = (transform.position - Universe.sunPosition.position).normalized;
        atmosphereMaterial.SetVector(LightDirection, lightDirection);
    }
}
