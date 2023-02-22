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
            minHight = distance;
        }
        if (distance > maxHight) {
            maxHight = distance;
        }
    }

    public float getMin() { return minHight; }
    public float getMax() { return maxHight; }

}
