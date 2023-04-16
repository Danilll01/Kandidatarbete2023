using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class orbitResetTester : MonoBehaviour
{
    public GameObject closestPlanet;
    public GameObject secondPlanet;
    public GameObject sun;

    public Vector3 rotationVector;

    public bool stopRotation;

    public bool resetOrbit;

    private Vector3 directionOneToSunBeforeReset;
    private Vector3 directionTwoToSunBeforeReset;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if (!stopRotation)
        {
            closestPlanet.transform.RotateAround(sun.transform.position, rotationVector, 10f * Time.deltaTime);
            secondPlanet.transform.RotateAround(sun.transform.position, rotationVector, 10f * Time.deltaTime);
        }
        else
        {
            directionOneToSunBeforeReset = closestPlanet.transform.position - sun.transform.position;
            directionTwoToSunBeforeReset = secondPlanet.transform.position - sun.transform.position;
            if (resetOrbit)
            {
                Vector3 directionatZeroY = directionOneToSunBeforeReset;
                directionatZeroY.y = 0;
                directionOneToSunBeforeReset = Quaternion.FromToRotation(directionOneToSunBeforeReset, directionatZeroY) * directionOneToSunBeforeReset;
                
                Vector3 directionTwoatZeroY = directionTwoToSunBeforeReset;
                directionTwoatZeroY.y = 0;
                directionTwoToSunBeforeReset = Quaternion.FromToRotation(directionTwoToSunBeforeReset, directionTwoatZeroY) * directionTwoToSunBeforeReset;


                sun.transform.position = Vector3.zero;
                
                
                closestPlanet.transform.position = sun.transform.position + directionOneToSunBeforeReset;
                secondPlanet.transform.position = sun.transform.position + directionTwoToSunBeforeReset;
                resetOrbit = false;
            }
        }
        
    }

    private void OnDrawGizmos()
    {
        float radius = (closestPlanet.transform.position - sun.transform.position).magnitude;
        Universe.DrawGizmosCircle(sun.transform.position, rotationVector, radius, 32);
        
        float radiusTwo = (secondPlanet.transform.position - sun.transform.position).magnitude;
        Universe.DrawGizmosCircle(sun.transform.position, rotationVector, radiusTwo, 32);

        if (stopRotation)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(sun.transform.position, sun.transform.position + directionOneToSunBeforeReset);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(sun.transform.position, sun.transform.position + directionTwoToSunBeforeReset);
        }
    }
}
