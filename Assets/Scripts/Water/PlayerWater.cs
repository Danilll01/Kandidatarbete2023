using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Material))]
public class PlayerWater : MonoBehaviour
{
    
    [SerializeField] private Material water;
    [HideInInspector] public Planet planet = null;
    [HideInInspector] public bool underWater;

    public void Initialize(Planet planet)
    {
        underWater = false;
        UpdatePlanet(planet);
        water.SetFloat("_UnderWater", 0);
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
        water.SetColor("_C1", planet.GetGroundColor());
    }

    /// <summary>
    /// Call once a frame. Checks if the player is under water or not.
    /// </summary>
    public void UpdateWater(Vector3 playerPos)
    {
        bool underWater = Mathf.Abs(planet.waterDiameter / 2) > playerPos.magnitude;
        if (underWater != this.underWater)
        {
            if (underWater)
            {
                water.SetFloat("_UnderWater", 1);
                this.underWater = true;
            }
            else
            {
                water.SetFloat("_UnderWater", 0);
                this.underWater = false;
            }
            this.underWater = underWater;
        }
    }
}
