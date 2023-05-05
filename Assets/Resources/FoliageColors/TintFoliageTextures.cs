using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class TintFoliageTextures : MonoBehaviour
{

    private TextureAndRows[] textureAndRows;
    private Texture2D[] copiedTextures;

    private int totalNumberOfPixelsPerRow;
    private int textureHeight;
    private List<(int, int)> rows; // start and end height coordinate

    private List<Color[]> originalColors;

    public Color planetGroundColor;

    public void Initialize()
    {
        originalColors = new List<Color[]>();
        rows = new List<(int, int)>();

        string path = "FoliageColors/FoliageColor";
        textureAndRows = Resources.Load<FoliageColor>(path).textureAndRows;
        copiedTextures = new Texture2D[textureAndRows.Length];

        for (int i = 0; i < textureAndRows.Length; i++)
        {
            Texture2D texture = textureAndRows[i].texture;
            Texture2D newTexture = new Texture2D(texture.width, texture.height);
            newTexture.SetPixels(texture.GetPixels());
            newTexture.Apply();
            copiedTextures[i] = newTexture;
            textureAndRows[i].material.mainTexture = newTexture;
            originalColors.Add(newTexture.GetPixels());
        }
    }

    public void TintTextures()
    {
        for (int t = 0; t < textureAndRows.Length; t++)
        {
            TextureAndRows textureAndrow = textureAndRows[t];
            Texture2D texture = copiedTextures[t];
            rows.Clear();
            textureHeight = texture.height;
            totalNumberOfPixelsPerRow = textureAndrow.totalNumberOfPixelsPerRow;
            int[] rowIndexes = textureAndrow.rowIndexesToTaint;
            GetRows();

            for (int h = 0; h < rowIndexes.Length; h++)
            {
                int index = rowIndexes[h];
                (int, int) startAndEnd = rows[index];

                for (int j = startAndEnd.Item1; j > startAndEnd.Item2 - 1; j--)
                {
                    for (int i = 0; i < textureHeight; i++)
                    {
                        int indexInOriginalColors = ((j - 1) * textureHeight) + i;
                        if (j == 0)
                        {
                            indexInOriginalColors = i;
                        }
                        Color newColor = (originalColors[t][indexInOriginalColors] + planetGroundColor) * 0.5f;
                        texture.SetPixel(i, j, newColor);
                    }

                }
            }

            texture.Apply();
        }
    }

    private void GetRows()
    {
        int start = textureHeight;
        int end = textureHeight - totalNumberOfPixelsPerRow;

        for (int j = textureHeight; j > 0; j--)
        {
            if (j > end && j <= start)
            {
                rows.Add((start, end));
                start = end;
                end -= totalNumberOfPixelsPerRow;
            }
        }
    }
}

[CreateAssetMenu(fileName = "FoliageColor", menuName = "ScriptableObjects/CreateAssetForFoliageColor", order = 1)]
public class FoliageColor : ScriptableObject
{
    public TextureAndRows[] textureAndRows;
}


[System.Serializable]
public struct TextureAndRows
{
    public Texture2D texture;
    public Material material;
    public int[] rowIndexesToTaint;
    public int totalNumberOfPixelsPerRow;
}

