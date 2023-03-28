using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphereHandler : MonoBehaviour
{
    [HideInInspector] public float lightIntensityLerp = 0; // How to lerp lights when near planet
    
    [SerializeField] private Shader atmosphereShader;
    private Material atmosphereMaterial;
    private float planetNormalRadius;
    private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
    private static readonly int AtmosphereRadius = Shader.PropertyToID("_AtmosphereRadius");
    private static readonly int LightDirection = Shader.PropertyToID("_LightDirection");
    private static readonly int LightIntensity = Shader.PropertyToID("_LightIntensity");

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
        planetNormalRadius = Mathf.RoundToInt(waterLevel) - 10;
        
        atmosphereMaterial.SetFloat(PlanetRadius, planetNormalRadius);
        atmosphereMaterial.SetFloat(AtmosphereRadius, Mathf.RoundToInt(planetRadius * 1.25f - 10));
        GetComponent<MeshRenderer>().material = atmosphereMaterial;
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 playerPosition = transform.position;
        Vector3 localScale = transform.localScale;
        Vector4 lightDirection = (playerPosition - Universe.sunPosition.position);
        atmosphereMaterial.SetVector(LightDirection, lightDirection);

        float playerHeight = Vector3.Distance(playerPosition, Universe.player.transform.position);
        atmosphereMaterial.SetFloat(PlanetRadius, Mathf.Min(planetNormalRadius, playerHeight - 10));

        lightIntensityLerp = Mathf.InverseLerp((localScale.x / 2f) - (localScale.x / 10f), (localScale.x / 2f), playerHeight);
        atmosphereMaterial.SetFloat(LightIntensity, Mathf.Lerp(20, 10, lightIntensityLerp));
    }
}
