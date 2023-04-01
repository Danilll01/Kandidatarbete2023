using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MinMaxTerrainLevel
{
    private float minHight = Mathf.Infinity;
    private float maxHight = -Mathf.Infinity;
    private float waterLevel;
    private float delta = 5f;

    public List<Vector3> waterPoints = new List<Vector3>();

    public MinMaxTerrainLevel(float waterDiameter)
    {
        waterLevel = waterDiameter / 2;
    }

    /// <summary>
    /// Potencially uppdates the min or max distance
    /// </summary>
    /// <param name="distance"></param>
    public void UpdateMinMax(float distance) {
        //float distance = math.length(vertex);

        // This is not doing anything right now
        if (distance < minHight && distance > 1) {
            //minHight = distance;
        }

        // Sets the max distance if the new value is higher
        if (distance > maxHight) {
            maxHight = distance;
        }
    }

    public void UpdateWaterPoints(float distance, Vector3 vertex)
    {
        //float distance = math.length(vertex);
        //Debug.Log("Dist:" + distance);

        // Check if the vertex is above waterlevel
        
        if (distance < waterLevel + delta && distance > waterLevel - delta)
        {
            waterPoints.Add(vertex);
        }
        
    }

    /// <summary>
    /// Sets the minimum hight the color should begin at
    /// </summary>
    /// <param name="minHight">The minimum hight</param>
    public void SetMin(float minHight) {
        this.minHight = minHight;
    }

    /// <summary>
    /// Gets the minimum terrain hight value
    /// </summary>
    /// <returns>The minimum hight value</returns>
    public float GetMin() { return minHight; }

    /// <summary>
    /// Gets the maximim terrain hight value
    /// </summary>
    /// <returns>The maximim hight value</returns>
    public float GetMax() { return maxHight; }

}
