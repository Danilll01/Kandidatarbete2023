using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Create3dTexture : MonoBehaviour
{

    [Range(0, 1), SerializeField] int update = 0;

    [SerializeField] ComputeShader shader;

    int resolution = 8;

#if UNITY_EDITOR
    // On validate called each time something changes in editor
    private void OnValidate()
    {
        Texture3D texture = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] colorArr = new Color[resolution * resolution * resolution];

        ComputeBuffer buffer = new ComputeBuffer(resolution * resolution * resolution, 4 * sizeof(float));

        buffer.SetData(colorArr);

        int kernelId = shader.FindKernel("CSMain");

        shader.SetBuffer(kernelId, "colors", buffer);
        shader.Dispatch(kernelId, 1, 1, 1);

        buffer.GetData(colorArr);

        buffer.Dispose();

        texture.SetPixels(colorArr);

        texture.Apply();

        AssetDatabase.CreateAsset(texture, "Assets/Assets/PlanetTerrain/Example3DTexture.asset");

        print(colorArrToString(colorArr));
    }

    private string colorToString(Color color)
    {
        return "(" + color.r + ", " + color.g + ", " + color.b + ", " + color.a +  ")";
    }

    private string colorArrToString(Color[] arr)
    {
        string res = "[";
        for(int i = 0; i < arr.Length - 1; i++)
        {
            res += colorToString(arr[i]) + ", ";
        }
        return res + colorToString(arr[arr.Length - 1]) + "]";
    }
#endif
}















