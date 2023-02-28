using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Material))]
public class PlayerWater : MonoBehaviour
{
    
    [SerializeField] private Material water;
    private Planet planet = null;
    public bool underWater = false;

    public void Initialize(Planet planet)
    {
        updatePlanet(planet);
    }

    public void updatePlanet(Planet planet)
    {
        this.planet = planet;
        setColors();
    }

    /// <summary>
    /// Supposed to check Joels script and check what color the seafloor is.
    /// </summary>
    private void setColors()
    {

    }

    /// <summary>
    /// Call once a frame. Checks if the player is under water or not.
    /// </summary>
    public void updateWater(Vector3 playerPos)
    {
        if (planet != null)
        {

        }
    }

}
