using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LSystem;
using UnityEngine.Rendering.VirtualTexturing;

public class Caves
{
    private Texture3D caves;
    private CaveSettings caveSettings;

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

        int totalResolution = (1 << chunkResolution) * (resolution << 3);

        caves = new Texture3D(totalResolution, totalResolution, totalResolution, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };

        GenerateCaves(ref caves);
    }

    /// <summary>
    /// <i>Note: function <see cref="GenerateCaves"/> must be run to generate the caves first</i>
    /// </summary>
    /// <returns><paramref name="caves">caves</paramref></returns>
    public Texture3D GetCaves() { return caves; }

    private void GenerateCaves(ref Texture3D caves)
    {
        LSystem lSystem = new LSystem(caveSettings.settings);

        while(true)
        {
            lSystem.Step(out Vector3 position, out bool isFinished);

            if (0.0f > position.x || position.x > 1.0f)
                continue;
            if (0.0f > position.y || position.y > 1.0f)
                continue;
            if (0.0f > position.z || position.z > 1.0f)
                continue;

            caves.SetPixel(
                x: (int)(caves.width * position.x),
                y: (int)(caves.height * position.y),
                z: (int)(caves.depth * position.z),
                color: new Color(255, 255, 255, 0)); // Black pixel denotes cave spot, alpha is the value used for terraingeneration

            if (isFinished) break;
        }
        
    }
}
