using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLSystem : MonoBehaviour
{
    [SerializeField] private LSystem.Sequence startSequence;
    [SerializeField] private List<LSystem.Rule> rules;
    [SerializeField] private int iterations;
    [SerializeField] private LSystem.MovementSettings movementSettings;

    private LSystem lSystem;

    private void Start()
    {
        lSystem = new LSystem(startSequence, rules, iterations, movementSettings);
    }

    private void Update()
    {
        for(int i = 0; i < 10; i++)
        {
            lSystem.Step(out Vector3 position, out bool isFinished);
            if (!isFinished)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position;
            }
        }
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
