using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlanet : MonoBehaviour
{
    [SerializeField] Planet prefab;
    [SerializeField, Range(0, 1)] int update = 0;
    [SerializeField] PillPlayerController player;

    Planet planet;

    void OnValidate()
    {
        Planet planet = Instantiate(prefab);

        planet.transform.position = new Vector3(100, 0, 0);

        Universe.InitializeRandomWithSeed();
        planet.Initialize();
        planet.SetUpPlanetValues();

        player.Initialize(planet.gameObject);
    }
}
