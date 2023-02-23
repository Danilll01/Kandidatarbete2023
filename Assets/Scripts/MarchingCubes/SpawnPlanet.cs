using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlanet : MonoBehaviour
{
    [SerializeField] Planet prefab;
    [SerializeField, Range(0, 1)] int update = 0;
    [SerializeField] PillPlayerController player;

    Planet planet;

    void Start()
    {
        Planet planet = Instantiate(prefab);

        planet.radius = 1000;

        planet.transform.position = new Vector3(100, 0, 0);

        Universe.InitializeRandomWithSeed();
        planet.Initialize(player);
        planet.SetUpPlanetValues();

        player.Initialize(planet.gameObject);
    }
}
