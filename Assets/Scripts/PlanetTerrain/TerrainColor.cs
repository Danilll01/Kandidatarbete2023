using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using ExtendedRandom;
using UnityEngine;

public class TerrainColor : MonoBehaviour {

    [SerializeField] private Gradient gradient = new Gradient();
    [SerializeField] private float tempMin = 200;
    [SerializeField] private float tempMax = 300;
    [SerializeField][Range(0, 180)] private float angleCutOf = 90;
    [SerializeField][Range(0, 1)] private float angleBlending = 0.5f;
    [SerializeField] private Shader shader;

    [HideInInspector] public Color bottomColor;
    private Material material;
    private Texture2D texture;
    private const int textureRes = 50;
    private RandomX random;

    private BiomeSettings biomeSettings;

    private Color[][] crazyColorPaletts =
    {
        new Color[] { new Color(49/255f, 55/255f, 21/255f), new Color(209/255f, 96/255f, 20/255f), new Color(147/255f, 159/255f, 92/255f), new Color(187/255f, 206/255f, 138/255f), new Color(226/255f, 249/255f, 184/255f) }, // Olive green palette
        new Color[] { new Color(70/255f, 34/255f, 85/255f), new Color(49/255f, 59/255f, 114/255f), new Color(98/255f, 168/255f, 124/255f), new Color(126/255f, 224/255f, 129/255f), new Color(195/255f, 243/255f, 192/255f) }, // Green blue palette
        new Color[] { new Color(32/255f, 220/255f, 93/255f), new Color(242/255f, 163/255f, 89/255f), new Color(219/255f, 144/255f, 101/255f), new Color(164/255f, 3/255f, 31/255f), new Color(36/255f, 11/255f, 54/255f) }, // Yellow red palette
        new Color[] { new Color(163/255f, 231/255f, 252/255f), new Color(38/255f, 196/255f, 133/255f), new Color(50/255f, 144/255f, 143/255f), new Color(85/255f, 58/255f, 65/255f), new Color(47/255f, 6/255f, 1/255f) }, // Turcoise blue palette
        new Color[] { new Color(58/255f, 64/255f, 90/255f), new Color(174/255f, 197/255f, 235/255f), new Color(249/255f, 222/255f, 201/255f), new Color(233/255f, 175/255f, 163/255f), new Color(104/255f, 80/255f, 68/255f) }, // Skin color blue brown palette
        new Color[] { new Color(197/255f, 197/255f, 197/255f), new Color(76/255f, 91/255f, 97/255f), new Color(130/255f, 145/255f, 145/255f), new Color(148/255f, 155/255f, 150/255f), new Color(44/255f, 66/255f, 63/255f) }, // Muted green palette
        new Color[] { new Color(212/255f, 150/255f, 167/255f), new Color(157/255f, 105/255f, 90/255f), new Color(120/255f, 224/255f, 220/255f), new Color(142/255f, 237/255f, 247/255f), new Color(161/255f, 205/255f, 241/255f) }, // Candy colors palette
        new Color[] { new Color(150/255f, 52/255f, 132/255f), new Color(48/255f, 102/255f, 190/255f), new Color(96/255f, 175/255f, 255/255f), new Color(40/255f, 194/255f, 255/255f), new Color(42/255f, 245/255f, 255/255f) }, // Blue palette
        new Color[] { new Color(81/255f, 45/255f, 56/255f), new Color(178/255f, 112/255f, 146/255f), new Color(244/255f, 191/255f, 219/255f), new Color(255/255f, 233/255f, 243/255f), new Color(135/255f, 186/255f, 171/255f) }, // Pink palette
        new Color[] { new Color(206/255f, 190/255f, 190/255f), new Color(236/255f, 226/255f, 208/255f), new Color(213/255f, 185/255f, 178/255f), new Color(162/255f, 103/255f, 105/255f), new Color(109/255f, 46/255f, 70/255f) }, // Light brown palette
        new Color[] { new Color(2/255f, 47/255f, 64/255f), new Color(56/255f, 174/255f, 204/255f), new Color(0/255f, 144/255f, 193/255f), new Color(24/255f, 52/255f, 70/255f), new Color(4/255f, 110/255f, 143/255f) }, // All blue palette
        new Color[] { new Color(63/255f, 63/255f, 55/255f), new Color(214/255f, 214/255f, 177/255f), new Color(73/255f, 67/255f, 49/255f), new Color(135/255f, 132/255f, 114/255f), new Color(222/255f, 84/255f, 30/255f) }, // Dark olive palette
        new Color[] { new Color(140/255f, 140 / 255f, 140 / 255f), new Color(180/255f, 179/255f, 20/255f), new Color(73/255f, 200/255f, 40/255f), new Color(149 / 255f, 149 / 255f, 149 / 255f), new Color(1, 1, 1) } // EARTH FOR TESTING
    };

    private Color[][] normalColorPalette =
    {
      new Color[] { new Color(140/255f, 140 / 255f, 140 / 255f), new Color(180/255f, 179/255f, 20/255f), new Color(73/255f, 200/255f, 40/255f), new Color(149 / 255f, 149 / 255f, 149 / 255f), new Color(1, 1, 1) } // Earth like palette
    };

    [SerializeField] private Gradient monutain;

    /// <summary>
    /// Will color the planet with a random color
    /// </summary>
    /// <param name="terrainLevel">The terrain level, this contains min and max hight for colors</param>
    /// <param name="randomSeedGen">Random seed to be used when creating new random</param>
    public Material GetPlanetMaterial(MinMaxTerrainLevel terrainLevel, int randomSeedGen, BiomeSettings biomeSettings) 
    {
        this.biomeSettings = biomeSettings;

        random = new RandomX(randomSeedGen);

        material = new Material(shader);

        if (terrainLevel != null) {
            tempMin = terrainLevel.GetMin();
            tempMax = terrainLevel.GetMax();
        }

        if (texture == null) {
            texture = new Texture2D(textureRes, 1);
        }

        UpdateMinMaxHight();
        SetMaterialColor();
        UpdateAngleColorCutOf();
        UpdateBiomeSetting();

        return material;
    }

    // Updates the min and max color hight
    private void UpdateMinMaxHight() {
        material.SetVector("_HightMinMax", new Vector4(tempMin, tempMax));
    }

    // Updates the angle color value
    private void UpdateAngleColorCutOf() {

        float cutOf = angleCutOf / 180;
        float newAngleBlend = Mathf.Lerp(0, (cutOf), angleBlending);
        float minVal = Mathf.Clamp((cutOf - newAngleBlend), 0, 1);
        float maxVal = Mathf.Clamp((cutOf), 0, 1);

        material.SetVector("_AngleCutAndBlend", new Vector4(minVal, maxVal));
    }

    private void UpdateBiomeSetting()
    {
        material.SetFloat("_Seed", biomeSettings.seed);
        material.SetFloat("_Distance", 0);
        material.SetFloat("_MountainFrequency", biomeSettings.mountainFrequency);
        material.SetFloat("_TempFrequency", biomeSettings.temperatureFrequency);
        material.SetFloat("_TemperatureDecay", biomeSettings.temperatureDecay);
        material.SetFloat("_FarTemperature", biomeSettings.farTemperature);
        material.SetFloat("_Roughness", biomeSettings.temperatureRoughness);
        material.SetFloat("_MountainAffect", biomeSettings.mountainTemperatureAffect);
        material.SetFloat("_TreeFrequency", biomeSettings.treeFrequency);

    }

    // Sets the material color bands to use based on hight
    private void SetMaterialColor() 
    {
        Color[] colors = new Color[textureRes];

        // Gets color palette and puts it into a gradient
        Color[] takePalette = normalColorPalette[random.Next(normalColorPalette.Length)];
        float[] keyPos = new float[] { 0f, 0.015f, 0.144f, 0.618f, 1f };
        GradientColorKey[] gradientKeys = new GradientColorKey[takePalette.Length];
        Color[] takePaletteRand = takePalette.OrderBy(x => random.Next()).ToArray();


        for (int i = 0; i < takePalette.Length; i++) {
            gradientKeys[i] = new GradientColorKey(takePaletteRand[i], keyPos[i]);
        }

        GradientAlphaKey[] alphaKey = new GradientAlphaKey[1];
        gradient.SetKeys(gradientKeys, alphaKey);

        // Use the gradient to sample the 50 values into the color texture
        for (int i = 0; i < textureRes; i++) {
            colors[i] = gradient.Evaluate(i / (textureRes - 1f));
        }

        // Applies all to material
        texture.SetPixels(colors);
        texture.Apply();

        material.SetColor("_GroundColor", colors[0]);
        material.SetTexture("_ColorGradient", texture);

        // Use this for water
        bottomColor = colors[0];

    }
}
