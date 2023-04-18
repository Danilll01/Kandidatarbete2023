using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public int scale;
        public List<LSystem.Rule> rules;
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
    }

    /// <summary>
    /// <i>Note: function <see cref="GenerateCaves"/> must be run to generate the caves first</i>
    /// </summary>
    /// <returns><paramref name="caves">caves</paramref></returns>
    public Texture3D GetCaves() { return caves; }

    /// <summary>
    /// Generates the <see cref="Texture3D"/> for the caves
    /// </summary>
    /// <returns>Returns the <see cref="Texture3D"/> for the caves</returns>
    public Texture3D GenerateCaves()
    {
        return caves;
    }
}
