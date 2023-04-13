using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExtendedRandom;

public class CreatureHandler : MonoBehaviour
{

    [SerializeField] public CreaturePack[] packs;
    [SerializeField] [Range(0, 1)] float keepPackRatio = 1f;
    [SerializeField] private float densityMultiplier = 0.0000012f;
    public bool isInstantiated = false;
    public bool debug = true;
    private Planet planet;
    private float density;

    public void Initialize(Planet planet, int seed)
    {
        RandomX rand = new RandomX(seed);
        this.planet = planet;

        //Remove some pack types
        List<CreaturePack> culledPack = new List<CreaturePack>();
        for (int i = 0; i < packs.Length; i++)
        {
            //Is true (keepPackRatio %) number of times
            if (rand.Value() < keepPackRatio)
            {
                culledPack.Add(packs[i]);
            }
        }
        packs = culledPack.ToArray();

        isInstantiated = true;

        density = planet.radius * densityMultiplier; // Magic numbers * "random"
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
