using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Universe
{
    public const float gravitationalConstant = 0.0001f;
    public static int nrOfPlanets = 1;
    public static int seed = 1;
    public static System.Random random;

    public static void InitializeSeed()
    {
        random = new System.Random(seed);
    }
}