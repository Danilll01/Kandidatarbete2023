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
    private List<Vector3>[] caves; //Each list represents the caves for the corresponding chunk
    private int noChunks;

    /// <summary>
    /// Settings for the caves
    /// </summary>
    [Serializable]
    public struct CaveSettings
    {
        public Settings settings;
    }

    /// <summary>
    /// Initializes the cave generator
    /// </summary>
    /// <param name="resolution"><see cref="Planet.resolution"/></param>
    /// <param name="chunkResolution"><see cref="ChunksHandler.chunkResolution"/></param>
    /// <param name="caveSettings">Settings for the caves</param>
    public Caves(int resolution, int chunkResolution, CaveSettings caveSettings)
    {
        this.caveSettings = caveSettings;
        noChunks = 1 << chunkResolution;

        caves = new List<Vector3>[noChunks * noChunks * noChunks];

        for(int i = 0; i < caves.Length; i++)
            caves[i] = new List<Vector3>();

        GenerateCaves(ref caves);
    }

    /// <summary>
    /// Returns the cave points for the specified chunk
    /// </summary>
    /// <returns><paramref name="caves">caves</paramref></returns>
    public List<Vector3> GetCaves(int chunkIndex) { return caves[chunkIndex]; }

    private void GenerateCaves(ref List<Vector3>[] caves)
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
            caves[GetChunkIndex(position)].Add(position);

            if (isFinished) break;
        }  
    }

    // Check which chunk contains given point, note: point is in range [0, 1]
    private int GetChunkIndex(Vector3 point)
    {
        return (int) point.x * (noChunks - 1) + (int) point.y * (noChunks - 1) * noChunks + (int) point.z * (noChunks - 1) * noChunks * noChunks;
    }
}
