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
        Vector3 v = ObjectToLineUpWith.transform.position - OtherObject.transform.position;
        Vector3 dir = Vector3.Cross(v, ObjectToLineUpWith.transform.forward).normalized;
        Vector3 v2 = Vector3.ProjectOnPlane(transform.position, dir);
        transform.position = v2;
        transform.up = ObjectToLineUpWith.transform.up;

        //Vector3 direction = transform.position - ObjectToLineUpWith.transform.position;
        //transform.position = direction.normalized * relativeDistance;
    }
}
