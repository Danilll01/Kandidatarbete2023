using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaySpring : MonoBehaviour
{

    [SerializeField] private float speed;
    [SerializeField] private float rayLength;
    [SerializeField] private float springStrength;
    [SerializeField] private float springDampening;
    [SerializeField] private Rigidbody physicsBody;
    
    private float lastHitDist = 0;


    private void FixedUpdate()
    {
        physicsBody.velocity = transform.forward * speed;
        AddSpringForce(physicsBody);
    }
    
    private void AddSpringForce(Rigidbody body)
    {
        Debug.DrawRay(transform.position, transform.forward, Color.blue, rayLength);

        if (Physics.Raycast(transform.position, transform.transform.forward, out RaycastHit hit, rayLength))
        {
            float forceAmount = CalculateSpringForce(hit.distance);
            body.AddForce(hit.normal * forceAmount);
        }
        else
        {
            lastHitDist = rayLength * 1.1f;
        }
    }

    private float CalculateSpringForce(float hitDistance)
    {
        float forceAmount = springStrength * (rayLength - hitDistance) + (springDampening * (lastHitDist - hitDistance));
        forceAmount = Mathf.Max(0f, forceAmount);
        lastHitDist = hitDistance;
        return forceAmount;
    }
}
