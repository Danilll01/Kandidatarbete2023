using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Material))]
public class PlayerWater : MonoBehaviour
{
    
    [SerializeField] private Material water;
    [SerializeField] private GameObject PostProssesing;
    [HideInInspector] public Planet planet = null;
    [HideInInspector] public bool underWater;
    private static readonly int C1 = Shader.PropertyToID("_C1");
    private static readonly int UnderWater = Shader.PropertyToID("_UnderWater");
    [SerializeField] private HandleAudio audio;

    public void Initialize(Planet planet)
    {
        underWater = false;
        UpdatePlanet(planet);
        water.SetFloat(UnderWater, 0);
        underWater = false;
    }

    /// <summary>
    /// Call this when changing planets
    /// </summary>
    /// <param name="planet"></param>
    public void UpdatePlanet(Planet planet)
    {
        this.planet = planet;
        SetColors();
    }

    /// <summary>
    /// Supposed to check Joels script and check what color the seafloor is.
    /// </summary>
    private void SetColors()
    {
        water.SetColor(C1, planet.GetSeaColor);
    }

    /// <summary>
    /// Call once a frame. Checks if the player is under water or not.
    /// </summary>
    public void UpdateWater(Vector3 playerPos)
    {
        bool underWater = Mathf.Abs(planet.waterDiameter / 2) > playerPos.magnitude + 1.7f; // Addition because camera is higher
        if (underWater != this.underWater)
        {
            if (underWater)
            {
                audio.PlaySimpleSoundEffect(HandleAudio.SoundEffects.WaterSplash, true);
                water.SetFloat(UnderWater, 1);
                PostProssesing.SetActive(true);
                this.underWater = true;
            }
            else
            {
                water.SetFloat(UnderWater, 0);
                PostProssesing.SetActive(false);
                this.underWater = false;
            }
            this.underWater = underWater;
        }
    }
}
