using System.Collections;
using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;

public class AtmosphereHandler : MonoBehaviour
{
    
    [SerializeField] private Shader atmosphereShader;
    private Material atmosphereMaterial;
    private float lightIntensityLerp;
    private float planetNormalRadius; // This is actually water level
    private float realPlanetRadius;   // This is the real planet radius used for ambient light calculations
    private RandomX random;
    private Gradient ambientGradient;
    private float maxLightIntensity = 20f;
    private bool atmosphereExist;

    private static readonly int PlanetRadius = Shader.PropertyToID("_PlanetRadius");
    private static readonly int AtmosphereRadius = Shader.PropertyToID("_AtmosphereRadius");
    private static readonly int LightDirection = Shader.PropertyToID("_LightDirection");
    private static readonly int LightIntensity = Shader.PropertyToID("_LightIntensity");
    private static readonly int RayleighScattering = Shader.PropertyToID("_RayleighScattering");

    private static readonly int[] maxLightProbability = { 20, 20, 20, 15, 10, 5, 5};
    private static readonly int[] atmosphereSizeProbability = { 50, 50, 50, 50, 70, 60, 40, 30, 20, 10};

    // These gradients are set in the editor and match the atmosphere values below. (Uses same index)
    [SerializeField] private Gradient noAtmosphereGradient;
    [SerializeField] private Gradient[] ambientGradients;
    
    // Atmosphere colors (colors assigned from nearest planet and up in the comments)
    private static readonly Vector4[] rayLeightValues = { new (0.08f, 0.2f, 0.51f), // Normal earth
                                                          new (0.2f, 0.08f, 0.51f), // Yellow, Orange, Pink, Purple
                                                          new (0.51f, 0.2f, 0.08f), // Blue, Yellow, Orange (Desert planer / Venus like)
                                                          new (0.2f, 0.51f, 0.08f), // Pink, Yellow, Green
                                                          new (0.08f, 0.51f, 0.2f), // Pink, Blue, Turquoise
                                                          new (0.51f, 0.08f, 0.2f), // Blue, Pink
                                                          new (0.02f, 0.1f, 1f),    // Red, Green, Blue
                                                          new (0.5f, 0.06f, 0.06f), // Turquoise, Red
                                                          new (0.15f, 0.04f, 0.74f) // Green, Yellow, Pink, Purple
    };


    /// <summary>
    /// Sets up the planet atmosphere
    /// </summary>
    /// <param name="planetRadius">The radius of the current planet</param>
    /// <param name="waterLevel">The water level on that planet</param>
    /// <param name="randomSeed">The random seed to be used</param>
    /// <param name="atmosphereExists">If the atmosphere should exist or not</param>
    public void Initialize(float planetRadius, float waterLevel, bool atmosphereExists, int randomSeed)
    {
        random = new RandomX(randomSeed);
        atmosphereExist = atmosphereExists;
        realPlanetRadius = planetRadius;
        
        transform.localScale = new Vector3((planetRadius*2) * 1.25f, (planetRadius*2) * 1.25f, (planetRadius*2) * 1.25f); // Set the size of material sphere around planet

        // Set up material
        atmosphereMaterial = new Material(atmosphereShader);
        planetNormalRadius = waterLevel - 10;

        atmosphereMaterial.renderQueue = 3200;
        atmosphereMaterial.SetFloat(PlanetRadius, planetNormalRadius);
        atmosphereMaterial.SetFloat(AtmosphereRadius, planetRadius * 1.25f - 10);

        if (atmosphereExists)
        {
            SelectAtmosphereColors();
            SelectMaxLightIntensity();
        }
        else
        {
            // Effectively turns of the atmosphere
            maxLightIntensity = 0;
            ambientGradient = noAtmosphereGradient;
            GetComponent<MeshRenderer>().enabled = false;
            atmosphereMaterial.SetFloat(LightIntensity, maxLightIntensity);
        }
        
        GetComponent<MeshRenderer>().material = atmosphereMaterial;

        if (ambientGradients.Length != rayLeightValues.Length)
        {
            Debug.LogError("Atmosphere ambient gradients do not match atmosphere values. This have chance to cause error");
        }
    }

    // Colors the atmosphere
    private void SelectAtmosphereColors()
    {
        if (random.Value() < 0.5f)
        {
            // Normal atmosphere is more common
            
            Vector4 finalColorVector = FixAtmosphereSize(rayLeightValues[0]);

            atmosphereMaterial.SetVector(RayleighScattering, finalColorVector);
            ambientGradient = ambientGradients[0];
        }
        else
        {
            // Can be other colors too
            int randomColorVal = random.Next(1, rayLeightValues.Length - 1);
            
            // Get color vector
            Vector4 finalColorVector = FixAtmosphereSize(rayLeightValues[randomColorVal]);

            atmosphereMaterial.SetVector(RayleighScattering, finalColorVector); 
            ambientGradient = ambientGradients[randomColorVal];
        }
    }

    // Selects an atmosphere size
    private Vector4 FixAtmosphereSize(Vector4 finalColorVector)
    {
        // Set size
        finalColorVector.w = atmosphereSizeProbability[random.Next(1, atmosphereSizeProbability.Length - 1)];
        return finalColorVector;
    }

    // Will select one value to use as the maximum light intensity
    private void SelectMaxLightIntensity()
    {
        maxLightIntensity = maxLightProbability[random.Next(maxLightProbability.Length - 1)];
        atmosphereMaterial.SetFloat(LightIntensity, Mathf.Min(10, maxLightIntensity));
    }

    // Update is called once per frame
    void Update()
    {
        // Light direction
        Vector4 lightDirection = (transform.position - Universe.sunPosition.position);
        atmosphereMaterial.SetVector(LightDirection, lightDirection);
    }

    /// <summary>
    /// Updates the ambient light and atmosphere based on current player and this atmosphere
    /// </summary>
    public void UpdateAtmosphereAmbient()
    {
        Vector3 planetPosition = transform.position;
        Vector3 localScale = transform.localScale;
        Vector3 sunPosition = Universe.sunPosition.position;
        Vector3 playerPosition = Universe.player.transform.position;
        
        UpdateAtmosphereValues(planetPosition, playerPosition, localScale);
        UpdateAmbientLight(sunPosition, planetPosition, playerPosition);
    }

    private void UpdateAtmosphereValues(Vector3 planetPosition, Vector3 playerPosition, Vector3 localScale)
    {
        
        // Planet radius (for when going underwater)
        float playerHeight = Vector3.Distance(planetPosition, playerPosition);
        atmosphereMaterial.SetFloat(PlanetRadius, Mathf.Min(planetNormalRadius, playerHeight - 10));

        if (!atmosphereExist) return;
        
        // Light intensity (Makes atmosphere appear more thick)
        lightIntensityLerp = Mathf.InverseLerp((localScale.x / 2f) - (localScale.x / 10f), (localScale.x / 2f), playerHeight);
        atmosphereMaterial.SetFloat(LightIntensity, Mathf.Lerp(maxLightIntensity, Mathf.Min(10, maxLightIntensity), lightIntensityLerp));

        // Makes water be shaded by the atmosphere when outside the planet and not when inside
        atmosphereMaterial.renderQueue = playerHeight > (localScale.x / 2f) ? 3200 : 3000;
    }

    private void UpdateAmbientLight(Vector3 sunPosition, Vector3 planetPosition, Vector3 playerPosition)
    {
        
        // Sets the ambient light
        float sunPlanetDistance = Vector3.Distance(sunPosition, planetPosition);
        float maxCutOfDistance = Vector3.Distance(Vector3.zero, new Vector3(sunPlanetDistance, realPlanetRadius));
        float minDistance = sunPlanetDistance - realPlanetRadius;

        // Will be between 0 and 1 with 1 being when the player is near sun (max light) and 0 being 90 degrees to the side of the planet (lowest ambient light) 
        float lightAmount = Mathf.InverseLerp(maxCutOfDistance, minDistance, Vector3.Distance(sunPosition, playerPosition));
        
        // If the atmosphere is thinner the ambient light will have less color
        float strengthAmount = Mathf.InverseLerp(20, 0,maxLightIntensity);
        Color ambientColor = Color.Lerp(ambientGradient.Evaluate(lightAmount), noAtmosphereGradient.Evaluate(lightAmount), strengthAmount);
        RenderSettings.ambientLight = Color.Lerp(ambientColor, ambientGradient.Evaluate(0), lightIntensityLerp);
        
    }
}
