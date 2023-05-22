using UnityEngine;
using System.Collections.Generic;


public class TintFoliageTextures : MonoBehaviour
{
    public Material[] textureMaterials;
    private TextureAndRows[] textureAndRows;
    private Texture2D[] copiedTextures;

    private int totalNumberOfPixelsPerRow;
    private int textureHeight;
    private List<(int start, int end)> pixelRows; // start and end height coordinate

    public List<Color[]> originalColors;

    public Color planetGroundColor;
    public Color planetMountainColor;
    public Color[] biomeColors;
    [SerializeField] public Material[] biomeMaterials;
    public BiomeFoliageData[] biomeFoliageDatas;
    

    private bool Initialized;

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

        biomeMaterials = new Material[biomeColors.Length * 2];
        biomeFoliageDatas = new BiomeFoliageData[biomeColors.Length * 2];

        int index = 0;
        for (int i = 0; i < 2; i++)
        {
            originalColors.Add(textureAndRows[i].texture.GetPixels());
            
            // We don't need to create new materials for every biome since we already have a material
            for (int j = 0; j < biomeColors.Length; j++)
            {
                Material newMaterial = CreateNewMaterial(index);
                Texture2D texture = CopyTextures(newMaterial, i);

                if (j != biomeColors.Length - 1)
                {
                    biomeFoliageDatas[index].biomeMaterial = newMaterial;
                    biomeFoliageDatas[index].texture = texture;
                }

                biomeFoliageDatas[index].biomeIndex = j;
                biomeFoliageDatas[index].packageIndex = i + 1;
                biomeFoliageDatas[index].biomeName = j == 0 ? "GroundColor" : "Biome " + j + ", pack " + (i + 1);
                biomeFoliageDatas[index].biomeColor = biomeColors[j];
                index++;
            }

            Texture2D copiedTexture = CopyTextures(textureMaterials[i], i);
            biomeMaterials[index - 1] = textureMaterials[i];
            biomeFoliageDatas[index - 1].biomeMaterial = textureMaterials[i];
            biomeFoliageDatas[index - 1].texture = copiedTexture;
            
        }
    }

    private Material CreateNewMaterial(int i)
    {
        Material newMaterial = new Material(textureMaterials[0].shader); // Both materials use the same shader (index does not matter)
        newMaterial.name = "Biome " + i;
        biomeMaterials[i] = newMaterial;
        return newMaterial;

    }

    private Texture2D CopyTextures(Material material, int packageIndex)
    {
        Texture2D textureToCopy = textureAndRows[packageIndex].texture;
        Texture2D newTexture = new Texture2D(textureToCopy.width, textureToCopy.height);
        newTexture.SetPixels(textureToCopy.GetPixels());
        newTexture.Apply();
        material.mainTexture = newTexture;
        return newTexture;
        


        /*
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
        */
    }

    /// <summary>
    /// Tint the textures on certain rows
    /// </summary>
    public void TintTextures()
    {
        if (!Initialized)
        {
            Initialize();
            Initialized = true;
        }
        else
        {
            GetNewBiomeColors();
        }

        for (int i = 0; i < biomeFoliageDatas.Length; i++)
        {
            BiomeFoliageData biomeFoliageData = biomeFoliageDatas[i];
            bool packageOne = biomeFoliageData.packageIndex == 1;
            TextureAndRows textureAndRow = packageOne ? textureAndRows[0] : textureAndRows[1];

            Texture2D texture = biomeFoliageData.texture;
            pixelRows.Clear();
            textureHeight = texture.height;
            totalNumberOfPixelsPerRow = textureAndRow.totalNumberOfPixelsPerRow;
            int[] rowIndexes = textureAndRow.rowIndexesToTaint;
            GetRows();

            for (int h = 0; h < rowIndexes.Length; h++)
            {
                int index = rowIndexes[h];
                (int, int) startAndEndPixelRows = pixelRows[index];
                TintColorOnRows(biomeFoliageData.packageIndex - 1, texture, startAndEndPixelRows, biomeFoliageData.biomeColor);
            }

            texture.Apply();
        }

        /*
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
        */
    }

    private void GetNewBiomeColors()
    {
        int index = 0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < biomeColors.Length; j++)
            {
                biomeFoliageDatas[index].biomeColor = biomeColors[j];
                index++;
            }
        }
    }

    private void TintColorOnRows(int t, Texture2D texture, (int startRow, int endRow) startAndEndPixelRows, Color biomeColor)
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
                Color newColor = (originalColors[t][indexInOriginalColors] + biomeColor) * 0.5f;
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

    [System.Serializable]
    public struct BiomeFoliageData
    {
        [HideInInspector] public string biomeName;
        public int biomeIndex;
        public int packageIndex;
        public Material biomeMaterial;
        public Texture2D texture;
        public Color biomeColor;
    }
}

