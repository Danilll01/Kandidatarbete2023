#ifndef __terrain_hlsl_
#define __terrain_hlsl_

#include "../../Tools/Noise/Noise.hlsl"

struct TerrainLayer
{
    float strength;
    float baseRoughness;
    float roughness;
    float persistance;
    float3 centre;
    int numLayers;
};

struct Terrain
{
    int frequency;
    float amplitude;
};

float getTerrain(Terrain terrain, float3 pos, RWStructuredBuffer<TerrainLayer> terrainLayers, int numTerrainLayers)
{
    float3 pointOnSphere = pos / length(pos);
    /*
    float noiseValues[numTerrainLayers];
    
    
    for (int i = 0; i < numTerrainLayers; i++)
    {
        TerrainLayer terrainLayer = terrainLayers[i];
        
        float amplitude = 1;
        float frequency = terrainLayer.baseRoughness;
        
        for (int j = 0; j < terrainLayer.numLayers; i++)
        {
            noiseValues[i] += (simplex.Evaluate((pointOnSphere + terrainLayer.centre) * frequency) + 1) * 0.5f * amplitude;
            frequency *= terrainLayer.roughness;
            amplitude += terrainLayer.persistance;
        }
        
        noiseValues[i] *= terrainLayer.strength;
    }*/
    
    
    
    float noiseOctave0 = (simplex.Evaluate(pointOnSphere * terrainLayers[0].baseRoughness) + 1) * 0.5f;
    float noiseOctave1 = (simplex.Evaluate(pointOnSphere * 2*2.5) + 1) * 0.5f * terrainLayers[0].persistance;
    float noiseOctave2 = (simplex.Evaluate(pointOnSphere * 2 * 2.5 * 2.5) + 1) * 0.5f * terrainLayers[0].persistance * terrainLayers[0].persistance;
    
    float noiseValue = noiseOctave0 + noiseOctave1 + noiseOctave2;
    
    noiseValue /= 16;
    
    return length(pos) < (1 - noiseValue) ? ((1 - noiseValue) - length(pos)) * 255 : 0;
    
    /*
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
      
    return density;*/
}

#endif