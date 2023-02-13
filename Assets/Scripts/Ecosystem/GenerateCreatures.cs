using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateCreatures : MonoBehaviour
{
    PlanetBody planet;
    Vector3 planetCenter;

    [SerializeField] GameObject creature;
    

    // Start is called before the first frame update
    void Start()
    {
        planet = GetComponent<PlanetBody>();

        planetCenter = planet.transform.position;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            // How far do we raycast
            float distance = planet.radius;

            // The ray that will be cast
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Registered a hit
            if (Physics.Raycast(ray, out hit, distance))
            {
                Debug.Log("Hit!");
                GameObject newObject = Instantiate(creature, hit.point, Quaternion.identity, gameObject.transform);
                newObject.transform.rotation = Quaternion.FromToRotation(newObject.transform.up, hit.normal) * newObject.transform.rotation;
            
                //newObject.transform.rotation = Quaternion.FromToRotation(newObject.transform.position - planetCenter)
            }
        }
    }
}
