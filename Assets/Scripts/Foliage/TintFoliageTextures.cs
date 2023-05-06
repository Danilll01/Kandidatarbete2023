using UnityEngine;
using System.Collections.Generic;


public class TintFoliageTextures : MonoBehaviour
{
    [SerializeField] private Material[] textureMaterials;
    private TextureAndRows[] textureAndRows;
    private Texture2D[] copiedTextures;

    private int totalNumberOfPixelsPerRow;
    private int textureHeight;
    private List<(int start, int end)> pixelRows; // start and end height coordinate

    private List<Color[]> originalColors;

    public Color planetGroundColor;

    /// <summary>
    /// Initialize the assets and variables for tinting foliage textures
    /// </summary>
    public void Initialize()
    {
        originalColors = new List<Color[]>();
        pixelRows = new List<(int, int)>();

        // Load in the scriptable object containing the different assets and information we want
        string path = "FoliageColors/FoliageColor";
        textureAndRows = Resources.Load<FoliageColor>(path).textureAndRows;
        
        CopyTextures();
    }

    private void CopyTextures()
    {
        copiedTextures = new Texture2D[textureAndRows.Length];

        for (int i = 0; i < textureAndRows.Length; i++)
        {
            Texture2D texture = textureAndRows[i].texture;
            Texture2D newTexture = new Texture2D(texture.width, texture.height);
            newTexture.SetPixels(texture.GetPixels());
            newTexture.Apply();
            copiedTextures[i] = newTexture;
            textureMaterials[i].mainTexture = newTexture;
            originalColors.Add(newTexture.GetPixels());
        }
    }

    /// <summary>
    /// Tint the textures on certain rows
    /// </summary>
    public void TintTextures()
    {
        for (int t = 0; t < textureAndRows.Length; t++)
        {
            TextureAndRows textureAndrow = textureAndRows[t];
            Texture2D texture = copiedTextures[t];
            pixelRows.Clear();
            textureHeight = texture.height;
            totalNumberOfPixelsPerRow = textureAndrow.totalNumberOfPixelsPerRow;
            int[] rowIndexes = textureAndrow.rowIndexesToTaint;
            GetRows();

            for (int h = 0; h < rowIndexes.Length; h++)
            {
                int index = rowIndexes[h];
                (int, int) startAndEndPixelRows = pixelRows[index];
                TintColorOnRows(t, texture, startAndEndPixelRows);
            }

            texture.Apply();
        }
    }

    private void TintColorOnRows(int t, Texture2D texture, (int startRow, int endRow) startAndEndPixelRows)
    {
        for (int j = startAndEndPixelRows.startRow; j > startAndEndPixelRows.endRow - 1; j--)
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

    private void GetRows()
    {
        int start = textureHeight;
        int end = textureHeight - totalNumberOfPixelsPerRow;

        for (int j = textureHeight; j > 0; j--)
        {
            if (j > end && j <= start)
            {
                pixelRows.Add((start, end));
                start = end;
                end -= totalNumberOfPixelsPerRow;
            }
        }
    }
}

