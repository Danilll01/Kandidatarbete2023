using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "FoliageColor", menuName = "ScriptableObjects/CreateAssetForFoliageColor", order = 1)]
public class FoliageColor : ScriptableObject
{
    public TextureAndRows[] textureAndRows;
}


[System.Serializable]
public struct TextureAndRows
{
    public Texture2D texture;
    public int[] rowIndexesToTaint;
    public int totalNumberOfPixelsPerRow;
}