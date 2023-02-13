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
        bodies[0].radius = 5000;
        bodies[0].SetUp();

        for (int i = 1; i < numberOfPlanets + 1; i++)
        {
            GameObject planet = Instantiate(planetsPrefab);
            planet.transform.parent = planetsParent.transform;
            planet.transform.position = new Vector3(0,0,10000*i);
            planet.gameObject.name = "Planet " + i;
            PlanetBody planetBody = planet.GetComponent<PlanetBody>();
            planetBody.GetComponent<Rigidbody>().position = planet.transform.position;
            planetBody.bodyName = "Planet " + i;
            planetBody.initialVelocity = new Vector3(100,0,0);
            planetBody.SetUp();
            bodies.Add(planetBody);
        }
    }
}
