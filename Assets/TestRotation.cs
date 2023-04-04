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
    private Quaternion planetRotationBefore;
    private Vector3 directionBefore;
    private Vector3 directionBY;


    // Start is called before the first frame update
    void Start()
    {
        rotationBefore = Sun.transform.rotation;
        planetRotationBefore = PlayerPlanet.transform.rotation;

        Vector3 sunUpBefore = Sun.transform.up;

        float heightDiff = Sun.transform.position.y - PlayerPlanet.transform.position.y;

        directionBefore = Sun.transform.position - PlayerPlanet.transform.position;

        directionBY = directionBefore;
        directionBY.y = 0;
        directionBY = PlayerPlanet.transform.rotation * directionBY;

        Sun.transform.rotation = Quaternion.identity;
        Sun.transform.position = pointToTeleportSunTo.position;
        PlayerPlanet.transform.position = pointToTeleportPlanetTo.position;

        Vector3 newVector = Sun.transform.position + sunUpBefore * heightDiff - PlayerPlanet.transform.position;
        PlayerPlanet.transform.rotation *= Quaternion.FromToRotation(directionBY, newVector);
        PlayerPlanet.transform.rotation *= Quaternion.Inverse(rotationBefore);
        PlayerPlanet.transform.rotation *= planetRotationBefore;

    }

    private void OnDrawGizmos()
    {
        float heightDiff = Sun.transform.position.y - PlayerPlanet.transform.position.y;

        directionBefore = Sun.transform.position - PlayerPlanet.transform.position;
        Vector3 sunUpBefore = Sun.transform.up;

        directionBY = directionBefore;
        directionBY.y = 0;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(PlayerPlanet.transform.position, directionBY);

        Gizmos.color = Color.black;
        directionBY  = PlayerPlanet.transform.rotation * directionBY;
        Gizmos.DrawRay(PlayerPlanet.transform.position, directionBY);


        Gizmos.color = Color.red;
        sunUpBefore = PlayerPlanet.transform.rotation * sunUpBefore;
        Gizmos.DrawRay(Sun.transform.position, sunUpBefore * heightDiff);

        Gizmos.color = Color.green;
        Vector3 newVector = Sun.transform.position + sunUpBefore * heightDiff - PlayerPlanet.transform.position;
        Gizmos.DrawRay(PlayerPlanet.transform.position, newVector);
    }
}
