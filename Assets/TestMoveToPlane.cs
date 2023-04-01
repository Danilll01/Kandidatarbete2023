using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMoveToPlane : MonoBehaviour
{
    public Transform ObjectToLineUpWith;
    public Transform OtherObject;



    // Update is called once per frame
    void Start()
    {
        Vector3 v = OtherObject.position - ObjectToLineUpWith.position;
        Vector3 dir = Vector3.Cross(v, Vector3.right).normalized;
        Vector3 v2 = Vector3.ProjectOnPlane(transform.position, dir);
        transform.position = v2;
    }
}
