using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoonRotation : MonoBehaviour
{
    public Vector3 rotationAxis;
    private void Update()
    {
        transform.RotateAround(transform.position, rotationAxis, 20f * Time.deltaTime);
    } 
}
