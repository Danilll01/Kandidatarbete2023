using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class PlanetBody : MonoBehaviour
{

    public float radius;
    public float surfaceGravity;
    public Vector3 initialVelocity;
    public string bodyName = "TBT";
    Transform meshHolder;

    public Vector3 velocity { get; private set; }
    public float mass;
    Rigidbody rb;

    public void UpdateVelocity(PlanetBody[] allBodies, float timeStep)
    {
        foreach (var otherBody in allBodies)
        {
            if (otherBody != this)
            {
                float sqrDst = (otherBody.rb.position - rb.position).sqrMagnitude;
                Vector3 forceDir = (otherBody.rb.position - rb.position).normalized;

                Vector3 acceleration = forceDir * Universe.gravitationalConstant * otherBody.mass / sqrDst;
                velocity += acceleration * timeStep;
            }
        }
    }

    public void UpdateVelocity(Vector3 acceleration, float timeStep)
    {
        velocity += acceleration * timeStep;
    }

    public void UpdatePosition(float timeStep)
    {
        if (velocity.magnitude > 0)
        {
            rb.MovePosition(rb.position + velocity * timeStep);
        }

    }

    public void SetUp()
    {
        mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        meshHolder = transform.GetChild(0);
        meshHolder.localScale = Vector3.one * radius;
        gameObject.name = bodyName;

        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.position = transform.position;
        velocity = initialVelocity;

    }

    public Rigidbody Rigidbody
    {
        get
        {
            return rb;
        }
    }

    public Vector3 Position
    {
        get
        {
            return rb.position;
        }
    }

}