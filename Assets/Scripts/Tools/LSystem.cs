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
    public struct Sequence
    {
        public List<Movement> sequence;

        override public string ToString()
        {
            string res = string.Empty;
            foreach(Movement movement in sequence)
            {
                res += movement.ToString() + "->";
            }
            return res;
        }
    }

    [Serializable]
    public struct Rule
    {
        public Movement identity;
        public Sequence replacement;
    }

    private List<Rule> rules;
    private int iterations;
    private Sequence sequence;

    public LSystem(Sequence startSequence, List<Rule> rules, int iterations)
    {
        sequence = startSequence;
        this.rules = rules;
        this.iterations = iterations;

        Generate();
    }

    private void Generate()
    {
        for(int i = 0; i < iterations; i++)
        {
            Sequence newSequence = new Sequence();
            newSequence.sequence = new List<Movement>();
            foreach(Movement movement in sequence.sequence)
            {
                newSequence.sequence.Add(movement);
                foreach (Rule rule in rules)
                {
                    if(rule.identity == movement)
                    {
                        newSequence.sequence.RemoveAt(newSequence.sequence.Count - 1);
                        newSequence.sequence.AddRange(rule.replacement.sequence);
                        break;
                    }
                }
            }
            sequence = newSequence;
        }
    }

    public Sequence GetSequence() { return sequence; }
}
