using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlanets : MonoBehaviour
{
    [HideInInspector] public List<PlanetBody> bodies;
    [SerializeField] private GameObject planetsPrefab;
    [SerializeField] private GameObject planetsParent;
    [SerializeField] private int numberOfPlanets;

    [SerializeField] private Material sunMaterial;

    void Awake()
    {
        
        Setup();
    }

    void Setup()
    {
        bodies = new List<PlanetBody>();
        GameObject Sun = Instantiate(planetsPrefab);
        Sun.transform.parent = planetsParent.transform;
        Sun.gameObject.name = "Sun";
        Sun.GetComponentInChildren<MeshRenderer>().material = sunMaterial;
        bodies.Add(Sun.GetComponent<PlanetBody>());
        bodies[0].bodyName = "Sun";
        bodies[0].initialVelocity = new Vector3(0, 0, 0);

        for (int i = 1; i < numberOfPlanets + 1; i++)
        {
            GameObject planet = Instantiate(planetsPrefab);
            planet.transform.parent = planetsParent.transform;
            planet.gameObject.name = "Planet " + i;
            PlanetBody planetBody = planet.GetComponent<PlanetBody>();
            planetBody.bodyName = "Planet " + i;
            bodies.Add(planetBody);
        }
    }
}
