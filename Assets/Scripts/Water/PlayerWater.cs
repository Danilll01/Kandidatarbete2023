using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Material))]
public class PlayerWater : MonoBehaviour
{
    
    [SerializeField] private Material water;
    private Planet planet = null;
    [HideInInspector] public bool underWater = false;

    public void Initialize(Planet planet)
    {
        underWater = false;
        UpdatePlanet(planet);
    }

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

    }

    /// <summary>
    /// Call once a frame. Checks if the player is under water or not.
    /// </summary>
    public void UpdateWater(Vector3 playerPos)
    {
        if (Mathf.Abs(planet.waterDiameter / 2) > playerPos.magnitude)
        {
            water.SetFloat("_UnderWater", 1);
            underWater = true;
        }
        else
        {
            water.SetFloat("_UnderWater", 0);
            underWater = false;
        }
    }

}
