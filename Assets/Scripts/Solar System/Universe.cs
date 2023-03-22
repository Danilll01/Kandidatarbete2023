using System.Collections;
using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;

public static class Universe
{
    public const float gravitationalConstant = 2f;
    public static int nrOfPlanets = 3;
    public static int seed = 123456789;
    public static RandomX random;

    //Guaranteed set after Awake()
    public static PillPlayerController player = null;

    /// <summary>
    /// Initializes the System.Random with the given seed
    /// </summary>
    public static void InitializeRandomWithSeed()
    {
        random = new RandomX(seed);
        DisplayDebug.AddOrSetDebugVariable("Seed", seed);
        DisplayDebug.AddOrSetDebugVariable("Number of planets", nrOfPlanets);
    }
}