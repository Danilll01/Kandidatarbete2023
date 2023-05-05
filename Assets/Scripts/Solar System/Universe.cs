using System.Collections;
using System.Collections.Generic;
using ExtendedRandom;
using UnityEngine;

public static class Universe
{
    public const float gravitationalConstant = 2f;
    public static int nrOfPlanets = 3;
    public static int seed = 393165;
    public static RandomX random;

    //Guaranteed set after Awake()
    public static PillPlayerController player = null;
    public static Transform spaceShip = null;
    public static Transform sunPosition = null;

    /// <summary>
    /// Initializes the System.Random with the given seed
    /// </summary>
    public static void InitializeRandomWithSeed()
    {
        random = new RandomX(seed);
        #if DEBUG || UNITY_EDITOR
        DisplayDebug.AddOrSetDebugVariable("Seed", seed);
        DisplayDebug.AddOrSetDebugVariable("Number of planets", nrOfPlanets);
        #endif
    }

    public static void DrawGizmosCircle(Vector3 pos, Vector3 normal, float radius, int numSegments)
    {
        Vector3 temp = (normal.x < normal.z) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f);
        Vector3 forward = Vector3.Cross(normal, temp).normalized;
        Vector3 right = Vector3.Cross(forward, normal).normalized;

        Vector3 prevPt = pos + (forward * radius);
        float angleStep = (Mathf.PI * 2f) / numSegments;
        for (int i = 0; i < numSegments; i++)
        {
            float angle = (i == numSegments - 1) ? 0f : (i + 1) * angleStep;
            Vector3 nextPtLocal = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            Vector3 nextPt = pos + (right * nextPtLocal.x) + (forward * nextPtLocal.z);

            Gizmos.DrawLine(prevPt, nextPt);

            prevPt = nextPt;
        }
    }
}