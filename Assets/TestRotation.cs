using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotation : MonoBehaviour
{
    public GameObject sun;
    public GameObject planet;
    public Transform pointToTeleportSunTo;
    private Vector3 startDirection;
    private Vector3 newDirection;
    private Quaternion originalSunRotation;


    // Start is called before the first frame update
    void Start()
    {
        originalSunRotation = sun.transform.rotation;
        startDirection = planet.transform.position - sun.transform.position;
        sun.transform.position = pointToTeleportSunTo.position;
        sun.transform.rotation = Quaternion.identity;
        newDirection = Quaternion.Inverse(originalSunRotation) * startDirection;
        planet.transform.rotation = Quaternion.identity;
        planet.transform.position = sun.transform.position + newDirection;
        



    }

    private void OnDrawGizmos()
    {

        if (newDirection != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(sun.transform.position, sun.transform.position + newDirection);
        }
        
    }
}
