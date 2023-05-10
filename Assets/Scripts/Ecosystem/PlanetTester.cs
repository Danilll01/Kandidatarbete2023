using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetTester : MonoBehaviour
{
    public Planet planet;
    public PillPlayerController player;

    // Start is called before the first frame update
    void Start()
    {
        Universe.InitializeRandomWithSeed();
        planet.radius = 500;
        planet.Initialize(Universe.random.Next(), true);
        planet.SetUpPlanetValues();
        player.Initialize(planet, Universe.random.Next());
    }

}
