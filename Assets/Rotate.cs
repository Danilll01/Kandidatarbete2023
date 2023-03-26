using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public Vector3 rotationAxis;

    public bool rotate = false;
    // Update is called once per frame
    void Update()
    {
        if (rotate)
        {
            transform.Rotate(rotationAxis, 10f * Time.deltaTime, Space.World);
            //transform.RotateAround(transform.position, rotationAxis, 10f * Time.deltaTime);
        }
    }
}
