using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureHandler : MonoBehaviour
{

    [SerializeField] public CreaturePack[] packs;
    public bool isInstantiated = false;
    public bool debug = true;
    private Planet planet;
    private float density;

    public void Initialize(Planet planet)
    {
        this.planet = planet;
        isInstantiated = true;


        density = planet.radius * 0.000002f; // Magic numbers * "random"
    }

    public float PlanetRadius
    {
        get { return planet.radius; }
    }
    public Vector3 PlanetPosition
    {
        get { return planet.transform.position; }
    }

    public float WaterRadius
    {
        get { return planet.waterDiameter / 2; }
    }
    public float Density
    {
        get { return density; }
    }
}
