using UnityEngine;
using Random = System.Random;

public class FoliageHandler
{
   
    private Planet planet;
    private float waterRadius;
    private int density;
    private float frequency = 0.01f;
    private readonly Random random = new (Universe.seed);

    public FoliageHandler(Planet planet)
    {
        this.planet = planet;
        waterRadius = Mathf.Abs(planet.waterDiameter / 2);
        density = (int)(planet.radius / 2 * (random.NextDouble()) - 0.5); // Magic numbers
    }

    public Vector3 PlanetPosition
    {
        get { return planet.transform.position; }
    }

    public int Density
    {
        get { return density; }
    }
    public float WaterRadius
    {
        get { return waterRadius; }
    }

    public float Frequency
    {
        get { return frequency; }
    }

    public float PlanetRadius
    {
        get { return planet.radius; }
    }

    public bool IsPlanet
    {
        get { return !planet.bodyName.Contains("Moon"); }
    }

}