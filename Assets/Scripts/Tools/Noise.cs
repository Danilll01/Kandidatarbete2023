using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine.UIElements;

namespace Noise
{
    /// <summary>
    /// Class implementing Perlin noise
    /// </summary>
    public static class Perlin
    {
        /// <summary>
        /// Evaluate noise at position
        /// </summary>
        public static float Evaluate(float x)
        {
            //Find unit interval that contains x
            int X = (int)Mathf.Floor(x) & 255;

            //Find relative point in this interval
            x -= Mathf.Floor(x);

            //Compute fade curve
            float u = Details.Fade(x);

            //Add blended results from edges of interval
            return Details.Lerp(u, Details.Grad(Details.p[X], x), Details.Grad(Details.p[X + 1], x - 1)) * 2;
        }

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
    /// Class implementing Simplex noise
    /// </summary>
    public static class Simplex
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
            float n0, n1, n2;   // Noise contributions from the three corners

            // Skewing/Unskewing factors for 2D
            const float F2 = 0.366025403f;  // F2 = (sqrt(3) - 1) / 2
            const float G2 = 0.211324865f;  // G2 = (3 - sqrt(3)) / 6   = F2 / (1 + 2 * K)

            // Skew the input space to determine which simplex cell we're in
            float s = (x + y) * F2;  // Hairy factor for 2D
            float xs = x + s;
            float ys = y + s;
            int i = Details.FastFloor(xs);
            int j = Details.FastFloor(ys);

            // Unskew the cell origin back to (x,y) space
            float t = (i + j) * G2;
            float X0 = i - t;
            float Y0 = j - t;
            float x0 = x - X0;  // The x,y distances from the cell origin
            float y0 = y - Y0;

            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            int i1, j1;  // Offsets for second (middle) corner of simplex in (i,j) coords
            if (x0 > y0)
            {   // lower triangle, XY order: (0,0)->(1,0)->(1,1)
                i1 = 1;
                j1 = 0;
            }
            else
            {   // upper triangle, YX order: (0,0)->(0,1)->(1,1)
                i1 = 0;
                j1 = 1;
            }

            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3-sqrt(3))/6

            float x1 = x0 - i1 + G2;            // Offsets for middle corner in (x,y) unskewed coords
            float y1 = y0 - j1 + G2;
            float x2 = x0 - 1.0f + 2.0f * G2;   // Offsets for last corner in (x,y) unskewed coords
            float y2 = y0 - 1.0f + 2.0f * G2;

            // Work out the hashed gradient indices of the three simplex corners
            int ii = i & 255;
            int jj = j & 255;
            int gi0 = Details.p[ii + Details.p[jj]] & 255;
            int gi1 = Details.p[ii + i1 + Details.p[jj + j1]] & 255;
            int gi2 = Details.p[ii + 1 + Details.p[jj + 1]] & 255;

            // Calculate the contribution from the first corner
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0.0f)
            {
                n0 = 0.0f;
            }
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Details.Grad(gi0, x0, y0);
            }

            // Calculate the contribution from the second corner
            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0.0f)
            {
                n1 = 0.0f;
            }
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Details.Grad(gi1, x1, y1);
            }

            // Calculate the contribution from the third corner
            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0.0f)
            {
                n2 = 0.0f;
            }
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Details.Grad(gi2, x2, y2);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return 45.23065f * (n0 + n1 + n2);
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
            float n0, n1, n2, n3; // Noise contributions from the four corners

            // Skewing/Unskewing factors for 3D
            const float F3 = 1.0f / 3.0f;
            const float G3 = 1.0f / 6.0f;

            // Skew the input space to determine which simplex cell we're in
            float s = (x + y + z) * F3; // Very nice and simple skew factor for 3D
            int i = Details.FastFloor(x + s);
            int j = Details.FastFloor(y + s);
            int k = Details.FastFloor(z + s);
            float t = (i + j + k) * G3;
            float X0 = i - t; // Unskew the cell origin back to (x,y,z) space
            float Y0 = j - t;
            float Z0 = k - t;
            float x0 = x - X0; // The x,y,z distances from the cell origin
            float y0 = y - Y0;
            float z0 = z - Z0;

            // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
            // Determine which simplex we are in.
            int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
            int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords
            if (x0 >= y0)
            {
                if (y0 >= z0)
                {
                    i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; // X Y Z order
                }
                else if (x0 >= z0)
                {
                    i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; // X Z Y order
                }
                else
                {
                    i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; // Z X Y order
                }
            }
            else
            { // x0<y0
                if (y0 < z0)
                {
                    i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; // Z Y X order
                }
                else if (x0 < z0)
                {
                    i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; // Y Z X order
                }
                else
                {
                    i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; // Y X Z order
                }
            }

            // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
            // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
            // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
            // c = 1/6.
            float x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2.0f * G3; // Offsets for third corner in (x,y,z) coords
            float y2 = y0 - j2 + 2.0f * G3;
            float z2 = z0 - k2 + 2.0f * G3;
            float x3 = x0 - 1.0f + 3.0f * G3; // Offsets for last corner in (x,y,z) coords
            float y3 = y0 - 1.0f + 3.0f * G3;
            float z3 = z0 - 1.0f + 3.0f * G3;

            // Work out the hashed gradient indices of the four simplex corners
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            int gi0 = Details.p[ii + Details.p[jj + Details.p[kk]]];
            int gi1 = Details.p[ii + i1 + Details.p[jj + j1 + Details.p[kk + k1]]];
            int gi2 = Details.p[ii + i2 + Details.p[jj + j2 + Details.p[kk + k2]]];
            int gi3 = Details.p[ii + 1 + Details.p[jj + 1 + Details.p[kk + 1]]];

            // Calculate the contribution from the four corners
            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0)
            {
                n0 = 0.0f;
            }
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Details.Grad(gi0, x0, y0, z0);
            }
            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0)
            {
                n1 = 0.0f;
            }
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Details.Grad(gi1, x1, y1, z1);
            }
            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0)
            {
                n2 = 0.0f;
            }
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Details.Grad(gi2, x2, y2, z2);
            }
            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0)
            {
                n3 = 0.0f;
            }
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * Details.Grad(gi3, x3, y3, z3);
            }
            // Add contributions from each corner to get the final noise value.
            // The result is scaled to stay just inside [-1,1]
            return 32.0f * (n0 + n1 + n2 + n3);
        }
    }

    public static class Worley
    {
        /// <summary>
        /// Evaluate cells at position
        /// </summary>
        public static float EvaluateCells(float2 pos, float angleOffset)
        {
            float X = Mathf.Floor(pos.x);
            float Y = Mathf.Floor(pos.y);
            float2 f = pos - new float2(X, Y);
            float2 res = new float2(8.0f, 0f);

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 lattice = new float2(x, y);
                    float2 offset = Details.randomVector(lattice + new float2(X, Y), angleOffset);
                    float2 diff = lattice + offset - f;
                    float d = Mathf.Sqrt(diff.x * diff.x + diff.y * diff.y);

                    if (d < res.x)
                    {
                        res = new float2(d, offset.x);
                    }
                }
            }
            return res.y;
        }

        /// <summary>
        /// Evaluate noise at position
        /// </summary>
        public static float EvaluateNoise(float2 pos, float angleOffset)
        {
            float X = Mathf.Floor(pos.x);
            float Y = Mathf.Floor(pos.y);
            float2 f = pos - new float2(X, Y);
            float res = 8.0f;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 lattice = new float2(x, y);
                    float2 offset = Details.randomVector(lattice + new float2(X, Y), angleOffset);
                    float2 diff = lattice + offset - f;
                    float d = Mathf.Sqrt(diff.x * diff.x + diff.y * diff.y);

                    if (d < res)
                    {
                        res = d;
                    }
                }
            }
            return res;
        }
    }


    /// <summary>
    /// Details class only used as helper class, not to be used outside Noise.cs
    /// </summary>
    public static class Details
    {
        /// <summary>
        /// Returns the largest integer that is less than or equal to the specified value
        /// </summary>
        public static int FastFloor(float x)
        {
            return x > 0 ? (int)x : (int)x - 1;
        }

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
        /// Returns the dot product with one of the two direction vectors used for 1D noise
        /// </summary>
        /// <param name="hash">Any integer value</param>
        /// <param name="x">x-position</param>
        public static float Grad(int hash, float x)
        {
            return (hash & 1) == 0 ? x : -x; //Let LSB represent direction vector -1 or 1
        }

        /// <summary>
        /// Returns the dot product with one of the four direction vectors used for 2D noise
        /// </summary>
        /// <param name="hash">Any integer value</param>
        /// <param name="x">x-position</param>
        /// <param name="y">y-position</param>
        public static float Grad(int hash, float x, float y)
        {
            int h = hash & 0x3F; // Convert low 3 bits of hash code
            float u = h < 4 ? x : y; // into 8 simple gradient directions,
            float v = h < 4 ? y : x;
            return ((h & 1) == 1 ? -u : u) + ((h & 2) == 2 ? -2.0f * v : 2.0f * v); // and compute the dot product with (x,y).
        }

        /// <summary>
        /// Returns the dot product with one of the twelve recomended vectors for 3D noise
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
        /// Returns a random vector
        /// </summary>
        public static Vector2 randomVector(Vector2 UV, float offset)
        {
            Matrix<double> m = DenseMatrix.OfArray(new double[,] {
                { 15.27f, 47.63f }, 
                { 99.41f, 89.98f } });

            Matrix<double> uv = DenseMatrix.OfArray(new double[,] { { UV.x, UV.y } });

            UV.x = Mathf.Sin((float)(uv * m)[0, 0]) % 1;
            UV.y = Mathf.Sin((float)(uv * m)[0, 1]) % 1;

            return new Vector2(Mathf.Sin(UV.y * +offset) * 0.5f + 0.5f, Mathf.Cos(UV.x * offset) * 0.5f + 0.5f);
        }

        /// <summary>
        /// Integer array used to retrieve pseudo-random hash values
        /// </summary>
        public static int[] p =
        { 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180, 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180 };

    }
}


