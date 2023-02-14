using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeGenerator
{
    ShapeSettings settings;

    public ShapeGenerator(ShapeSettings settings)
    {
        this.settings = settings;
        Perlin.SetSeed(settings.seed);
    }

    public Vector3 CalculatePointOnPlanet(Vector3 pointOnUnitSphere)
    {
        float elevation = Perlin.Noise(settings.frequency * pointOnUnitSphere) * settings.seperation * settings.planetRadius + settings.magnitude * settings.planetRadius;
        return pointOnUnitSphere * Mathf.Max(elevation, settings.planetRadius);
    }
}
