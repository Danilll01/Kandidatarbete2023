using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitTest : MonoBehaviour
{
    public GameObject orbitObject;
    public Vector3 rotationAxis;
    public float objectRadius;
    public float offset;

    public Transform Target;

    public float RotationSpeed = 1;

    public float CircleRadius = 1;

    public float ElevationOffset = 0;

    private Vector3 positionOffset;

    [Range(0, 360)]
    public float StartAngle = 0;

    public bool UseTargetCoordinateSystem = false;

    public bool LookAtTarget = false;

    private float angle;
    public Vector3 relativeDistance = Vector3.zero;
    public float orbitDistance = 3.0f;
    public bool once = true;

    [SerializeField] private GameObject target;
    [SerializeField] private float degreesPerSecond = 45;

    private Vector3 planetToSun = new Vector3();
    public GameObject sun;

    // Update is called once per frame
    void Update()
    {
        planetToSun = sun.transform.position - target.transform.position;
        var orthogonalVector = Vector3.RotateTowards(planetToSun, -planetToSun, Mathf.PI / 2f, 0f);
        orthogonalVector.y = 0;
        transform.RotateAround(target.transform.position, orthogonalVector, degreesPerSecond * Time.deltaTime);
    }

    /*
    // Update is called once per frame
    void Start()
    {
        //transform.position = orbitObject.transform.position + RandomPointOnCircleEdge(objectRadius + offset);
        //angle = StartAngle;
        if (Target != null)
        {
            relativeDistance = transform.position - Target.position;
        }
    }

    void LateUpdate()
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

        if (Target != null)
        {
            // Keep us at the last known relative position
            transform.position = (Target.position + relativeDistance);
            transform.position = RotateAroundModified(orbitObject.transform.position, rotationAxis, 10 * Time.deltaTime);
        }

        if (once)
        {
            // transform.position *= orbitDistance;
            var newPos = (transform.position - Target.position).normalized * orbitDistance;
            newPos += Target.position;
            transform.position = newPos;
            once = false;
        }
        relativeDistance = transform.position - Target.position;
    }

    Vector3 RotateAroundModified(Vector3 center, Vector3 axis, float angle)
    {
        Vector3 pos = transform.position;
        Quaternion rot = Quaternion.AngleAxis(angle, axis); // get the desired rotation
        Vector3 dir = pos - center; // find current direction relative to center
        dir = rot * dir; // rotate the direction
        pos = center + dir; // define new position
        return pos;
    }

   


    private void LateUpdate()
    {
        // Define the position the object must rotate around
        Vector3 position = Target != null ? Target.position : Vector3.zero;

        Vector3 positionOffset = ComputePositionOffset(angle);

        // Assign new position
        transform.position = position + positionOffset;

        // Rotate object so as to look at the target
        if (LookAtTarget)
            transform.rotation = Quaternion.LookRotation(position - transform.position, Target == null ? Vector3.up : Target.up);

        angle += Time.deltaTime * RotationSpeed;
    }

    private Vector3 ComputePositionOffset(float a)
    {
        a *= Mathf.Deg2Rad;

        // Compute the position of the object
        Vector3 positionOffset = new Vector3(
            Mathf.Cos(a) * CircleRadius,
            Mathf.Tan(a) * CircleRadius,
            Mathf.Sin(a) * CircleRadius
        );
        positionOffset += Vector3.Scale(positionOffset, rotationAxis);
        positionOffset = positionOffset.normalized * CircleRadius;

        // Change position if the object must rotate in the coordinate system of the target
        // (i.e in the local space of the target)
        if (Target != null && UseTargetCoordinateSystem)
            positionOffset = Target.TransformVector(positionOffset);

        return positionOffset;
    }

#if UNITY_EDITOR

    [SerializeField]
    private bool drawGizmos = true;

    

#endif
    */
}
