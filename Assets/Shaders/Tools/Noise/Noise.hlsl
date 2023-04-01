#ifndef __noise_hlsl_
#define __noise_hlsl_

float lerp(float a, float b, float t);
float fade(float t);
float2 fade(float2 t);
float3 fade(float3 t);
float grad(int hash, float x);
float grad(int hash, float x, float y);
float grad(int hash, float x, float y, float z);

static const int p[512] =
{ 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180, 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180 };

static const int grad3[] =
{
    int3(1, 1, 0),
    int3(-1, 1, 0),
    int3(1, -1, 0),
    int3(-1, -1, 0),
    int3(1, 0, 1),
    int3(-1, 0, 1),
    int3(1, 0, -1),
    int3(-1, 0, -1),
    int3(0, 1, 1),
    int3(0, -1, 1),
    int3(0, 1, -1),
    int3(0, -1, -1)
};

class Perlin
{
    // Function declarations
    float Evaluate(float x, float y, float z);
    float Evaluate(float x, float y);
    float Evaluate(float3 pos);
    float Evaluate(float2 pos);
    
    // Evaluate at point (x, y, z)
    float Evaluate(float x, float y, float z)
    {
        return Evaluate(float3(x, y, z));
    }
    
    // Evaluate at point (x, y)
    float Evaluate(float x, float y)
    {
        return Evaluate(float2(x, y));
    }
    
    // Evaluate at pos
    float Evaluate(float3 pos)
    {
        int3 cube = (int3) floor(pos) & 255;

        pos -= floor(pos);

        float3 f = fade(pos);
    
        int A = p[cube.x] + cube.y & 255;
        int B = p[cube.x + 1] + cube.y & 255;
        int AA = p[A] + cube.z & 255;
        int AB = p[A + 1] + cube.z & 255;
        int BA = p[B] + cube.z & 255;
        int BB = p[B + 1] + cube.z & 255;
    
    
        return lerp(lerp(lerp(grad(p[AA], pos.x, pos.y, pos.z), // Add
			grad(p[BA], pos.x - 1, pos.y, pos.z), f.x), // blended
			lerp(grad(p[AB], pos.x, pos.y - 1, pos.z), // results
				grad(p[BB], pos.x - 1, pos.y - 1, pos.z), f.x), f.y), // from 8
			lerp(lerp(grad(p[AA + 1], pos.x, pos.y, pos.z - 1), // corners
				grad(p[BA + 1], pos.x - 1, pos.y, pos.z - 1), f.x), // of cube
				lerp(grad(p[AB + 1], pos.x, pos.y - 1, pos.z - 1),
					grad(p[BB + 1], pos.x - 1, pos.y - 1, pos.z - 1), f.x), f.y), f.z);
    }
    
    // Evaluate at pos
    float Evaluate(float2 pos)
    {
        //Find unit square that contains x, y
        int2 square = (int2) floor(pos) & 255;

	    //Find relative point in this square
        pos -= floor(pos);

	    //Compute fade curves for each x, y
        float2 f = fade(pos);

	    //Hash coordinates for each of the four corners
        int A = p[square.x] + square.y & 255;
        int B = p[square.x + 1] + square.y & 255;

	    //Add blended results from 4 corners of square
        return lerp(lerp(grad(p[A], pos.x, pos.y),
			grad(p[B], pos.x - 1, pos.y), f.x),
			lerp(grad(p[A + 1], pos.x, pos.y - 1),
				grad(p[B + 1], pos.x - 1, pos.y - 1), f.x), f.y);
    }
    
    float Evaluate(float x)
    {
        //Find unit interval that contains x
        int X = (int) floor(x) & 255;

		//Find relative point in this interval
        x -= floor(x);

		//Compute fade curve
        float u = fade(x);

		//Add blended results from edges of interval
        return lerp(u, grad(p[X], x), grad(p[X + 1], x - 1)) * 2;
    }
};

Perlin perlin;

// Evaluate at point UV using Scale
void EvaluatePerlin_float(float3 UV, float Scale, out float Out)
{
    Out = perlin.Evaluate(UV * Scale);
}
    
// Evaluate at point UV using Scale
void EvaluatePerlin_float(float2 UV, float Scale, out float Out)
{
    Out = perlin.Evaluate(UV * Scale);
}

// Evalute at point UV using Scale
void EvaluatePerlin_float(float UV, float Scale, out float Out)
{
    Out = perlin.Evaluate(UV * Scale);
}

class Simplex
{
    // Function declarations
    float Evaluate(float x, float y, float z);
    float Evaluate(float x, float y);
    float Evaluate(float3 pos);
    float Evaluate(float2 pos);
    
    // Evaluate at point (x, y, z)
    float Evaluate(float x, float y, float z)
    {
        return Evaluate(float3(x, y, z));
    }
    
    // Evaluate at point (x, y)
    float Evaluate(float x, float y)
    {
        return Evaluate(float2(x, y));
    }
    
    // Evaluate at pos
    float Evaluate(float3 pos)
    {
        float x = pos.x;
        float y = pos.y;
        float z = pos.z;
        
        float4 n; // Noise contributions from the four corners

        // Skewing/Unskewing factors for 3D
        static const float F3 = 1.0f / 3.0f;
        static const float G3 = 1.0f / 6.0f;

        // Skew the input space to determine which simplex cell we're in
        float s = (x + y + z) * F3; // Very nice and simple skew factor for 3D
        int3 i = floor(pos + s);
        float t = (i[0] + i[1] + i[2]) * G3;
        float3 pos0 = pos - (i - t);

        // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
        // Determine which simplex we are in.
        int3 i1, i2;
        if (pos0.x >= pos0.y)
        {
            if (pos0.y >= pos0.z)
            {
                i1 = int3(1, 0, 0);
                i2 = int3(1, 1, 0);
            }
            else if (pos0.x >= pos0.z)
            {
                i1 = int3(1, 0, 0);
                i2 = int3(1, 0, 1);
            }
            else
            {
                i1 = int3(0, 0, 1);
                i2 = int3(1, 0, 1);
            }
        }
        else
        { // x0<y0
            if (pos0.y < pos0.z)
            {
                i1 = int3(0, 0, 1);
                i2 = int3(0, 1, 1);
            }
            else if (pos0.x < pos0.z)
            {
                i1 = int3(0, 1, 0);
                i2 = int3(0, 1, 1);
            }
            else
            {
                i1 = int3(0, 1, 0);
                i2 = int3(1, 1, 0);
            }
        }

        // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
        // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
        // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
        // c = 1/6.
        float3 pos1 = pos0 - i1 + G3;
        float3 pos2 = pos0 - i2 + 2.0f * G3;
        float3 pos3 = pos0 - 1.0f + 3.0f * G3;

        // Work out the hashed gradient indices of the four simplex corners
        int ii = i[0] & 255;
        int jj = i[1] & 255;
        int kk = i[2] & 255;
        int gi0 = p[ii + p[jj + p[kk]]];
        int gi1 = p[ii + i1[0] + p[jj + i1[1] + p[kk + i1[2]]]];
        int gi2 = p[ii + i2[0] + p[jj + i2[1] + p[kk + i2[2]]]];
        int gi3 = p[ii + 1 + p[jj + 1 + p[kk + 1]]];

        // Calculate the contribution from the four corners
        float t0 = 0.6f - pos0.x * pos0.x - pos0.y * pos0.y - pos0.z * pos0.z;
        if (t0 < 0)
        {
            n[0] = 0.0;
        }
        else
        {
            t0 *= t0;
            n[0] = t0 * t0 * grad(gi0, pos0.x, pos0.y, pos0.z);
        }
        float t1 = 0.6f - pos1.x * pos1.x - pos1.y * pos1.y - pos1.z * pos1.z;
        if (t1 < 0)
        {
            n[1] = 0.0;
        }
        else
        {
            t1 *= t1;
            n[1] = t1 * t1 * grad(gi1, pos1.x, pos1.y, pos1.z);
        }
        float t2 = 0.6f - pos2.x * pos2.x - pos2.y * pos2.y - pos2.z * pos2.z;
        if (t2 < 0)
        {
            n[2] = 0.0;
        }
        else
        {
            t2 *= t2;
            n[2] = t2 * t2 * grad(gi2, pos2.x, pos2.y, pos2.z);
        }
        float t3 = 0.6f - pos3.x * pos3.x - pos3.y * pos3.y - pos3.z * pos3.z;
        if (t3 < 0)
        {
            n[3] = 0.0;
        }
        else
        {
            t3 *= t3;
            n[3] = t3 * t3 * grad(gi3, pos3.x, pos3.y, pos3.z);
        }
        // Add contributions from each corner to get the final noise value.
        // The result is scaled to stay just inside [-1,1]
        return 32.0f * (n[0] + n[1] + n[2] + n[3]);
    }
    
    // Evaluate at pos
    float Evaluate(float2 pos)
    {   
        float3 n; // Noise contributions from the three corners

        // Skewing/Unskewing factors for 2D
        static const float F2 = 0.366025403f; // F2 = (sqrt(3) - 1) / 2
        static const float G2 = 0.211324865f; // G2 = (3 - sqrt(3)) / 6   = F2 / (1 + 2 * K)

        // Skew the input space to determine which simplex cell we're in
        const float s = (pos.x + pos.y) * F2; // Hairy factor for 2D
        const int2 i = floor(pos + s);
        
        // Unskew the cell origin back to (x,y) space
        const float t = (i[0] + i[1]) * G2;
        
        const float2 pos0 = pos - (i - t);

        // For the 2D case, the simplex shape is an equilateral triangle.
        // Determine which simplex we are in.
        int2 i1; // Offsets for second (middle) corner of simplex in (i,j) coords
        if (pos0.x > pos0.y)
        { // lower triangle, XY order: (0,0)->(1,0)->(1,1)
            i1 = int2(1, 0);
        }
        else
        { // upper triangle, YX order: (0,0)->(0,1)->(1,1)
            i1 = int2(0, 1);
        }

        // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
        // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
        // c = (3-sqrt(3))/6
        const float2 pos1 = pos0 - i1 + G2;
        const float2 pos2 = pos0 - 1.0f + 2.0f * G2;

        // Work out the hashed gradient indices of the three simplex corners
        int ii = i[0] & 255;
        int jj = i[1] & 255;
        int gi0 = p[ii + p[jj]] & 255;
        int gi1 = p[ii + i1[0] + p[jj + i1[1]]] & 255;
        int gi2 = p[ii + 1 + p[jj + 1]] & 255;

        // Calculate the contribution from the first corner
        float t0 = 0.5f - pos0.x * pos0.x - pos0.y * pos0.y;
        if (t0 < 0.0f)
        {
            n[0] = 0.0f;
        }
        else
        {
            t0 *= t0;
            n[0] = t0 * t0 * grad(gi0, pos0.x, pos0.y);
        }

        // Calculate the contribution from the second corner
        float t1 = 0.5f - pos1.x * pos1.x - pos1.y * pos1.y;
        if (t1 < 0.0f)
        {
            n[1] = 0.0f;
        }
        else
        {
            t1 *= t1;
            n[1] = t1 * t1 * grad(gi1, pos1.x, pos1.y);
        }

        // Calculate the contribution from the third corner
        float t2 = 0.5f - pos2.x * pos2.x - pos2.y * pos2.y;
        if (t2 < 0.0f)
        {
            n[2] = 0.0f;
        }
        else
        {
            t2 *= t2;
            n[2] = t2 * t2 * grad(gi2, pos2.x, pos2.y);
        }

        // Add contributions from each corner to get the final noise value.
        // The result is scaled to return values in the interval [-1,1].
        return 45.23065f * (n[0] + n[1] + n[2]);
    }
};

Simplex simplex;

// Evaluate at point UV using Scale
void EvaluateSimplex_float(float3 UV, float Scale, out float Out)
{
    Out = simplex.Evaluate(UV * Scale);   
}
    
// Evaluate at point UV using Scale
void EvaluateSimplex_float(float2 UV, float Scale, out float Out)
{
    Out = simplex.Evaluate(UV * Scale);
}

// Helper functions
float lerp(float a, float b, float t)
{
    return a + t * (b - a);
}

float fade(float t)
{
    return t * t * t * (t * (t * 6 - 15) + 10);
}

float2 fade(float2 t)
{
    return t * t * t * (t * (t * 6 - 15) + 10);
}

float3 fade(float3 t)
{
    return t * t * t * (t * (t * 6 - 15) + 10);
}

float grad(int hash, float x)
{
    return (hash & 1) == 0 ? x : -x; //Let LSB represent direction vector -1 or 1
}

float grad(int hash, float x, float y) {
    const int h = hash & 0x3F; // Convert low 3 bits of hash code
    const float u = h < 4 ? x : y; // into 8 simple gradient directions,
    const float v = h < 4 ? y : x;
    return ((h & 1) ? -u : u) + ((h & 2) ? -2.0f * v : 2.0f * v); // and compute the dot product with (x,y).
}

float grad(int hash, float x, float y, float z)
{
    int h = hash & 15; // Convert lo 4 bits of hash code
    float u = h < 8 ? x : y; // into 12 gradient vectors.
    float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
    return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
}

#endif