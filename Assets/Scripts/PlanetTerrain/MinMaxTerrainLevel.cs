using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinMaxTerrainLevel
{
    private float minHight;
    private float maxHight;

    /// <summary>
    /// Potencially uppdates the min or max distance
    /// </summary>
    /// <param name="distance"></param>
    public void UpdateMinMax(float distance) {
        if (distance < minHight) {
            minHight = distance;
        }
        if (distance > maxHight) {
            maxHight = distance;
        }
    }

}
