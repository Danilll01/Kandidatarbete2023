using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterTestingScript : MonoBehaviour
{
    public WaterHandler waterHandler;

    void Start()
    {
        waterHandler.InitializeTest(1000, Color.blue);
    }

}
