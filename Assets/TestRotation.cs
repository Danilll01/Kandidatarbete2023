using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotation : MonoBehaviour
{
    public GameObject Sun;
    public GameObject PlayerPlanet;
    public GameObject PlayerOrbitObject;
    public Transform pointToTeleportSunTo;
    public Transform pointToTeleportPlanetTo;
    private Quaternion rotationBefore;
    private Vector3 directionBefore;
    private Vector3 directionAfter;
    private Vector3 rotationAfter;


    // Start is called before the first frame update
    void Start()
    {
        rotationBefore = Sun.transform.rotation;
        directionBefore = Sun.transform.position - PlayerPlanet.transform.position;

        // Sun.transform.position = pointToTeleportSunTo.transform.position;
        //PlayerOrbitObject.transform.position = pointToTeleportPlanetTo.transform.position;

        Sun.transform.rotation = Quaternion.identity;

        rotationAfter = Sun.transform.rotation.eulerAngles;

        directionAfter = Sun.transform.position - PlayerPlanet.transform.position;

        PlayerPlanet.transform.rotation *= Quaternion.Inverse(rotationBefore) * Quaternion.FromToRotation(directionBefore, directionAfter);
        //PlayerPlanet.transform.rotation ;

    }

    private void OnDrawGizmos()
    {
        

        Gizmos.color = Color.red;


        if (directionBefore != null && directionAfter != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(PlayerPlanet.transform.position, directionBefore);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(PlayerPlanet.transform.position, directionAfter);

        }
    }
}
