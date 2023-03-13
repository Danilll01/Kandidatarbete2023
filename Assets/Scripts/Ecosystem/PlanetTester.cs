using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetTester : MonoBehaviour
{
    public Planet planet;
    public PillPlayerController player;

    private bool show = true;
    // Start is called before the first frame update
    void Start()
    {
        player.Initialize(planet);

        Universe.InitializeRandomWithSeed();
        planet.Initialize(player.transform, Universe.random.Next(), true);
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
