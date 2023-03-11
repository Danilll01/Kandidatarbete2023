#ifndef __noise_hlsl_
#define __noise_hlsl_

float lerp(float a, float b, float t);
float2 fade(float2 t);
float3 fade(float3 t);
float grad(int hash, float x, float y);
float grad(int hash, float x, float y, float z);

static const int p[257] =
{
    151, 160, 137, 91, 90, 15,
    131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23,
    190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
    88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
    77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244,
    102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
    135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
    5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
    223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
    129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
    251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107,
    49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
    138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180,
    151
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

// Helper functions
float lerp(float a, float b, float t)
{
    return a + t * (b - a);
}

float2 fade(float2 t)
{
    return t * t * t * (t * (t * 6 - 15) + 10);
}

float3 fade(float3 t)
{
    return t * t * t * (t * (t * 6 - 15) + 10);
}

float grad(int hash, float x, float y)
{
    return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y); //Let bit1 represent direction vectors (0, 1) and (0, -1)
}

float grad(int hash, float x, float y, float z)
{
    int h = hash & 15; // Convert lo 4 bits of hash code
    float u = h < 8 ? x : y; // into 12 gradient vectors.
    float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
    return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
}

#endif