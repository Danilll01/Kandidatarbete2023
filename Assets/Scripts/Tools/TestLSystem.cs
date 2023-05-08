using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TestLSystem : MonoBehaviour
{
    [SerializeField] private int resolution;
    [SerializeField] private int chunkResolution;
    [SerializeField] private Caves.CaveSettings caveSettings;

    private Caves caves;
    private void OnValidate()
    {
        caves = new Caves(resolution, chunkResolution, caveSettings);
        Texture3D caveTexture = caves.GetCaves();
        AssetDatabase.CreateAsset(caveTexture, "Assets/Test.asset");
    }

    private void Update()
    {
        /*
        for(int i = 0; i < 10; i++)
        {
            lSystem.Step(out Vector3 position, out bool isFinished);
            if (!isFinished)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position;
            }
        }*/
    }

    /*
    #if UNITY_EDITOR
        void OnValidate()
        {
            LSystem lsystem = new LSystem(startSequence, rules, iterations, movementSettings);
            //LSystem.Sequence sequence = lsystem.GetSequence();
            //Debug.Log(sequence.ToString());
        }
    #endif*/
}
