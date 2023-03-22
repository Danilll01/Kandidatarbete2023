using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlanet : MonoBehaviour
{
    [SerializeField] Planet prefab;
    [SerializeField] PillPlayerController player;

    Planet planet;

    void Start()
    {
        Planet planet = Instantiate(prefab);
        
        planet.radius = 1000;

        planet.transform.position = new Vector3(500, 0, 0);

        Universe.InitializeRandomWithSeed();
        planet.Initialize(player.transform, 1, true);
        planet.SetUpPlanetValues();

        player.Initialize(planet);
    }
}
