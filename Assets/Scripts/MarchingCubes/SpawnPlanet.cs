using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlanet : MonoBehaviour
{
    [SerializeField] Planet prefab;
    [SerializeField, Range(0, 1)] int update = 0;

    Planet planet;

    void OnValidate()
    {
        Planet planet = Instantiate(prefab);

        planet.transform.position = Vector3.zero;

        Universe.InitializeRandomWithSeed();
        planet.Initialize();
        planet.SetUpPlanetValues();
    }
}
