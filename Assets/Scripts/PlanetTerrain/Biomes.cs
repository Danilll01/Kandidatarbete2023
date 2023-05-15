using UnityEngine;
using Noise;
using System;

/// <summary>
/// Struct for storing the biomesettings
/// </summary>
[Serializable]
public struct BiomeSettings
{
    public float seed;
    public float distance;
    public float temperatureDecay;
    public float farTemperature;
    public float mountainFrequency;
    public float temperatureFrequency;
    [Range(0, 1)] public float temperatureRoughness;
    [Range(0, 1)] public float mountainTemperatureAffect;
    public float treeFrequency;

    /// <summary>
    /// <i>Note: parameters <paramref name="temperatureRoughness"/> and <paramref name="mountainTemperatureAffect"/> must
    /// be in range [0, 1]</i>
    /// </summary>
    public BiomeSettings(float seed, float distance, float temperatureDecay, float farTemperature, float mountainFrequency, float temperatureFrequency, float temperatureRoughness, float mountainTemperatureAffect, float treeFrequency)
    {
        // Check that variables are in range
        Details.AssertInRange(temperatureRoughness, 0, 1, nameof(temperatureRoughness));
        Details.AssertInRange(mountainTemperatureAffect, 0, 1, nameof(mountainTemperatureAffect));
        Details.AssertInRange(farTemperature, 0, 1, nameof(farTemperature));

        this.seed = seed;
        this.distance = distance;
        this.temperatureDecay = temperatureDecay;
        this.farTemperature = farTemperature;
        this.mountainFrequency = mountainFrequency;
        this.temperatureFrequency = temperatureFrequency;
        this.temperatureRoughness = temperatureRoughness;
        this.mountainTemperatureAffect = mountainTemperatureAffect;
        this.treeFrequency = treeFrequency;
    }

    /// <summary>
    /// Turns the biomesettings into an array
    /// </summary>
    public float[] ToArray()
    {
        float[] arr = new float[8];
        for(int i = 0; i < arr.Length; i++)
        {
            arr[i] = this[i];
        }
        return arr;
    }

    private float this[int i]
    {
        get
        {
            switch (i)
            {
                case 0: return seed;
                case 1: return temperatureDecay;
                case 2: return farTemperature;
                case 3: return mountainFrequency;
                case 4: return temperatureFrequency;
                case 5: return temperatureRoughness;
                case 6: return mountainTemperatureAffect;
                case 7: return treeFrequency;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
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

    public bool IsInsideRange(BiomeRange range)
    {
        return
            (!range.mountainRelevant || (range.mountainMin <= mountains && mountains <= range.mountainMax)) &&
            (!range.temperatureDependent || (range.temperatureMin <= temperature && temperature <= range.temperatureMax)) &&
            (!range.treesDependent || (range.treesMin <= trees && trees <= range.treesMax));
    }
}

[Serializable]
public class BiomeRange
{
    public bool mountainRelevant = false;
    [Range(0f, 1f)] public float mountainMin = 0;
    [Range(0f, 1f)] public float mountainMax = 1;
    public bool temperatureDependent = false;
    [Range(0f, 1f)] public float temperatureMin = 0;
    [Range(0f, 1f)] public float temperatureMax = 1;
    public bool treesDependent = false;
    [Range(0f, 1f)] public float treesMin = 0;
    [Range(0f, 1f)] public float treesMax = 1;
}

/// <summary>
/// Class for calculating biomeValue
/// </summary>
public static class Biomes
{
    /// <summary>
    /// Evaluates biomemap with given <paramref name="biomeSettings"/> at <paramref name="position"/> 
    /// with the <paramref name="distance"/> from the sun.
    /// </summary>
    public static BiomeValue EvaluteBiomeMap(BiomeSettings biomeSettings, Vector3 position, float distance)
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

        // Calculate multiplier for temperatue (due to distance from sun)
        tempValue *= (1 - biomeSettings.farTemperature) / (biomeSettings.temperatureDecay * biomeSettings.temperatureDecay * distance + 1) + biomeSettings.farTemperature;

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
    /// with the <paramref name="distance"/> to the sun.
    /// </summary>
    public static float EvaluteBiomeMapTemperature(BiomeSettings biomeSettings, Vector3 position, float distance)
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

        // Calculate multiplier for temperatue (due to distance from sun)
        tempValue *= (1 - biomeSettings.farTemperature) / (biomeSettings.temperatureDecay * biomeSettings.temperatureDecay * distance + 1) + biomeSettings.farTemperature;

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

    /// <summary>
    /// Evaluates a representation of the temperaturemap at <paramref name="position"/> with <paramref name="biomeSettings"/> in celcius
    /// with the <paramref name="distance"/> to the sun.
    /// </summary>
    public static string GetTemperatureAt(BiomeSettings biomeSettings, Vector3 position, float distance)
    {
        float tmp = EvaluteBiomeMapTemperature(biomeSettings, position, distance);

        return GetTemperature(tmp);
    }

    /// <summary>
    /// Evaluates a representation of the temperaturemap with the value <paramref name="tmp"/>.
    /// </summary>
    public static string GetTemperature(float tmp)
    {
        (float temperature, float celcius)[] tempGuides = { (0.03f, -50f), (0.1f, 5f), (0.2f, 15f), (0.3f, 25f), (0.5f, 40f), (0.6f, 60f), (0.7f, 100f) };

        if (tempGuides[0].temperature > tmp)
        {
            return "ERROR LOW �C";
        }

        for (int i = 0; i < tempGuides.Length - 1; i++)
        {
            if (tempGuides[i + 1].temperature >= tmp)
            {
                (float temperature, float celcius) tmp1 = tempGuides[i];
                (float temperature, float celcius) tmp2 = tempGuides[i + 1];
                float progressToNext = (tmp - tmp1.temperature) / (tmp2.temperature - tmp1.temperature);
                

                float tmpC = (1 - progressToNext) * tmp1.celcius + progressToNext * tmp2.celcius;
                tmpC = (float)Math.Round(tmpC * 100) / 100;
                return tmpC.ToString() + " �C";
            }
        }
        return "ERROR HIGH �C";
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

