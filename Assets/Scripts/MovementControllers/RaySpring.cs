using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaySpring : MonoBehaviour
{

    private float lastHitDist = 0;

    public void AddSpringForce(Rigidbody body, float rayLength, float springStrength, float springDampening)
    {
        Debug.DrawRay(transform.position, transform.forward, Color.blue, rayLength);

        if (Physics.Raycast(transform.position, transform.transform.forward, out RaycastHit hit, rayLength))
        {
            // Calculate spring force
            float forceAmount = springStrength * (rayLength - hit.distance) + (springDampening * (lastHitDist - hit.distance));
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
