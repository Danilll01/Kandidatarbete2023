using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitTest : MonoBehaviour
{
    public GameObject orbitObject;
    public Vector3 rotationAxis;
    public float objectRadius;
    public float offset;

    // Update is called once per frame
    void OnValidate()
    {
        transform.position = orbitObject.transform.position + RandomPointOnCircleEdge(objectRadius + offset);
    }

    void Update()
    {
        RotateAroundAxis();
    }

    // Gives back a random position on the edge of a circle given the radius of the circle
    private Vector3 RandomPointOnCircleEdge(float radius)
    {
        var orthogonalVector = Vector3.RotateTowards(rotationAxis, -rotationAxis, Mathf.PI / 2f, 0f);
        var anotherOrthogonalVector = Quaternion.AngleAxis(Random.value * 360f, rotationAxis) * orthogonalVector;
        Vector3 randomVector = Vector3.Scale(anotherOrthogonalVector, new Vector3(UnityEngine.Random.Range(1, 360), UnityEngine.Random.Range(1, 360), UnityEngine.Random.Range(1, 360)));
        var vector3 = randomVector.normalized * radius;
        return new Vector3(vector3.x, vector3.y, vector3.z);
    }

    private void RotateAroundAxis()
    {
        transform.RotateAround(orbitObject.transform.position, rotationAxis, 10 * Time.deltaTime);
    }
}
