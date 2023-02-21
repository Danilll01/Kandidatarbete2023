using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetTester : MonoBehaviour
{
    public Planet planet;
    // Start is called before the first frame update
    void Start()
    {
        Universe.InitializeRandomWithSeed();
        planet.Initialize();
        planet.SetUpPlanetValues();
    }
}
