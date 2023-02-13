using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NBodySimulation : MonoBehaviour
{
    private List<PlanetBody> bodies;
    private static NBodySimulation instance;
    [SerializeField] private GameObject planetsPrefab;
    [SerializeField] private GameObject planetsParent;
    [SerializeField] private int numberOfPlanets;

    void Awake()
    {
        bodies = new List<PlanetBody>();
        GameObject Sun = Instantiate(planetsPrefab);
        Sun.transform.parent = planetsParent.transform;
        Sun.gameObject.name = "Sun";
        bodies.Add(Sun.GetComponent<PlanetBody>());

        for (int i = 0; i < numberOfPlanets; i++)
        {
            GameObject planet = Instantiate(planetsPrefab);
            planet.transform.parent = planetsParent.transform;
            bodies.Add(planet.GetComponent<PlanetBody>());
        }
        Time.fixedDeltaTime = Universe.physicsTimeStep;
        Debug.Log("Setting fixedDeltaTime to: " + Universe.physicsTimeStep);
        Setup();
    }

    void Setup()
    {
        bodies[0].bodyName = "Sun";
        bodies[0].initialVelocity = new Vector3(0, 0, 0);
        for (int i = 1; i < bodies.Count; i++)
        {
            bodies[i].gameObject.name = "Planet " + i;
            bodies[i].bodyName = "Planet " + i;
        }
    }

    void FixedUpdate()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            Vector3 acceleration = CalculateAcceleration(bodies[i].Position, bodies[i]);
            bodies[i].UpdateVelocity(acceleration, Universe.physicsTimeStep);
            //bodies[i].UpdateVelocity (bodies, Universe.physicsTimeStep);
        }

        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].UpdatePosition(Universe.physicsTimeStep);
        }

    }

    public static Vector3 CalculateAcceleration(Vector3 point, PlanetBody ignoreBody = null)
    {
        Vector3 acceleration = Vector3.zero;
        foreach (var body in Instance.bodies)
        {
            if (body != ignoreBody)
            {
                float sqrDst = (body.Position - point).sqrMagnitude;
                Vector3 forceDir = (body.Position - point).normalized;
                acceleration += forceDir * Universe.gravitationalConstant * body.mass / sqrDst;
            }
        }

        return acceleration;
    }

    public static PlanetBody[] Bodies
    {
        get
        {
            return Instance.bodies.ToArray();
        }
    }

    static NBodySimulation Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NBodySimulation>();
            }
            return instance;
        }
    }
}