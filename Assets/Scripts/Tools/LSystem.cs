using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSystem
{
    public enum Movement
    {
        Forward,
        YawClockwise,
        YawCounterClockwise,
        PitchUp,
        PitchDown,
        StartBranch,
        EndBranch
    }

    [Serializable]
    public struct Rule
    {
        public List<Movement> rule;
    }

    private List<Rule> rules;
    private int iterations;

    public LSystem(List<Rule> rules, int iterations)
    {
        this.rules = rules;
        this.iterations = iterations;
    }
}
