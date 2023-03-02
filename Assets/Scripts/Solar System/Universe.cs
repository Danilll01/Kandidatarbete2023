using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Universe
{
    public const float gravitationalConstant = 2f;
    public static int nrOfPlanets = 3;
    public static int seed = 123;
    public static System.Random random;

    //Set after Awake()
    public static PillPlayerController player = null;

    /// <summary>
    /// Initializes the System.Random with the given seed
    /// </summary>
    public static void InitializeRandomWithSeed()
    {
        random = new System.Random(seed);
        DisplayDebug.AddOrSetDebugVariable("Seed", seed);
        DisplayDebug.AddOrSetDebugVariable("Number of planets", nrOfPlanets);
    }
}