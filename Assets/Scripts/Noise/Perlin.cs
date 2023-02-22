//
// Perlin noise generator
// Manfred Hästmark, 2023
// https://github.com/Manfred-Hastmark/Seedable-3D-perlin-noise
//
// Inspired by original implementation by Ken Perlin, with added seeding algoritm for creating permutation array
// https://mrl.cs.nyu.edu/~perlin/paper445.pdf
//

using JetBrains.Annotations;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

/// <summary>
/// Class for generating seedable 3D perlin noise.
/// <br/> Has support for <see href="https://unity.com/">Unity</see> <see cref="Vector3"/>
/// <br/>
/// <br/>Sources and extra reading material on how perlin noise works:
/// <list type="bullet">
/// <item><description><see href="https://weber.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf">Simplex noise demystified</see></description></item>
/// <item><description><see href="https://github.com/keijiro/PerlinNoise"/></description></item>
/// <item><description><see href="https://www.youtube.com/watch?v=ip0XBBBY6A8">Interpolation</see> by <see href="https://www.youtube.com/@mathsgenie7808">Maths Genie</see></description></item>
/// <item><description><see href="https://www.youtube.com/watch?v=MJ3bvCkHJtE">Perlin Noise Explained Tutorial 2</see> by <see href="https://www.youtube.com/@Fataho">Fataho</see></description></item>
/// <item><description>Wikipedia, <see href="https://en.wikipedia.org/wiki/Perlin_noise">Perlin noise</see></description></item>
/// <item><description>TODO: Add explanation for seeding</description></item>
/// </list>
/// </summary>

public class Perlin
{
    public static float Noise(Vector3 vec)
    {
        return Noise(vec.x, vec.y, vec.z);
    }

    public static float Noise(float x, float y, float z)
    {
		//Find unit cube that contains x, y, z
		int X = (int)Mathf.Floor(x) & 255;
		int Y = (int)Mathf.Floor(y) & 255;
		int Z = (int)Mathf.Floor(z) & 255;

		//Find relative point on this cube
		x -= Mathf.Floor(x);
		y -= Mathf.Floor(y);
		z -= Mathf.Floor(z);

		//Compute fade curves for each x, y, z
		float u = fade(x);
		float v = fade(y);
		float w = fade(z);

		//Hash coordinates of each of the 8 cube corners
		int A = p[X] + Y & 255;
		int B = p[X + 1] + Y & 255;
		int AA = p[A] + Z & 255;
		int AB = p[A + 1] + Z & 255;
		int BA = p[B] + Z & 255;
		int BB = p[B + 1] + Z & 255;

		return	lerp(w, lerp(v, lerp(u, grad(p[AA], x, y, z),       // Add
				grad(p[BA], x - 1, y, z)),							// blended
				lerp(u, grad(p[AB], x, y - 1, z),					// results
						grad(p[BB], x - 1, y - 1, z))),				// from 8
				lerp(v, lerp(u, grad(p[AA + 1], x, y, z - 1),		// corners
						grad(p[BA + 1], x - 1, y, z - 1)),			// of cube
				lerp(u, grad(p[AB + 1], x, y - 1, z - 1),
						grad(p[BB + 1], x - 1, y - 1, z - 1))));
	}


    private static float grad(int hash, float x, float y, float z)
    {
		int h = hash & 15;                      // Convert lo 4 bits of hash code
		float u = h < 8 ? x : y;                 // into 12 gradient vectors.
		float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
		return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
	}

    private static float lerp(float t, float a, float b)
    {
		return a + t * (b - a);
	}

    private static float fade(float t)
    {
		return t * t * t * (t * (t * 6 - 15) + 10);
	}

    private static int[] p = {151,160,137,91,90,15,
		131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
		190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
		88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
		77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
		102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
		135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
		5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
		223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
		129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
		251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
		49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
		138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
		151
	};
}