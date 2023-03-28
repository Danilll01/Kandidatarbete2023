using UnityEngine;
using Noise;
using System;


/// <summary>
/// Struct for storing the biomesettings
/// </summary>
public struct BiomeSettings
{
    public float seed;
    public float mountainFrequency;
    public float temperatureFrequency;
    public float temperatureRoughness;
    public float mountainTemperatureAffect;
    public float treeFrequency;

    /// <summary>
    /// <i>Note: parameters <paramref name="temperatureRoughness"/> and <paramref name="mountainTemperatureAffect"/> must
    /// be in range [0, 1]</i>
    /// </summary>
    public BiomeSettings(float seed, float mountainFrequency, float temperatureFrequency, float temperatureRoughness, float mountainTemperatureAffect, float treeFrequency)
    {
        // Check that variables are in range
        Details.AssertInRange(temperatureRoughness, 0, 1, nameof(temperatureRoughness));
        Details.AssertInRange(mountainTemperatureAffect, 0, 1, nameof(mountainTemperatureAffect));

        this.seed = seed;
        this.mountainFrequency = mountainFrequency;
        this.temperatureFrequency = temperatureFrequency;
        this.temperatureRoughness = temperatureRoughness;
        this.mountainTemperatureAffect = mountainTemperatureAffect;
        this.treeFrequency = treeFrequency;
    }
}

/// <summary>
/// Struct for keeping the biomevalues
/// </summary>
public struct BiomeValue
{
    public float mountains;
    public float temperature;
    public float trees;

    /// <summary>
    /// <i>Note: All parameters must be in range [0, 1]</i>
    /// </summary>
    public BiomeValue(float mountains, float temperature, float trees)
    {
        // Check that variable are in range
        Details.AssertInRange(mountains, 0, 1, nameof(mountains));
        Details.AssertInRange(temperature, 0, 1, nameof(temperature));
        Details.AssertInRange(trees, 0, 1, nameof(trees));

        this.mountains = mountains;
        this.temperature = temperature;
        this.trees = trees;
    }
}

/// <summary>
/// Class for calculating biomeValue
/// </summary>
public static class Biomes
{
    /// <summary>
    /// Evaluates biomemap with given <paramref name="biomeSettings"/> at <paramref name="position"/>
    /// </summary>
    public static BiomeValue EvaluteBiomeMap(BiomeSettings biomeSettings, Vector3 position)
    {
        // Normalize position
        position = Vector3.Normalize(position);

        // Get noisevalue for each map
        float mountainNoise = (Simplex.Evaluate(
            x: (position.x + biomeSettings.seed) * biomeSettings.mountainFrequency,
            y: position.y * biomeSettings.mountainFrequency,
            z: position.z * biomeSettings.mountainFrequency) + 1) * .5f;

        float tempNoise = (Simplex.Evaluate(
            x: position.x * biomeSettings.temperatureFrequency,
            y: (position.y + biomeSettings.seed) * biomeSettings.temperatureFrequency,
            z: position.z * biomeSettings.temperatureFrequency) + 1) * .5f;

        float treeNoise = (Simplex.Evaluate(
            x: position.x * biomeSettings.treeFrequency,
            y: position.y * biomeSettings.treeFrequency,
            z: (position.z + biomeSettings.seed) * biomeSettings.treeFrequency) + 1) * .5f;

        // Generate temperature map
        float tempValue = 1 - Mathf.Abs(position.y);

        // Rough up the temperature map
        tempValue *= 1 - biomeSettings.temperatureRoughness + biomeSettings.temperatureRoughness * tempNoise;

        // Change temperature with mountains
        tempValue *= (1 - mountainNoise) * biomeSettings.mountainTemperatureAffect + 1 - biomeSettings.mountainTemperatureAffect;

        //Return the calculated values
        return new BiomeValue(mountainNoise, tempValue, treeNoise);
    }

    /// <summary>
    /// Evaluates the value of the mountainmap at <paramref name="position"/> with <paramref name="biomeSettings"/>
    /// </summary>
    public static float EvaluteBiomeMapMountains(BiomeSettings biomeSettings, Vector3 position)
    {
        // Normalize position
        position = Vector3.Normalize(position);

        // Return noisevalue for mountains
        return (Simplex.Evaluate(
            x: (position.x + biomeSettings.seed) * biomeSettings.mountainFrequency,
            y: position.y * biomeSettings.mountainFrequency,
            z: position.z * biomeSettings.mountainFrequency) + 1) * .5f;
    }

    /// <summary>
    /// Evaluates the value of the temperaturemap at <paramref name="position"/> with <paramref name="biomeSettings"/>
    /// </summary>
    public static float EvaluteBiomeMapTemperature(BiomeSettings biomeSettings, Vector3 position)
    {
        // Normalize position
        position = Vector3.Normalize(position);

        // Get noisevalue for each map
        float mountainNoise = (Simplex.Evaluate(
            x: (position.x + biomeSettings.seed) * biomeSettings.mountainFrequency,
            y: position.y * biomeSettings.mountainFrequency,
            z: position.z * biomeSettings.mountainFrequency) + 1) * .5f;

        float tempNoise = (Simplex.Evaluate(
            x: position.x * biomeSettings.temperatureFrequency,
            y: (position.y + biomeSettings.seed) * biomeSettings.temperatureFrequency,
            z: position.z * biomeSettings.temperatureFrequency) + 1) * .5f;

        // Generate temperature map
        float tempValue = 1 - Mathf.Abs(position.y);

        // Rough up the temperature map
        tempValue *= 1 - biomeSettings.temperatureRoughness + biomeSettings.temperatureRoughness * tempNoise;

        // Change temperature with mountains
        tempValue *= (1 - mountainNoise) * biomeSettings.mountainTemperatureAffect + 1 - biomeSettings.mountainTemperatureAffect;

        return tempValue;
    }

    /// <summary>
    /// Evaluates the value of the treemap at <paramref name="position"/> with <paramref name="biomeSettings"/>
    /// </summary>
    public static float EvaluteBiomeMapTrees(BiomeSettings biomeSettings, Vector3 position)
    {
        // Normalize position
        position = Vector3.Normalize(position);

        // Return noisevalue for trees
        return (Simplex.Evaluate(
            x: position.x * biomeSettings.treeFrequency,
            y: position.y * biomeSettings.treeFrequency,
            z: (position.z + biomeSettings.seed) * biomeSettings.treeFrequency) + 1) * .5f;
    }
}

/// <summary>
/// Details class only used as helper class, not to be used outside Biomes.cs
/// </summary>
public static class Details 
{
    /// <summary>
    /// Checks if <paramref name="parameter"/> is in range [<paramref name="a"/>, <paramref name="b"/>], 
    /// otherwise throws new <exception cref="ArgumentOutOfRangeException">ArgumentOutOfRangeException</exception>
    /// </summary>
    /// <param name="parameterName">Name of parameter to be checked</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void AssertInRange(float parameter, float a, float b, string parameterName)
    {
        if (a > parameter || parameter > b)
            throw new ArgumentOutOfRangeException(parameterName, $"Parameter must be in range [{a}, {b}]");
    }
}

