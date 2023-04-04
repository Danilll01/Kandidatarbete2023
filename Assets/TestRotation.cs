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
    private Vector3 directionBY;


    // Start is called before the first frame update
    void Start()
    {
        rotationBefore = Sun.transform.rotation;
        directionBefore = Sun.transform.position - PlayerPlanet.transform.position;
        Vector3 sunUpBefore = Sun.transform.up;

        float heightDiff = Sun.transform.position.y - PlayerPlanet.transform.position.y;

        directionBefore = Sun.transform.position - PlayerPlanet.transform.position;

        directionBY = directionBefore;
        directionBY.y = 0;

        Sun.transform.rotation = Quaternion.identity;

        Vector3 resetVector = sunUpBefore;
        directionAfter = resetVector - PlayerPlanet.transform.position;

        Vector3 newVector = Sun.transform.position + sunUpBefore * heightDiff - PlayerPlanet.transform.position;
        PlayerPlanet.transform.rotation *= Quaternion.FromToRotation(directionBY, newVector);
        PlayerPlanet.transform.rotation *= Quaternion.Inverse(rotationBefore);

    }

    private void OnDrawGizmos()
    {
        float heightDiff = Sun.transform.position.y - PlayerPlanet.transform.position.y;

        directionBefore = Sun.transform.position - PlayerPlanet.transform.position;
        Vector3 sunUpBefore = Sun.transform.up;

        directionBY = directionBefore;
        directionBY.y = 0;

        Vector3 resetVector = sunUpBefore;
        directionAfter = resetVector - PlayerPlanet.transform.position;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(PlayerPlanet.transform.position, directionBY);


        Gizmos.color = Color.red;
        Gizmos.DrawRay(Sun.transform.position, Sun.transform.up * heightDiff);

        Gizmos.color = Color.green;
        Vector3 newVector = Sun.transform.position + Sun.transform.up * heightDiff - PlayerPlanet.transform.position;
        Gizmos.DrawRay(PlayerPlanet.transform.position, newVector);
    }
}
