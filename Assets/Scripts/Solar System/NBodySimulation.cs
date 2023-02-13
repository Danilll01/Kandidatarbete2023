using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NBodySimulation : MonoBehaviour
{
    [HideInInspector] public List<PlanetBody> bodies;
    private static NBodySimulation instance;
    private SpawnPlanets planetsSpawner;
    private static PlanetBody sun;

    void Start()
    {
        planetsSpawner = GetComponent<SpawnPlanets>();
        bodies = planetsSpawner.bodies;
        sun = bodies[0];
        Time.fixedDeltaTime = Universe.physicsTimeStep;
        Debug.Log("Setting fixedDeltaTime to: " + Universe.physicsTimeStep);

    }

    void FixedUpdate()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            Vector3 velocity = CalculateVelocity(bodies[i]);
            bodies[i].UpdateVelocity(velocity);
        }

        for (int i = 0; i < bodies.Count; i++)
        {
            bodies[i].UpdatePosition(Universe.physicsTimeStep);
        }

    }

    public static Vector3 CalculateVelocity(PlanetBody body)
    {
        Vector3 velocity = Vector3.zero;
        if (body.bodyName != "Sun")
        {
            // Get the distance between the two object centers .
            MeshRenderer sunRenderer = sun.GetComponentInChildren<MeshRenderer>();
            MeshRenderer bodyRenderer = body.GetComponentInChildren<MeshRenderer>();
            Vector2 dist = new Vector2(sunRenderer.bounds.center.x - bodyRenderer.bounds.center.x, sunRenderer.bounds.center.z - bodyRenderer.bounds.center.z);
            float r = dist.magnitude;
            Vector2 tdist = new Vector2(dist.y, -dist.x).normalized; // 2D vector prependicular to the dist vector .
            float force = Mathf.Sqrt(Universe.gravitationalConstant * ((body.mass + sun.mass) / r)); // Calculate the velocity .
            velocity = tdist * force;
        }

        return velocity;
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