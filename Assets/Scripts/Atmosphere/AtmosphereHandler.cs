using System.Collections;
using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;

public class AtmosphereHandler : MonoBehaviour
{
    [HideInInspector] public float lightIntensityLerp = 0; // How to lerp lights when near planet
    
    [SerializeField] private Shader atmosphereShader;
    private Material atmosphereMaterial;
    private float planetNormalRadius;
    private RandomX random;
    
    private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
    private static readonly int AtmosphereRadius = Shader.PropertyToID("_AtmosphereRadius");
    private static readonly int LightDirection = Shader.PropertyToID("_LightDirection");
    private static readonly int LightIntensity = Shader.PropertyToID("_LightIntensity");
    private static readonly int RayleighScattering = Shader.PropertyToID("_RayleighScattering");  

    // Atmosphere colors (colors assigned from nearest planet and up in the comments)
    private static readonly Vector4[] rayLeightValues = { new (0.08f, 0.2f, 0.51f, 50), // Normal earth
                                                          new (0.2f, 0.08f, 0.51f, 50), // Yellow, Orange, Pink, Purple
                                                          new (0.51f, 0.2f, 0.08f, 50), // Blue, Yellow, Orange (Desert planer / Venus like)
                                                          new (0.2f, 0.51f, 0.08f, 50), // Pink, Yellow, Green
                                                          new (0.08f, 0.51f, 0.2f, 50), // Pink, Blue, Turquoise
                                                          new (0.51f, 0.08f, 0.2f, 50), // Blue, Pink
                                                          new (0.02f, 0.1f, 1f, 50),    // Red, Green, Blue
                                                          new (0.5f, 0.06f, 0.06f, 50), // Turquoise, Red
                                                          new (0.15f, 0.04f, 0.74f, 50) // Green, Yellow, Pink, Purple
    };

    

    /// <summary>
    /// Sets up the planet atmosphere
    /// </summary>
    /// <param name="planetRadius">The radius of the current planet</param>
    /// <param name="waterLevel">The water level on that planet</param>
    /// <param name="randomSeed">The random seed to be used</param>
    public void Initialize(float planetRadius, float waterLevel, int randomSeed)
    {
        random = new RandomX(randomSeed);
        
        transform.localScale = new Vector3((planetRadius*2) * 1.25f, (planetRadius*2) * 1.25f, (planetRadius*2) * 1.25f); // Set the size of material sphere around planet

        // Set up material
        atmosphereMaterial = new Material(atmosphereShader);
        planetNormalRadius = Mathf.RoundToInt(waterLevel) - 10;
        
        atmosphereMaterial.SetFloat(PlanetRadius, planetNormalRadius);
        atmosphereMaterial.SetFloat(AtmosphereRadius, Mathf.RoundToInt(planetRadius * 1.25f - 10));
        SelectAtmosphereColors();
        GetComponent<MeshRenderer>().material = atmosphereMaterial;
        
    }

    // Colors the atmosphere
    private void SelectAtmosphereColors()
    {
        if (random.Value() < 0.5f)
        {
            // Normal atmosphere is more common
            atmosphereMaterial.SetVector(RayleighScattering, rayLeightValues[0]);
        }
        else
        {
            // Can be other colors too
            atmosphereMaterial.SetVector(RayleighScattering, rayLeightValues[random.Next(1, rayLeightValues.Length - 1)]); 
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Light direction
        Vector4 lightDirection = (transform.position - Universe.sunPosition.position);
        atmosphereMaterial.SetVector(LightDirection, lightDirection);
    }

    public void UpdateAtmosphereAmbient(Gradient skyGradient)
    {
        Vector3 planetPosition = transform.position;
        Vector3 localScale = transform.localScale;
        Vector3 sunPosition = Universe.sunPosition.position;
        Vector3 playerPosition = Universe.player.transform.position;
        
        // Planet radius (for when going underwater)
        float playerHeight = Vector3.Distance(planetPosition, playerPosition);
        atmosphereMaterial.SetFloat(PlanetRadius, Mathf.Min(planetNormalRadius, playerHeight - 10));

        // Light intensity (Makes atmosphere appear more thick)
        lightIntensityLerp = Mathf.InverseLerp((localScale.x / 2f) - (localScale.x / 10f), (localScale.x / 2f), playerHeight);
        atmosphereMaterial.SetFloat(LightIntensity, Mathf.Lerp(20, 10, lightIntensityLerp));


        
        // Sets the ambient light
        float sunPlanetDistance = Vector3.Distance(sunPosition, planetPosition);
        float maxCutOfDistance = Vector3.Distance(Vector3.zero, new Vector3(sunPlanetDistance, planetNormalRadius + 10));
        float minDistance = sunPlanetDistance - planetNormalRadius + 10;

        // Will be between 0 and 1 with 1 being when the player is near sun (max light) and 0 being 90 degrees to the side of the planet (lowest ambient light) 
        float lightAmount  = Mathf.InverseLerp(maxCutOfDistance, minDistance, Vector3.Distance(sunPosition, playerPosition));

        RenderSettings.ambientLight = Color.Lerp( skyGradient.Evaluate(lightAmount), skyGradient.Evaluate(0), lightIntensityLerp);
    }
}
