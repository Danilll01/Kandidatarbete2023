using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLSystem : MonoBehaviour
{
    [SerializeField] private LSystem.Sequence startSequence;
    [SerializeField] private List<LSystem.Rule> rules;
    [SerializeField] private int iterations;
    [SerializeField] private LSystem.MovementSettings movementSettings;


#if UNITY_EDITOR
    void OnValidate()
    {
        LSystem lsystem = new LSystem(startSequence, rules, iterations, movementSettings);
        //LSystem.Sequence sequence = lsystem.GetSequence();
        //Debug.Log(sequence.ToString());
    }
#endif
}
