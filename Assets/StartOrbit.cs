using SimpleKeplerOrbits;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartOrbit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Turns on orbit for the given planet
        KeplerOrbitMover orbitMover = GetComponent<KeplerOrbitMover>();
        orbitMover.LockOrbitEditing = false;
        orbitMover.SetUp();
        orbitMover.SetAutoCircleOrbit();
        orbitMover.ForceUpdateOrbitData();
        orbitMover.enabled = true;
    }
}

