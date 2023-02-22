using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MinMaxTerrainLevel
{
    private float minHight = Mathf.Infinity;
    private float maxHight = -Mathf.Infinity;

    /// <summary>
    /// Potencially uppdates the min or max distance
    /// </summary>
    /// <param name="distance"></param>
    public void UpdateMinMax(Vector3 vertex) {
        float distance = math.length(vertex);

        if (distance < minHight && distance > 1) {
            //minHight = distance;
        }
        if (distance > maxHight) {
            maxHight = distance;
        }
    }

    /// <summary>
    /// Sets the minimum hight the color should begin at
    /// </summary>
    /// <param name="minHight"></param>
    public void SetMin(float minHight) {
        this.minHight = minHight;
    }

    public float GetMin() { return minHight; }
    public float GetMax() { return maxHight; }

}
