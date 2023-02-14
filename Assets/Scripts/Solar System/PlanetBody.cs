using System.Collections.Generic;
using UnityEngine;


public class PlanetBody : MonoBehaviour
{
    public float radius;
    public float surfaceGravity;
    public string bodyName = "TBT";
    Transform meshHolder;
    public float mass;
  
    /// <summary>
    /// Set up the values for the planets
    /// </summary>
    public void SetUpPlanetValues()
    {
        mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        meshHolder = transform.GetChild(0);
        meshHolder.localScale = Vector3.one * radius;
        gameObject.name = bodyName;
    }

}