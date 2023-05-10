#ifndef __terrain_hlsl_
#define __terrain_hlsl_

#include "../Tools/Noise/Noise.hlsl"

struct TerrainLayer
{
    float strength;
    float baseRoughness;
    float roughness;
    float persistance;
    float mountainPeakness;
    float mountainHeight;
    float flatSmoothness;
    float3 centre;
    int numLayers;
};

// Note, TemperatureRoughness and MountainTemperatureAffect must be in the range 0-1
struct BiomeSettings
{
    float Seed;
    float TemperatureDecay;
    float FarTemperature;
    float MountainFrequency;
    float TemperatureFrequency;
    float TemperatureRoughness;
    float MountainTemperatureAffect;
    float TreeFrequency;
};

float evalutateBiomeMapMountains(BiomeSettings biomeSettings, float3 pos);

float getTerrain(float3 pos, RWStructuredBuffer<TerrainLayer> terrainLayers, int numTerrainLayers, float seed, BiomeSettings biomeSettings)
{
    float3 pointOnSphere = pos / length(pos);
   
    float noiseValue = 0;
    
    float3 noiseOffset = float3(perlin.Evaluate(seed) * seed, perlin.Evaluate(seed + 17.8f) * seed, perlin.Evaluate(seed + 23.5) * seed);
    
    for (int i = 0; i < numTerrainLayers; i++)
    {
        float amplitude = 1;
        float frequency = terrainLayers[i].baseRoughness;
        float noiseLayer;
        float amplitudeSum = 0;
    
        for (int j = 0; j < terrainLayers[i].numLayers; j++)
        {
            amplitudeSum += amplitude;
            noiseLayer += (simplex.Evaluate(pointOnSphere * frequency + noiseOffset) + 1) * 0.5f * amplitude;
            frequency *= terrainLayers[i].roughness;
            amplitude *= terrainLayers[i].persistance;
            noiseOffset = float3(perlin.Evaluate(noiseOffset.x) * seed, perlin.Evaluate(noiseOffset.y + 17.8f) * seed, perlin.Evaluate(noiseOffset.z + 23.5) * seed);
        }
        
        // Normalize noiselayer to be within range [0, 1]
        noiseLayer *= 1 / amplitudeSum;
        
        // Create mountains
        float mountains = evalutateBiomeMapMountains(biomeSettings, pos);
        mountains *= mountains;
        float mountainGround = pow(abs(noiseLayer), (1 + mountains) * terrainLayers[i].mountainPeakness) * mountains;
        float flatGround = pow(abs(noiseLayer), terrainLayers[i].flatSmoothness);
        noiseLayer = mountainGround * terrainLayers[i].mountainHeight + flatGround;
        
        // Multiply the noiselayer with the wanted strength
        noiseLayer *= terrainLayers[i].strength;    
        
        // Add the current noiselayer
        noiseValue += noiseLayer;
    }

    float ground = length(pos) < (.7 + noiseValue) ? ((.7 + noiseValue) - length(pos)) * 255 : 0;
    /*
    float caveFrequency = 13;
    float caveDensity = 55;
    
    // Create the caves
    float caveNoise = (simplex.Evaluate(pos * caveFrequency) + 1) * .5;
    ground *= (1 - 1 / (caveDensity * caveNoise + 1)) * (1 + 1 / caveDensity); //function to control cave density*/
    
    return ground;
}

// Note, TemperatureRoughness and MountainTemperatureAffect must be in the range 0-1
void EvaluateBiomeMap_float(float3 UV, float Distance, float Seed, float TemperatureDecay, float FarTemperature, float MountainFrequency, float TemperatureFrequency, float TemperatureRoughness, float MountainTemperatureAffect, float TreeFrequency, out float3 Out)
{
    // Normalize position
    UV = normalize(UV);
    
    // Get noisevalue for each map
    float mountainNoise = (simplex.Evaluate(float3(UV.x + Seed, UV.y, UV.z) * MountainFrequency) + 1) * .5f;
    float tempNoise = (simplex.Evaluate(float3(UV.x, UV.y + Seed, UV.z) * TemperatureFrequency) + 1) * .5f;
    float treeNoise = (simplex.Evaluate(float3(UV.x, UV.y, UV.z + Seed) * TreeFrequency) + 1) * .5f;
    
    float distanceTemp = (1 - FarTemperature) / (TemperatureDecay * TemperatureDecay * Distance + 1) + FarTemperature;
    
    // Generate temperature map
    float tempValue = distanceTemp - abs(UV.y) * sqrt(1 - distanceTemp);
    
    // Rough up the temperature map
    tempValue *= 1 - TemperatureRoughness + TemperatureRoughness * tempNoise;
    
    // Change temperature with mountains
    tempValue *= (1 - mountainNoise) * MountainTemperatureAffect + 1 - MountainTemperatureAffect;
    
    // Calculate multiplier for temperatue (due to distance from sun)
    //tempValue *= (1 - FarTemperature) / (TemperatureDecay * TemperatureDecay * Distance + 1) + FarTemperature;
    
    //Return the calculated values
    Out = float3(mountainNoise, tempValue, treeNoise);
}

float3 evaluateBiomeMap(BiomeSettings biomeSettings, float3 pos, float distance)
{
    float3 returnValue;
    EvaluateBiomeMap_float(pos, distance, biomeSettings.Seed, biomeSettings.TemperatureDecay, biomeSettings.FarTemperature, biomeSettings.MountainFrequency, biomeSettings.TemperatureFrequency, biomeSettings.TemperatureRoughness, biomeSettings.MountainTemperatureAffect, biomeSettings.TreeFrequency, returnValue);
    return returnValue;
}

float evalutateBiomeMapMountains(BiomeSettings biomeSettings, float3 pos)
{
    pos = normalize(pos);
    return (simplex.Evaluate(float3(pos.x + biomeSettings.Seed, pos.y, pos.z) * biomeSettings.MountainFrequency) + 1) * .5f;
}

#endif