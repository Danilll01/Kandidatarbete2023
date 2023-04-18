using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLSystem : MonoBehaviour
{
    [SerializeField] private LSystem.Sequence startSequence;
    [SerializeField] private List<LSystem.Rule> rules;
    [SerializeField] private int iterations;


#if UNITY_EDITOR
    void OnValidate()
    {
        LSystem lsystem = new LSystem(startSequence, rules, iterations);
        LSystem.Sequence sequence = lsystem.GetSequence();
        Debug.Log(sequence.ToString());
    }
#endif
}
