using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainColor : MonoBehaviour
{

    [SerializeField] private Gradient gradient = new Gradient();
    [SerializeField] private float tempMin = 200;
    [SerializeField] private float tempMax = 300;
    [SerializeField][Range(0, 180)] private float angleCutOf = 90;
    [SerializeField][Range(0, 1)] private float angleBlending = 0.5f;

    private Texture2D texture;
    private const int textureRes = 50;
    private Material material;


    public void OnValidate() {
        MinMaxTerrainLevel terrainLevel = null;
        ColorPlanet(terrainLevel);
    }

    public void ColorPlanet(MinMaxTerrainLevel terrainLevel) 
    {

        if (terrainLevel != null) {
            tempMin = terrainLevel.GetMin();
            tempMax = terrainLevel.GetMax();
        }

        if (texture == null) {
            texture = new Texture2D(textureRes, 1);
        }
        if (material == null) {
            material = transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
        }

        UpdateMinMaxHight();
        UpdateAngleColorCutOf();
        SetMaterialColor();
        
    }

    private void UpdateMinMaxHight() {
        material.SetVector("_HightMinMax", new Vector4(tempMin, tempMax));
    }

    private void UpdateAngleColorCutOf() {

        float cutOf = angleCutOf / 180;
        float newAngleBlend = Mathf.Lerp(0, (cutOf), angleBlending);
        float minVal = Mathf.Clamp((cutOf - newAngleBlend), 0, 1);
        float maxVal = Mathf.Clamp((cutOf), 0, 1);

        material.SetVector("_AngleCutAndBlend", new Vector4(minVal, maxVal));
    }

    private void SetMaterialColor() {
        Color[] colors = new Color[textureRes];

        for (int i = 0; i < textureRes; i++) {
            colors[i] = gradient.Evaluate(i / (textureRes - 1f));
        }

        texture.SetPixels(colors);
        texture.Apply();
        material.SetColor("_GroundColor", colors[0]);
        material.SetTexture("_ColorGradient", texture);
    }

}
