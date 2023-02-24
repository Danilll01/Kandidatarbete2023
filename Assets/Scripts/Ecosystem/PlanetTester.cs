using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetTester : MonoBehaviour
{
    public Planet planet;

    private bool show = true;
    // Start is called before the first frame update
    void Start()
    {
        Universe.InitializeRandomWithSeed();
        planet.Initialize(Universe.random.Next());
        planet.SetUpPlanetValues();
        planet.ShowCreatures(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            planet.ShowCreatures(!show);
            show = !show;
        }
    }
}
