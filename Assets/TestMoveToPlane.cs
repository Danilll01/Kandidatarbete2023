using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMoveToPlane : MonoBehaviour
{
    public GameObject ObjectToLineUpWith;
    public GameObject OtherObject;
    public float relativeDistance;

    private void Start()
    {
        relativeDistance = (ObjectToLineUpWith.transform.position - transform.position).magnitude;
    }

    // Update is called once per frame
    void Update()
    {

        /*
        Vector3 v2 = Vector3.ProjectOnPlane(transform.position, ObjectToLineUpWith.transform.TransformDirection(Vector3.up));
        transform.position = v2;
        //transform.up = ObjectToLineUpWith.transform.up;

        //Vector3 direction = transform.position - ObjectToLineUpWith.transform.position;
        //transform.position = direction.normalized * relativeDistance;
        */

        transform.position = ClosestPointOnPlane(ObjectToLineUpWith.transform.position, ObjectToLineUpWith.transform.TransformDirection(Vector3.up), transform.position);

        Debug.DrawRay(ObjectToLineUpWith.transform.position, ObjectToLineUpWith.transform.TransformDirection(Vector3.up) * 10, Color.blue);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 forward = ObjectToLineUpWith.transform.TransformDirection(Vector3.forward) * 10;
        //Gizmos.DrawLine(ObjectToLineUpWith.transform.position, forward);
    }

    public Vector3 ClosestPointOnPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    {
        return point + DistanceFromPlane(planeOffset, planeNormal, point) * planeNormal;
    }

    public float DistanceFromPlane(Vector3 planeOffset, Vector3 planeNormal, Vector3 point)
    { 
          return Vector3.Dot(planeOffset - point, planeNormal);
    }
}
