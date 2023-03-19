using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleKeplerOrbits;

public class OrbitTest : MonoBehaviour
{
    public Vector3 rotationAxis;
    public List<Planet> moons;
    public bool resetMoons = false;
    public GameObject moonsParent;
    private bool moonsLocked = true;
    private bool orbit = false;

    private Vector3[] moonsrelativeDistances;


    private void Start()
    {
        InitializeMoonsValues();
    }

    // Update is called once per frame
    void Update()
    {
        if (!resetMoons)
        {
            var orthogonalVector = rotationAxis;//Vector3.RotateTowards(planetToSun, -planetToSun, Mathf.PI / 2f, 0f);
                                                //orthogonalVector.y = 0;
            transform.RotateAround(transform.position, orthogonalVector, 10f * Time.deltaTime);
        }
        else if(!orbit)
        {
            ResetMoons();
            orbit = true;
        }
        
    }

    public void InitializeMoonsValues()
    {
        moonsrelativeDistances = new Vector3[moons.Count];

        for (int i = 0; i < moons.Count; i++)
        {
            moonsrelativeDistances[i] = moons[i].transform.position - this.transform.position;
        }
    }

    public void ResetMoons()
    {
        LockMoons(false);
        for (int j = 0; j < 5; j++)
        {
            for (int i = 0; i < moons.Count; i++)
            {
                Planet moon = moons[i];
                moon.GetComponent<KeplerOrbitMover>().enabled = false;
            }
            resetMoons = true;
            moonsParent.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            for (int i = 0; i < moons.Count; i++)
            {
                Planet moon = moons[i];
                Vector3 direction = moon.transform.position - moonsParent.transform.position;
                direction.y = 0;
                moon.transform.position = direction.normalized * moonsrelativeDistances[i].magnitude;
            }

            ReactivateMoonOrbits();
        }
    }

    private void ReactivateMoonOrbits()
    {
        for (int i = 0; i < moons.Count; i++)
        {
            Planet moon = moons[i];

            KeplerOrbitMover orbitMover = moon.GetComponent<KeplerOrbitMover>();
            orbitMover.enabled = true;
            orbitMover.LockOrbitEditing = false;
            orbitMover.SetUp();
            orbitMover.SetAutoCircleOrbit();
            orbitMover.ForceUpdateOrbitData();
            orbitMover.LockOrbitEditing = true;
        }

    }

    private void LockMoons(bool lockMoons)
    {
        if (moonsLocked != lockMoons)
        {
            foreach (Planet moon in moons)
            {
                moon.gameObject.GetComponent<KeplerOrbitMover>().LockOrbitEditing = lockMoons;
                moon.gameObject.GetComponent<KeplerOrbitMover>().VelocityHandle.localPosition = new Vector3(100,0,0);
            }
            moonsLocked = lockMoons;
        }
    }
}
