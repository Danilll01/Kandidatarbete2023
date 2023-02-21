using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleKeplerOrbits;

public class SolarSystemTransform : MonoBehaviour
{
    public int activePlanetIndex = 0;
    private int previousMovedPlanet = -1;
    [SerializeField] private SpawnPlanets spawnPlanets;
    private GameObject sun;
    public bool update = false;
    private GameObject planetsParent;

    void Start()
    {
        if (spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.bodies[0].gameObject;
        }
        planetsParent = this.gameObject;
    }


    // Update is called once per frame
    void Update()
    {
        if (sun == null && spawnPlanets.bodies != null)
        {
            sun = spawnPlanets.bodies[0].gameObject;
        }

        if (previousMovedPlanet != activePlanetIndex && update)
        {
            MovePlanets(activePlanetIndex);
            previousMovedPlanet = activePlanetIndex;
            update = false;
        }
    }

    private void MovePlanets(int planetIndex)
    {
        Planet planet = spawnPlanets.bodies[planetIndex];
        planet.gameObject.GetComponent<KeplerOrbitMover>().enabled = false;
        Vector3 distanceFromOrigin = planet.transform.GetChild(0).transform.position - Vector3.zero;
        planetsParent.transform.position -= distanceFromOrigin;
    }
}
