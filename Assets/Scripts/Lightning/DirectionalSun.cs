using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalSun : MonoBehaviour
{
    [SerializeField] private Transform player;  // Player to point light towards
    private Transform sun;                      // Sun to point light from

    /// <summary>
    /// Initializes the script to make directional light point from the sun object
    /// </summary>
    /// <param name="sun">The sunubject to point light from</param>
    public void Initialize(Transform sun) {
        this.sun = sun;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.sun != null) 
        { 
            Vector3 sunToPlayerVector = Vector3.Normalize(player.position - sun.position);   
            transform.rotation = Quaternion.LookRotation(sunToPlayerVector);
        }

    }
}
