using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TestLSystem : MonoBehaviour
{
    [SerializeField] private int resolution;
    [SerializeField] private int chunkResolution;
    [SerializeField] private Caves.CaveSettings caveSettings;
    [SerializeField] private bool run = false;
    [SerializeField] private bool update;

    private Caves caves;
    private void OnValidate()
    {
        if(run)
        {
            caves = new Caves(resolution, chunkResolution, caveSettings);
            Texture3D test = caves.GetCaves();
            AssetDatabase.CreateAsset(test, "Assets/Test.asset");
            Debug.Log("Caves generated!");
        }
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
