using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LSystem;
using UnityEngine.Rendering.VirtualTexturing;
using JetBrains.Annotations;

public class Caves
{
    private CaveSettings caveSettings;
    //private List<Vector3>[] caves; //Each list represents the caves for the corresponding chunk
    //private int noChunks;
    private CavePoint[,,] cavePoints;

    /// <summary>
    /// Settings for the caves
    /// </summary>
    [Serializable]
    public struct CaveSettings
    {
        public Settings settings;
        public int resolution;
    }

    /// <summary>
    /// Struct for holding a cavepoint
    /// </summary>
    public struct CavePoint
    {
        public bool set;
        public Vector3 position; 
        
        /// <summary>
        /// Initalize cavepoint
        /// </summary>
        public CavePoint(bool set, Vector3 position)
        {
            this.set = set;
            this.position = position;
        }

        /// <summary>
        /// Update values of point
        /// </summary>
        public void Set(bool set, Vector3 position)
        {
            this.set = set;
            this.position = position;
        }
    }

    /// <summary>
    /// Initializes the cave generator
    /// </summary>
    /// <param name="resolution"><see cref="Planet.resolution"/></param>
    /// <param name="chunkResolution"><see cref="ChunksHandler.chunkResolution"/></param>
    /// <param name="caveSettings">Settings for the caves</param>
    public Caves(int chunkResolution, CaveSettings caveSettings)
    {
        this.caveSettings = caveSettings;
        //noChunks = 1 << chunkResolution;

        //caves = new List<Vector3>[noChunks * noChunks * noChunks];
        //for (int i = 0; i < caves.Length; i++)
        //    caves[i] = new List<Vector3>();

        cavePoints = new CavePoint[caveSettings.resolution, caveSettings.resolution, caveSettings.resolution];
        for(int i = 0; i < caveSettings.resolution; i++)
            for(int j = 0; j < caveSettings.resolution; j++)
                for(int k = 0; k < caveSettings.resolution; k++) 
                    cavePoints[i, j, k] = new CavePoint(false, Vector3.zero);

        GenerateCaves();
    }

    /// <summary>
    /// Returns the cave points for the specified chunk
    /// </summary>
    /// <returns><paramref name="caves">caves</paramref></returns>
    public CavePoint[,,] GetCaves() { return cavePoints; }

    private void GenerateCaves()
    {
        LSystem lSystem = new LSystem(caveSettings.settings);

        while(true)
        {
            // Get next cavepoint
            lSystem.Step(out Vector3 position, out bool isFinished);

            // Check if cavepoint is outside bounding box for planet
            if (0.0f > position.x || position.x > 1.0f)
                continue;
            if (0.0f > position.y || position.y > 1.0f)
                continue;
            if (0.0f > position.z || position.z > 1.0f)
                continue;

            // Add point to correct caves list
            //caves[GetChunkIndex(position)].Add(position);
            cavePoints[(int)(position.x * caveSettings.resolution), (int)(position.x * caveSettings.resolution), (int)(position.x * caveSettings.resolution)].Set(true, position);

            if (isFinished) break;
        }  
    }

    /*
    // Check which chunk contains given point, note: point is in range [0, 1]
    private int GetChunkIndex(Vector3 point)
    {
        return (int) point.x * (noChunks - 1) + (int) point.y * (noChunks - 1) * noChunks + (int) point.z * (noChunks - 1) * noChunks * noChunks;
    }*/
}
