using System;
using System.Collections;
using System.Collections.Generic;
using SimpleKeplerOrbits;
using UnityEngine;

public class SetUpOrbit : MonoBehaviour
{
    private Vector3 moonsrelativeDistances;
    public Transform parent;
    public Vector3 rotationAxis;

    // Start is called before the first frame update
    void Start()
    {
        moonsrelativeDistances = transform.position - parent.position;
        KeplerOrbitMover planetOrbitMover = GetComponent<KeplerOrbitMover>();
        // Turns on orbit for the given planet
        planetOrbitMover.LockOrbitEditing = false;
        planetOrbitMover.SetUp();
        planetOrbitMover.SetAutoCircleOrbit();
        planetOrbitMover.ForceUpdateOrbitData();
        planetOrbitMover.enabled = true;
    }

    /*
    
    private void Update()
    {
        //RotateAround(transform.position, rotationAxis, 5f * Time.deltaTime);
    }
    
    private void RotateAround(Vector3 center, Vector3 axis, float angle) {
        Vector3 pos = this.transform.position;
        Quaternion rot = Quaternion.AngleAxis(angle, axis); // get the desired rotation
        Vector3 dir = pos - center; // find current direction relative to center
        dir = rot * dir; // rotate the direction
        //this.transform.position = center + dir; // define new position
        // rotate object to keep looking at the center:
        Quaternion myRot = transform.rotation;
        transform.rotation *= Quaternion.Inverse(myRot) * rot * myRot;
    }

    */
    private void Update()
    {
        Transform moon = this.transform;
        Vector3 direction = moon.transform.position - parent.position;
        moon.gameObject.GetComponent<KeplerOrbitMover>().SetAutoCircleOrbit();
        moon.transform.position = direction.normalized * moonsrelativeDistances.magnitude;
        
    }
    
}
