//
// Perlin noise generator
// Manfred H?stmark, 2023
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
    static int permLength = 64;
    static string seed = "";

    #region Noise functions

    /// <summary>
    /// Calculates the noise value for given position vector <paramref name="pos"/>.
    /// <br/> Uses the perlin noise method, for more information see article,
    /// <see href="https://weber.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf">Simplex noise demystified</see>
    /// </summary>
    /// <param name="pos">Position to calulate noise at</param>
    /// <returns>noise value for given position <paramref name="pos"/></returns>
    static public float Noise(Vector3 pos)
    {
        return Noise(pos.x, pos.y, pos.z);
    }

    /// <summary>
    /// Calculates the noise value for given position (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>).
    /// <br/> Uses the perlin noise method, for more information see article, <see href="https://weber.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf">Simplex noise demystified</see>
    /// </summary>
    /// <param name="x">x-coord.</param>
    /// <param name="y">y-coord.</param>
    /// <param name="z">z-coord.</param>
    /// <returns>float between (-1, 1)</returns>
    static public float Noise(float x, float y, float z)
    {
        //Get (X, Y, Z) for the cube we are in
        int X = Mathf.FloorToInt(x) & (permLength - 1);
        int Y = Mathf.FloorToInt(y) & (permLength - 1);
        int Z = Mathf.FloorToInt(z) & (permLength - 1);

        //Get our position relative to the cube we are in
        x -= Mathf.FloorToInt(x);
        y -= Mathf.FloorToInt(y);
        z -= Mathf.FloorToInt(z);

        //Calculate the fade function
        float u = Fade(x);
        float v = Fade(y);
        float w = Fade(z);

        //Get the gradient vectors, important that shared corners in adjacent cubes have same gradient vectors,
        //recal perm[perm.length] = perm[0] (loops around)
        int A = (perm[X] + Y) & (permLength - 1);
        int B = (perm[X + 1] + Y) & (permLength - 1);
        int AA = (perm[A] + Z) & (permLength - 1);
        int AB = (perm[B] + Z) & (permLength - 1);
        int BA = (perm[A + 1] + Z) & (permLength - 1);
        int BB = (perm[B + 1] + Z) & (permLength - 1);

        //Please read article to understand belowe code
        return Interpolate(w, Interpolate(v, Interpolate(u, DotProduct(perm[AA], x, y, z), DotProduct(perm[AB], x - 1, y, z)), Interpolate(u, DotProduct(perm[BA], x, y - 1, z), DotProduct(perm[BB], x - 1, y - 1, z))), Interpolate(v, Interpolate(u, DotProduct(perm[AA + 1], x, y, z - 1), DotProduct(perm[AB + 1], x - 1, y, z - 1)), Interpolate(u, DotProduct(perm[BA + 1], x, y - 1, z - 1), DotProduct(perm[BB + 1], x - 1, y - 1, z - 1))));
    }

    #endregion

    #region getters and setters

    /// <summary>
    /// Change the seed for the perlin noise
    /// <br/>Each seed will generate a uniqe perlin noise
    /// </summary>
    /// <param name="seed">Seed for seeding the array</param>
    static public void SetSeed(string seed)
    {
        Perlin.seed = seed;
        perm = new int[permLength + 1]; //+1 so that we can loop and get same values on edges

        int nrSections = Mathf.CeilToInt((float)permLength / possibleVectors.Length);    //Calculate how many sections we need

        //Calculate how much of the seed is needed for each section
        int factorial = Factorial(possibleVectors.Length);
        int charsPerSection = NumberOfChars(factorial);

        List<int> permList = new List<int>();

        for (int i = 0; i < nrSections; i++)
        {
            int sectionSeed = (int)(ExtractNumber(ref seed, charsPerSection) % (uint)factorial);
            permList.AddRange(GetPerm(possibleVectors.Length - 1, sectionSeed));
        }

        int[] permArr = permList.ToArray();

        for (int i = 0; i < perm.Length - 1; i++)
        {
            perm[i] = permArr[i];
        }
        perm[perm.Length - 1] = perm[0];
    }

    /// <returns>current seed</returns>
    static public string GetSeed()
    {
        return seed;
    }

    /// <summary>
    /// Sets the size of the permutation array to 2^<paramref name="size"/>
    /// </summary>
    static public void SetSize(int size)
    {
        permLength = (int)Mathf.Pow(2, size);
        SetSeed(seed);
    }

    /// <returns>log2(x) where x=length of perm array</returns>
    static public int GetSize()
    {
        return Mathf.FloorToInt(Mathf.Log(permLength, 2));
    }

    #endregion

    #region Private permutaion generation functions

    /// <summary>
    /// Each <paramref name="index"/> corresponds to one unique permutation of the number from 0 to <paramref name="amount"/>,
    /// <br/>see documentation for how the algoritm works 
    /// </summary>
    /// <param name="amount">Upper bound of numbers to permute</param>
    /// <param name="index">Index to choose permutation</param>
    /// <returns>Returns the given permutation</returns>
    static List<int> GetPerm(int amount, int index)
    {
        return GetPerm(0, amount, index);
    }


    /// <summary>
    /// Each <paramref name="index"/> corresponds to one unique permutation of the number from <paramref name="lower"/> to <paramref name="upper"/>,
    /// <br/>see documentation for how the algoritm works 
    /// </summary>
    /// <param name="lower">Lower bound of numbers to permute</param>
    /// <param name="upper">Upper bound of numbers to permute</param>
    /// <param name="index">Index to choose permutation</param>
    /// <returns>Returns the given permutation</returns>
    static List<int> GetPerm(int lower, int upper, int index)
    {
        int range = upper - lower;
        if (range == 0)
        {
            return new List<int>() { upper };
        }
        int length = Factorial(range + 1);
        index %= length;
        int lowersRow = Mathf.FloorToInt((((float)index) / ((float)length)) * ((float)range + 1));

        List<int> subPerm = GetPerm(lower + 1, upper, index);
        subPerm.Insert(lowersRow, lower);
        return subPerm;
    }

    #endregion

    #region Private helper functions

    /// <summary>
    /// Extract number from the string <paramref name="s"/>. <br/>
    /// Each character is squished to a 2-decimal number.
    /// </summary>
    /// <param name="s">ref to string to extract from</param>
    /// <param name="len">how many characters are needed in the return int</param>
    /// <returns>Extracted number</returns>
    static uint ExtractNumber(ref string s, int len)
    {
        len = Mathf.CeilToInt(len / 2f);
        int res = 0;
        string newS = "";
        for (int i = 0; i < Mathf.Min(len, s.Length); i++)
        {
            res += s[i] % 100 * (int)Mathf.Pow(100, i); //Each characters take up 5 spaces
        }
        if (len > s.Length)
        {
            s = newS;
            return (uint)res;
        }
        for (int i = len; i < s.Length; i++)
        {
            newS += s[i];
        }
        s = newS;
        return (uint)res;
    }

    /// <summary>
    /// Calculates how many characters are used to represent n, for example inputting 4503 returns 4
    /// </summary>
    /// <returns>Number of characters in n</returns>
    static int NumberOfChars(int n)
    {
        int res = 1;
        while ((n /= 10) != 0) { res++; }
        return res;
    }

    /// <summary>
    /// Calculates the factorial of fact
    /// </summary>
    /// <param name="fact">Number to calculate factorial with</param>
    /// <returns>Returns the factorial</returns>
    static int Factorial(int fact)
    {
        int res = 1;
        for (int i = 1; i < fact + 1; i++)
        {
            res *= i;
        }
        return res;
    }

    /// <summary>
    /// The interpolation function makes a linear interpolation using the fade value <paramref name="t"/>, 
    /// this is used to smooth transitions between point <paramref name="a"/> and point <paramref name="b"/>
    /// <br/> See following video for explanation of interpolation, <see href="https://www.youtube.com/watch?v=ip0XBBBY6A8">Interpolation</see> 
    /// by <see href="https://www.youtube.com/@mathsgenie7808">Maths Genie</see>
    /// </summary>
    /// <param name="t">Fade value</param>
    /// <param name="a">Point a</param>
    /// <param name="b">Point b</param>
    /// <returns>Returns the smoothed of value from point <paramref name="a"/> to <paramref name="b"/></returns>
    static float Interpolate(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    /// <summary>
    /// Fade function used to create a smoother transition between noise points, 
    /// <br/>this function is the recomended perlin noise fade function, 
    /// <br/>see article <see href="https://weber.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf">Simplex noise demystified</see> for more information
    /// </summary>
    /// <param name="t">Value to fade</param>
    /// <returns>Returns a fade value for use in <see cref="Interpolation"/></returns>
    static float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    /// <summary>
    /// Calculates the dot product between <paramref name="vector"/> and vector (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>)
    /// </summary>
    /// <param name="vector">Vector on form 0bXX_YY_ZZ, where XX, YY and ZZ can be of value 00=0, 01=1, 10=-1</param>
    /// <param name="x">x-coord.</param>
    /// <param name="y">y-coord.</param>
    /// <param name="z">z-coord.</param>
    /// <returns>dot product</returns>
    static float DotProduct(int vector, float x, float y, float z)
    {
        x *= (vector & 0b11_00_00) == 0 ? 0 : (vector & 0b11_00_00) == 0b01_00_00 ? 1 : -1;
        y *= (vector & 0b00_11_00) == 0 ? 0 : (vector & 0b00_11_00) == 0b00_01_00 ? 1 : -1;
        z *= (vector & 0b00_00_11) == 0 ? 0 : (vector & 0b00_00_11) == 0b00_00_01 ? 1 : -1;
        return x + y + z;
    }

    /// <summary>
    /// Recomended gridvectors for perlin noise represented in bitformat 00=0, 01=1, 10=-1.
    /// <br/>Retrieved from article <see href="https://weber.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf">Simplex noise demystified</see>
    /// </summary>
    static int[] possibleVectors =
    {
        0b00_01_01,
        0b00_10_01,
        0b00_01_10,
        0b00_10_10,
        0b01_00_01,
        0b10_00_01,
        0b01_00_10,
        0b10_00_10,
        0b01_01_00,
        0b10_01_00,
        0b01_10_00,
        0b10_10_00
    };

    //Default permarray (seed = 0)
    static int[] perm = {
        0b00_01_01,
        0b00_10_01,
        0b00_01_10,
        0b00_10_10,
        0b01_00_01,
        0b10_00_01,
        0b01_00_10,
        0b10_00_10,
        0b01_01_00,
        0b10_01_00,
        0b01_10_00,
        0b10_10_00,
        0b00_01_01,
        0b00_10_01,
        0b00_01_10,
        0b00_10_10,
        0b01_00_01,
        0b10_00_01,
        0b01_00_10,
        0b10_00_10,
        0b01_01_00,
        0b10_01_00,
        0b01_10_00,
        0b10_10_00,
        0b00_01_01,
        0b00_10_01,
        0b00_01_10,
        0b00_10_10,
        0b01_00_01,
        0b10_00_01,
        0b01_00_10,
        0b10_00_10,
        0b00_01_01
    };

    #endregion
}