using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainColor : MonoBehaviour
{

    [SerializeField] private Gradient gradient = new Gradient();
    [SerializeField] private float tempMin = 200;
    [SerializeField] private float tempMax = 300;

    private Texture2D texture;
    private const int textureRes = 50;
    private Material material;


    public void OnValidate() {
        ColorPlanet();
    }

    public void ColorPlanet() 
    {

        if (texture == null) {
            texture = new Texture2D(textureRes, 1);
        }
        if (material == null) {
            material = transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
        }

        UpdateMinMaxHight();
        SetMaterialColor();

    }

    private void UpdateMinMaxHight() {
        material.SetVector("_HightMinMax", new Vector4(tempMin, tempMax));
    }

    private void SetMaterialColor() {
        Color[] colors = new Color[textureRes];

        for (int i = 0; i < textureRes; i++) {
            colors[i] = gradient.Evaluate(i / (textureRes - 1f));
        }

        texture.SetPixels(colors);
        texture.Apply();
        material.SetTexture("_ColorGradient", texture);
    }

}
