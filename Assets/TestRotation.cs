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
    private Vector3 directionBefore;
    private Vector3 directionAfter;


    // Start is called before the first frame update
    void Start()
    {
        directionBefore = Sun.transform.position - PlayerPlanet.transform.position;

        Sun.transform.position = pointToTeleportSunTo.transform.position;
        PlayerOrbitObject.transform.position = pointToTeleportPlanetTo.transform.position;

        directionAfter = Sun.transform.position - PlayerPlanet.transform.position;

        PlayerPlanet.transform.rotation *= Quaternion.FromToRotation(directionBefore, directionAfter);
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
