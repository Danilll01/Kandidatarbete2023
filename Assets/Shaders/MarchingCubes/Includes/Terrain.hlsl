#ifndef __terrain_hlsl_
#define __terrain_hlsl_

#include "../../Tools/Noise/Noise.hlsl"

struct Terrain
{
    int frequency;
    float amplitude;
};

float getTerrain(Terrain terrain, float3 pos)
{
    // If outside circle return 0
    float lengthPos = length(pos);
    if (lengthPos > 1)
        return 0;
    
    // Add noise to current point
    float density = (1 - lengthPos) * 255;
    float noiseOctave0 = (simplex.Evaluate(pos * (terrain.frequency << 0)) + 1) * .5 * terrain.amplitude;
    float noiseOctave1 = (simplex.Evaluate(pos * (terrain.frequency << 1)) + 1) * .5 * terrain.amplitude * terrain.amplitude;
    float noiseOctave2 = (simplex.Evaluate(pos * (terrain.frequency << 2)) + 1) * .5 * terrain.amplitude * terrain.amplitude * terrain.amplitude;
    density *= noiseOctave0 * noiseOctave1 * noiseOctave2;
        
    return density;
}

#endif