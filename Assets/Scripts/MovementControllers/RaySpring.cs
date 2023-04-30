using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaySpring : MonoBehaviour
{

    /// <summary>
    /// Adds a force to the rigidbody corresponding to a imaginary  spring
    /// </summary>
    /// <param name="body">The rigidbody to get force</param>
    /// <param name="rayLength">The length of the imaginary spring</param>
    /// <param name="springStrength">The strength of the imaginary spring</param>
    /// <param name="springDampening">The dampening force when the spring is compressed quick</param>
    public void AddSpringForce(Rigidbody body, float rayLength, float springStrength, float springDampening)
    {
        Debug.DrawRay(transform.position, transform.forward * rayLength, Color.blue, rayLength);

        if (Physics.Raycast(transform.position, transform.transform.forward, out RaycastHit hit, rayLength))
        {
            // Get direction speed for spring dampen
            Vector3 bodyVelocity = body.velocity;
            bodyVelocity.Scale(transform.forward);
            float directionSpeed = bodyVelocity.magnitude;

            // Calculate spring force
            float forceAmount = springStrength * (rayLength - hit.distance) + (springDampening * directionSpeed);
            forceAmount = Mathf.Max(0f, forceAmount);

            body.AddForce(hit.normal * forceAmount);
        }
    }
    
}
