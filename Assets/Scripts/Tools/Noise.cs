using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Noise
{
    public static class PerlinNoise
    {
        /// <summary>
        /// Evaluate noise at position
        /// </summary>
        public static float Evaluate(Vector2 pos)
        {
            return Evaluate(pos.x, pos.y);
        }

        /// <summary>
        /// Evaluate noise at position
        /// </summary>
        public static float Evaluate(float x, float y)
        {
            //Find unit square that contains x, y
            int X = (int)Mathf.Floor(x) & 255;
            int Y = (int)Mathf.Floor(y) & 255;

            //Find relative point in this square
            x -= Mathf.Floor(x);
            y -= Mathf.Floor(y);

            //Compute fade curves for each x, y
            float u = Details.Fade(x);
            float v = Details.Fade(y);

            //Hash coordinates for each of the four corners
            int A = Details.p[X] + Y & 255;
            int B = Details.p[X + 1] + Y & 255;

            //Add blended results from 4 corners of square
            return  Details.Lerp(v, Details.Lerp(u, Details.Grad(Details.p[A], x, y),
                                                    Details.Grad(Details.p[B], x - 1, y)),
                                    Details.Lerp(u, Details.Grad(Details.p[A + 1], x, y - 1),
                                                    Details.Grad(Details.p[B + 1], x - 1, y - 1)));
        }

        /// <summary>
        /// Evaluate noise at position
        /// </summary>
        public static float Evaluate(Vector3 pos)
        {
            return Evaluate(pos.x, pos.y, pos.z);
        }

        /// <summary>
        /// Evaluate noise at position
        /// </summary>
        public static float Evaluate(float x, float y, float z)
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
            float u = Details.Fade(x);
            float v = Details.Fade(y);
            float w = Details.Fade(z);

            //Hash coordinates of each of the 8 cube corners
            int A  = Details.p[X] + Y     & 255;
            int B  = Details.p[X + 1] + Y & 255;
            int AA = Details.p[A] + Z     & 255;
            int AB = Details.p[A + 1] + Z & 255;
            int BA = Details.p[B] + Z     & 255;
            int BB = Details.p[B + 1] + Z & 255;

            return Details.Lerp(w, Details.Lerp(v, Details.Lerp(u, Details.Grad(Details.p[AA], x, y, z),                // Add
                                                                   Details.Grad(Details.p[BA], x - 1, y, z)),           // blended
                                                   Details.Lerp(u, Details.Grad(Details.p[AB], x, y - 1, z),            // results
                                                                   Details.Grad(Details.p[BB], x - 1, y - 1, z))),      // from 8
                                   Details.Lerp(v, Details.Lerp(u, Details.Grad(Details.p[AA + 1], x, y, z - 1),        // corners
                                                                   Details.Grad(Details.p[BA + 1], x - 1, y, z - 1)),   // of cube
                                                   Details.Lerp(u, Details.Grad(Details.p[AB + 1], x, y - 1, z - 1),
                                                                   Details.Grad(Details.p[BB + 1], x - 1, y - 1, z - 1))));
        }
    }

    /// <summary>
    /// Details class only used as helper class, not to be used outside Noise.cs
    /// </summary>
    public static class Details
    {
        /// <summary>
        /// Performs linear interpolation between a and b using t
        /// </summary>
        public static float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        /// <summary>
        /// Fade function used for noise functions
        /// </summary>
        public static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        /// <summary>
        /// Returns one of the four direction vectors used for 2D noise
        /// </summary>
        /// <param name="hash">Any integer value</param>
        /// <param name="x">x-position</param>
        /// <param name="y">y-position</param>
        public static float Grad(int hash, float x, float y)
        {
            return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);	//Let bit1 represent direction vectors (0, 1) and (0, -1)
        }

        /// <summary>
        /// Returns one of the twelve recomended vectors for 3D noise
        /// </summary>
        /// <param name="hash">Any integer value</param>
        /// <param name="x">x-position</param>
        /// <param name="y">y-position</param>
        /// <param name="z">z-position</param>
        public static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;                      // Convert lo 4 bits of hash code
            float u = h < 8 ? x : y;                 // into 12 gradient vectors.
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>
        /// Integer array used to retrieve pseudo-random hash values
        /// </summary>
        public static int[] p = 
        {
            151,160,137,91,90,15,
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

    
}


