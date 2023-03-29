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

// Note, TemperatureRoughness and MountainTemperatureAffect must be in the range 0-1
struct BiomeSettings
{
    float Seed;
    float MountainFrequency;
    float TemperatureFrequency;
    float TemperatureRoughness;
    float MountainTemperatureAffect;
    float TreeFrequency;
};

float getTerrain(float3 pos, RWStructuredBuffer<TerrainLayer> terrainLayers, int numTerrainLayers)
{
    float3 pointOnSphere = pos / length(pos);
   
    float noiseValue = 0;
    
    for (int i = 0; i < numTerrainLayers; i++)
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


// Note, TemperatureRoughness and MountainTemperatureAffect must be in the range 0-1
void EvaluateBiomeMap_float(float3 UV, float Seed, float MountainFrequency, float TemperatureFrequency, float TemperatureRoughness, float MountainTemperatureAffect, float TreeFrequency, out float3 Out)
{
    // Normalize position
    UV = normalize(UV);
    
    // Get noisevalue for each map
    float mountainNoise = (simplex.Evaluate(float3(UV.x + Seed, UV.y, UV.z) * MountainFrequency) + 1) * .5f;
    float tempNoise = (simplex.Evaluate(float3(UV.x, UV.y + Seed, UV.z) * TemperatureFrequency) + 1) * .5f;
    float treeNoise = (simplex.Evaluate(float3(UV.x, UV.y, UV.z + Seed) * TreeFrequency) + 1) * .5f;
    
    // Generate temperature map
    float tempValue = 1 - abs(UV.y);
    
    // Rough up the temperature map
    tempValue *= 1 - TemperatureRoughness + TemperatureRoughness * tempNoise;
    
    // Change temperature with mountains
    tempValue *= (1 - mountainNoise) * MountainTemperatureAffect + 1 - MountainTemperatureAffect;
    
    //Return the calculated values
    Out = float3(mountainNoise, tempValue, treeNoise);
}

float3 evaluateBiomeMap(BiomeSettings biomeSettings, float3 pos)
{
    float3 returnValue;
    EvaluateBiomeMap_float(pos, biomeSettings.Seed, biomeSettings.MountainFrequency, biomeSettings.TemperatureFrequency, biomeSettings.TemperatureRoughness, biomeSettings.MountainTemperatureAffect, biomeSettings.TreeFrequency, returnValue);
    return returnValue;
}

#endif