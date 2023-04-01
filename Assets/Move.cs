using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        transform.position += Vector3.forward * Time.deltaTime * 0.5f;  
    }
}
