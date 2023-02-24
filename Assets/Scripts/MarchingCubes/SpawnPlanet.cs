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

        planet.diameter = 1000;

        planet.transform.position = new Vector3(100, 0, 0);

        Universe.InitializeRandomWithSeed();
        planet.Initialize(player, 1);
        planet.SetUpPlanetValues();

        player.Initialize(planet.gameObject);
    }
}
