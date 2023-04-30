using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaySpring : MonoBehaviour
{

    private float lastHitDist = 0;

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
            // Calculate spring force
            Vector3 test = body.velocity;
            test.Scale(transform.forward);
            float testSpeed = test.magnitude;
            
            //float forceAmount = springStrength * (rayLength - hit.distance) + (springDampening * (lastHitDist - hit.distance));
            Debug.Log(testSpeed);
            float forceAmount = springStrength * (rayLength - hit.distance) + (springDampening * testSpeed);
            forceAmount = Mathf.Max(0f, forceAmount);
            lastHitDist = hit.distance;
            
            body.AddForce(hit.normal * forceAmount);
        }
        else
        {
            lastHitDist = rayLength * 1.1f;
        }
    }
    
}
