#ifndef __terrain_hlsl_
#define __terrain_hlsl_

#include "../Tools/Noise/Noise.hlsl"

struct TerrainLayer
{
    float strength;
    float baseRoughness;
    float roughness;
    float persistance;
    float3 centre;
    int numLayers;
};

float getTerrain(float3 pos, RWStructuredBuffer<TerrainLayer> terrainLayers, int numTerrainLayers)
{
    float3 pointOnSphere = pos / length(pos);
   
    float noiseValue = 0;
    
    for (int i = 0; i < 1; i++)
    {
        float amplitude = 1;
        float frequency = terrainLayers[i].baseRoughness;
        float noiseLayer;
    
        for (int j = 0; j < terrainLayers[i].numLayers; j++)
        {
            noiseLayer += (simplex.Evaluate(pointOnSphere * frequency) + 1) * 0.5f * amplitude;
            frequency *= terrainLayers[i].roughness;
            amplitude *= terrainLayers[i].persistance;
        }
        
        noiseLayer *= terrainLayers[i].strength;
        
        noiseValue += noiseLayer;
    }
    
    return length(pos) < (1 - noiseValue) ? ((1 - noiseValue) - length(pos)) * 255 : 0;
}

#endif